using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.BuildingBlocks.Infrastructure.Persistence;
using RutaViva.BuildingBlocks.IntegrationTests.Persistence;
using RutaViva.IntegrationContracts;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.Messaging;

[Trait("Category", "Integration")]
public sealed class OutboxIntegrationEventPublisherTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string ServiceName = "maintenance";
    private const string ExpectedType = "rutaviva.maintenance.SampleHappened.v1";
    private const string ExpectedSource = "/rutaviva/maintenance";
    private const string OrderIdProperty = "orderId";

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Enqueue_Should_NotPersistRows_When_SaveChangesIsNotCalled()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string subject = Guid.NewGuid().ToString();
        await using TestDbContext context = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        OutboxIntegrationEventPublisher publisher = CreatePublisher(context);

        publisher.Enqueue(new SampleHappenedV1(Guid.NewGuid()), subject);

        int rows = await CountRowsAsync(subject, cancellationToken);
        rows.Should().Be(0);
    }

    [Fact]
    public async Task Enqueue_Should_PersistEnvelopeMessage_When_SaveChangesIsCalled()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string subject = Guid.NewGuid().ToString();
        Guid orderId = Guid.NewGuid();
        await using TestDbContext context = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        OutboxIntegrationEventPublisher publisher = CreatePublisher(context);

        publisher.Enqueue(new SampleHappenedV1(orderId), subject);
        Result saved = await new EfUnitOfWork<TestDbContext>(context).SaveChangesAsync(cancellationToken);

        saved.IsSuccess.Should().BeTrue();
        OutboxMessage message = await ReadSingleAsync(subject, cancellationToken);
        message.EventType.Should().Be(ExpectedType);
        message.Stream.Should().Be(SampleHappenedV1.Stream);
        message.ProcessedAt.Should().BeNull();
        IntegrationEventEnvelope envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(message.Payload, IntegrationEventJson.Options)!;
        envelope.Id.Should().Be(message.Id);
        envelope.Type.Should().Be(ExpectedType);
        envelope.Source.Should().Be(ExpectedSource);
        envelope.Subject.Should().Be(subject);
        envelope.Time.Should().Be(Now);
        envelope.Data.GetProperty(OrderIdProperty).GetGuid().Should().Be(orderId);
    }

    private static OutboxIntegrationEventPublisher CreatePublisher(TestDbContext context)
    {
        IntegrationEventSettings settings = new() { ServiceName = ServiceName };
        IntegrationEventEnvelopeFactory envelopeFactory = new(new FakeClock(Now), settings);
        return new OutboxIntegrationEventPublisher(context, new OutboxMessageFactory(envelopeFactory));
    }

    private async Task<int> CountRowsAsync(string subject, CancellationToken cancellationToken)
    {
        await using TestDbContext reader = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        return await reader.Set<OutboxMessage>().CountAsync(message => message.Subject == subject, cancellationToken);
    }

    private async Task<OutboxMessage> ReadSingleAsync(string subject, CancellationToken cancellationToken)
    {
        await using TestDbContext reader = await database.CreateDbContextAsync<TestDbContext>(cancellationToken);
        return await reader.Set<OutboxMessage>().SingleAsync(message => message.Subject == subject, cancellationToken);
    }
}
