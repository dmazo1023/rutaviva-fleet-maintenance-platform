using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.BuildingBlocks.EventBus.Client.Publishing;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Publishing;

[Trait("Category", "Integration")]
public sealed class OutboxBatchProcessorTests(EventBusTestServer server, SqlServerTestDatabase database)
    : IClassFixture<EventBusTestServer>, IClassFixture<SqlServerTestDatabase>
{
    private const string RetryStream = "relay.retry";
    private const string OrderStream = "relay.order";
    private const string IdempotencyStream = "relay.idempotency";
    private const string EventType = "rutaviva.relay.SampleHappened.v1";
    private const string EventSource = "/rutaviva/relay";
    private const string EventSubject = "relay-subject";
    private const string DataProperty = "value";
    private const string DataValue = "sample";
    private const int FailingCycles = 3;
    private const int ReadMax = 100;
    private const string PoisonStream = "relay.poison";
    private const string InvalidPayload = "not-json";
    private const string HubFailureMessage = "Simulated hub failure";
    private const string UnavailableMessage = "Simulated bus outage";
    private const long FromStart = 0;
    private const int NoAttempts = 0;
    private const int SingleAttempt = 1;

    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_Should_RegisterFailuresAndPublishOnce_When_TransportFailsInFirstCycles()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await ClearOutboxAsync(cancellationToken);
        OutboxMessage message = CreateMessage(RetryStream, Now);
        await SeedAsync([message], cancellationToken);
        await using EventBusPublisher publisher = CreatePublisher();
        FailingThenDelegatingTransport transport = new(publisher, FailingCycles);

        for (int cycle = 0; cycle < FailingCycles; cycle++)
        {
            await RunCycleAsync(transport, cancellationToken);
        }

        OutboxMessage pending = await ReadMessageAsync(message.Id, cancellationToken);
        pending.ProcessedAt.Should().BeNull();
        pending.Attempts.Should().Be(FailingCycles);
        pending.LastError.Should().Be(FailingThenDelegatingTransport.FailureMessage);

        await RunCycleAsync(transport, cancellationToken);

        OutboxMessage published = await ReadMessageAsync(message.Id, cancellationToken);
        published.ProcessedAt.Should().Be(Now);
        IReadOnlyList<EventRecord> events = await ReadEventsAsync(RetryStream, cancellationToken);
        events.Should().ContainSingle(record => record.Envelope.Id == message.Id);
    }

    [Fact]
    public async Task ProcessAsync_Should_PublishInInsertionOrder_When_ThreeMessagesShareOccurredAt()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await ClearOutboxAsync(cancellationToken);
        OutboxMessage first = CreateMessage(OrderStream, Now);
        OutboxMessage second = CreateMessage(OrderStream, Now);
        OutboxMessage third = CreateMessage(OrderStream, Now);
        await SeedAsync([third, first, second], cancellationToken);
        await using EventBusPublisher publisher = CreatePublisher();

        await RunCycleAsync(publisher, cancellationToken);

        IReadOnlyList<EventRecord> events = await ReadEventsAsync(OrderStream, cancellationToken);
        events.Select(record => record.Envelope.Id).Should().Equal(third.Id, first.Id, second.Id);
    }

    [Fact]
    public async Task ProcessAsync_Should_MarkFailedAndPublishNext_When_MessageFailsTransientlyMaxAttemptsTimes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await ClearOutboxAsync(cancellationToken);
        OutboxMessage poisoned = CreateMessage(PoisonStream, Now);
        OutboxMessage healthy = CreateMessage(PoisonStream, Now);
        await SeedAsync([poisoned, healthy], cancellationToken);
        await using EventBusPublisher publisher = CreatePublisher();
        FailingThenDelegatingTransport transport = new(publisher, OutboxMessage.MaxAttempts);

        for (int cycle = 0; cycle < OutboxMessage.MaxAttempts; cycle++)
        {
            await RunCycleAsync(transport, cancellationToken);
        }

        OutboxMessage failed = await ReadMessageAsync(poisoned.Id, cancellationToken);
        failed.FailedAt.Should().Be(Now);
        failed.ProcessedAt.Should().BeNull();
        failed.Attempts.Should().Be(OutboxMessage.MaxAttempts);

        await RunCycleAsync(transport, cancellationToken);

        OutboxMessage published = await ReadMessageAsync(healthy.Id, cancellationToken);
        published.ProcessedAt.Should().Be(Now);
        IReadOnlyList<EventRecord> events = await ReadEventsAsync(PoisonStream, cancellationToken);
        events.Should().ContainSingle(record => record.Envelope.Id == healthy.Id);
    }

    [Fact]
    public async Task ProcessAsync_Should_MarkFailedWithoutRetries_When_PayloadIsInvalid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await ClearOutboxAsync(cancellationToken);
        OutboxMessage message = new(Guid.NewGuid(), PoisonStream, EventType, EventSubject, InvalidPayload, Now);
        await SeedAsync([message], cancellationToken);
        IIntegrationEventTransport transport = Substitute.For<IIntegrationEventTransport>();

        await RunCycleAsync(transport, cancellationToken);
        await RunCycleAsync(transport, cancellationToken);

        OutboxMessage failed = await ReadMessageAsync(message.Id, cancellationToken);
        failed.FailedAt.Should().Be(Now);
        failed.Attempts.Should().Be(SingleAttempt);
        await transport.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<IntegrationEventEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_Should_MarkFailedWithoutRetries_When_HubRejectsTheMessage()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await ClearOutboxAsync(cancellationToken);
        OutboxMessage message = CreateMessage(PoisonStream, Now);
        await SeedAsync([message], cancellationToken);
        IIntegrationEventTransport transport = Substitute.For<IIntegrationEventTransport>();
        transport.SendAsync(Arg.Any<string>(), Arg.Any<IntegrationEventEnvelope>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HubException(HubFailureMessage));

        await RunCycleAsync(transport, cancellationToken);
        await RunCycleAsync(transport, cancellationToken);

        OutboxMessage failed = await ReadMessageAsync(message.Id, cancellationToken);
        failed.FailedAt.Should().Be(Now);
        failed.LastError.Should().Be(HubFailureMessage);
        await transport.Received(SingleAttempt).SendAsync(Arg.Any<string>(), Arg.Any<IntegrationEventEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_Should_KeepMessagePending_When_EventBusIsUnavailable()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await ClearOutboxAsync(cancellationToken);
        OutboxMessage message = CreateMessage(PoisonStream, Now);
        await SeedAsync([message], cancellationToken);
        IIntegrationEventTransport transport = Substitute.For<IIntegrationEventTransport>();
        transport.SendAsync(Arg.Any<string>(), Arg.Any<IntegrationEventEnvelope>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EventBusUnavailableException(UnavailableMessage));

        await RunCycleAsync(transport, cancellationToken);

        OutboxMessage pending = await ReadMessageAsync(message.Id, cancellationToken);
        pending.FailedAt.Should().BeNull();
        pending.ProcessedAt.Should().BeNull();
        pending.Attempts.Should().Be(NoAttempts);
    }

    [Fact]
    public async Task SendAsync_Should_NotDuplicateEvent_When_SameEnvelopeIsPublishedTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        OutboxMessage message = CreateMessage(IdempotencyStream, Now);
        IntegrationEventEnvelope envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(message.Payload, IntegrationEventJson.Options)!;
        await using EventBusPublisher publisher = CreatePublisher();

        long firstOffset = await publisher.SendAsync(IdempotencyStream, envelope, cancellationToken);
        long secondOffset = await publisher.SendAsync(IdempotencyStream, envelope, cancellationToken);

        secondOffset.Should().Be(firstOffset);
        IReadOnlyList<EventRecord> events = await ReadEventsAsync(IdempotencyStream, cancellationToken);
        events.Should().ContainSingle(record => record.Envelope.Id == message.Id);
    }

    private static OutboxMessage CreateMessage(string stream, DateTimeOffset occurredAt)
    {
        IntegrationEventEnvelope envelope = new()
        {
            Id = Guid.NewGuid(),
            Source = EventSource,
            Type = EventType,
            Subject = EventSubject,
            Time = occurredAt,
            Data = JsonSerializer.SerializeToElement(new Dictionary<string, string> { [DataProperty] = DataValue }),
        };
        string payload = JsonSerializer.Serialize(envelope, IntegrationEventJson.Options);
        return new OutboxMessage(envelope.Id, stream, EventType, EventSubject, payload, occurredAt);
    }

    private EventBusPublisher CreatePublisher()
    {
        EventBusClientOptions options = new();
        server.ConfigureClient(options);
        return new EventBusPublisher(new EventBusConnectionFactory(Options.Create(options)));
    }

    private async Task RunCycleAsync(IIntegrationEventTransport transport, CancellationToken cancellationToken)
    {
        await using OutboxRelayTestDbContext context = await database.CreateDbContextAsync<OutboxRelayTestDbContext>(cancellationToken);
        OutboxBatchProcessor<OutboxRelayTestDbContext> processor = new(
            context,
            transport,
            new FakeClock(Now),
            NullLogger<OutboxBatchProcessor<OutboxRelayTestDbContext>>.Instance);

        await processor.ProcessAsync(cancellationToken);
    }

    private async Task ClearOutboxAsync(CancellationToken cancellationToken)
    {
        await using OutboxRelayTestDbContext context = await database.CreateDbContextAsync<OutboxRelayTestDbContext>(cancellationToken);
        await context.Set<OutboxMessage>().ExecuteDeleteAsync(cancellationToken);
    }

    private async Task SeedAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken cancellationToken)
    {
        await using OutboxRelayTestDbContext context = await database.CreateDbContextAsync<OutboxRelayTestDbContext>(cancellationToken);
        foreach (OutboxMessage message in messages)
        {
            context.Set<OutboxMessage>().Add(message);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<OutboxMessage> ReadMessageAsync(Guid id, CancellationToken cancellationToken)
    {
        await using OutboxRelayTestDbContext context = await database.CreateDbContextAsync<OutboxRelayTestDbContext>(cancellationToken);
        return await context.Set<OutboxMessage>().AsNoTracking().SingleAsync(message => message.Id == id, cancellationToken);
    }

    private async Task<IReadOnlyList<EventRecord>> ReadEventsAsync(string stream, CancellationToken cancellationToken)
    {
        EventBusClientOptions options = new();
        server.ConfigureClient(options);
        await using HubConnection connection = new EventBusConnectionFactory(Options.Create(options)).Create();
        await connection.StartAsync(cancellationToken);
        return await connection.InvokeAsync<IReadOnlyList<EventRecord>>(
            EventBusHubMethods.GetEvents,
            new[] { stream },
            FromStart,
            ReadMax,
            cancellationToken);
    }
}
