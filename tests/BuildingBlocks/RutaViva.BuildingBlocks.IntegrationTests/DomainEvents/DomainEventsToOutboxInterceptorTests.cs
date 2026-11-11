using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.Testing.Fixtures;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

[Trait("Category", "Integration")]
public sealed class DomainEventsToOutboxInterceptorTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChangesAsync_Should_PersistAggregatesAndOutboxRow_When_HandlerCreatesAnotherAggregate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid parentId = Guid.NewGuid();
        string subject = parentId.ToString();
        await using DomainEventsTestHost host = await DomainEventsTestHost.CreateAsync(database, cancellationToken);
        await using AsyncServiceScope scope = host.CreateScope();
        EventsTestDbContext context = scope.ServiceProvider.GetRequiredService<EventsTestDbContext>();
        TestParent parent = TestParent.Register(parentId, Now);
        context.Parents.Add(parent);

        await context.SaveChangesAsync(cancellationToken);

        await using EventsTestDbContext reader = await database.CreateDbContextAsync<EventsTestDbContext>(cancellationToken);
        (await reader.Parents.AnyAsync(item => item.Id == parentId, cancellationToken)).Should().BeTrue();
        (await reader.Children.AnyAsync(item => item.Id == parentId, cancellationToken)).Should().BeTrue();
        OutboxMessage message = await reader.Set<OutboxMessage>().SingleAsync(item => item.Subject == subject, cancellationToken);
        message.Stream.Should().Be(ParentRegisteredV1.Stream);
        parent.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_Should_PersistNothing_When_TransactionFails()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid parentId = Guid.NewGuid();
        Guid duplicatedChildId = Guid.NewGuid();
        string subject = parentId.ToString();
        await SeedChildAsync(duplicatedChildId, cancellationToken);
        await using DomainEventsTestHost host = await DomainEventsTestHost.CreateAsync(database, cancellationToken);
        await using AsyncServiceScope scope = host.CreateScope();
        EventsTestDbContext context = scope.ServiceProvider.GetRequiredService<EventsTestDbContext>();
        context.Parents.Add(TestParent.Register(parentId, Now));
        context.Children.Add(new TestChild(duplicatedChildId));

        Func<Task> save = () => context.SaveChangesAsync(cancellationToken);

        await save.Should().ThrowAsync<DbUpdateException>();
        await using EventsTestDbContext reader = await database.CreateDbContextAsync<EventsTestDbContext>(cancellationToken);
        (await reader.Parents.AnyAsync(item => item.Id == parentId, cancellationToken)).Should().BeFalse();
        (await reader.Children.AnyAsync(item => item.Id == parentId, cancellationToken)).Should().BeFalse();
        (await reader.Set<OutboxMessage>().AnyAsync(item => item.Subject == subject, cancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task SaveChangesAsync_Should_ThrowInvalidOperation_When_EventCycleExceedsMaxRounds()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DomainEventsTestHost host = await DomainEventsTestHost.CreateAsync(database, cancellationToken);
        await using AsyncServiceScope scope = host.CreateScope();
        EventsTestDbContext context = scope.ServiceProvider.GetRequiredService<EventsTestDbContext>();
        context.Loops.Add(TestLoop.Start(Guid.NewGuid(), Now));

        Func<Task> save = () => context.SaveChangesAsync(cancellationToken);

        await save.Should().ThrowAsync<InvalidOperationException>();
    }

    private async Task SeedChildAsync(Guid childId, CancellationToken cancellationToken)
    {
        await using EventsTestDbContext seedContext = await database.CreateDbContextAsync<EventsTestDbContext>(cancellationToken);
        seedContext.Children.Add(new TestChild(childId));
        await seedContext.SaveChangesAsync(cancellationToken);
    }
}
