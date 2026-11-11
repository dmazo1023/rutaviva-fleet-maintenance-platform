using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed class SpawnLoopOnLoopTriggeredHandler(EventsTestDbContext context) : IDomainEventHandler<LoopTriggered>
{
    public Task HandleAsync(LoopTriggered domainEvent, CancellationToken cancellationToken)
    {
        context.Loops.Add(TestLoop.Start(Guid.NewGuid(), domainEvent.OccurredAt));
        return Task.CompletedTask;
    }
}
