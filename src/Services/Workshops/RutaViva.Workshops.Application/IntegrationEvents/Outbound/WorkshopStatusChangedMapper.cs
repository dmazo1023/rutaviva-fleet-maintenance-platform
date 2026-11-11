using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Workshops;
using RutaViva.Workshops.Domain.Events;

namespace RutaViva.Workshops.Application.IntegrationEvents.Outbound;

public sealed class WorkshopStatusChangedMapper : IIntegrationEventMapper<WorkshopStatusChanged>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(WorkshopStatusChanged domainEvent) =>
        [
            new WorkshopStatusChangedV1(
                domainEvent.WorkshopId.Value,
                domainEvent.Status.ToString(),
                domainEvent.OccurredAt)
        ];
}
