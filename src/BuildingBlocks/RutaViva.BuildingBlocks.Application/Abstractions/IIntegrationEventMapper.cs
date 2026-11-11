using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.Application.Abstractions;

public interface IIntegrationEventMapper<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    IReadOnlyCollection<IIntegrationEvent> Map(TDomainEvent domainEvent);
}
