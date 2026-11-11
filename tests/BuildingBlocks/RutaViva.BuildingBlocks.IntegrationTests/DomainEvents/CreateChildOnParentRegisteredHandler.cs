using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed class CreateChildOnParentRegisteredHandler(EventsTestDbContext context) : IDomainEventHandler<ParentRegistered>
{
    public Task HandleAsync(ParentRegistered domainEvent, CancellationToken cancellationToken)
    {
        context.Children.Add(new TestChild(domainEvent.ParentId));
        return Task.CompletedTask;
    }
}
