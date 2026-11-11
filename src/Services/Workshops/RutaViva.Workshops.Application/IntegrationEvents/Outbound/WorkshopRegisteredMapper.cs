using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Workshops;
using RutaViva.Workshops.Domain.Events;

namespace RutaViva.Workshops.Application.IntegrationEvents.Outbound;

public sealed class WorkshopRegisteredMapper : IIntegrationEventMapper<WorkshopRegistered>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(WorkshopRegistered domainEvent) =>
        [
            new WorkshopRegisteredV1(
                domainEvent.WorkshopId.Value,
                domainEvent.TaxId.Value,
                domainEvent.Name,
                domainEvent.City,
                domainEvent.Status.ToString(),
                domainEvent.OccurredAt)
        ];
}
