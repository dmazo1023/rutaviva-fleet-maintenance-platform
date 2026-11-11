using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.DomainEvents;

public sealed class ParentRegisteredMapper : IIntegrationEventMapper<ParentRegistered>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(ParentRegistered domainEvent)
        => [new ParentRegisteredV1(domainEvent.ParentId)];
}
