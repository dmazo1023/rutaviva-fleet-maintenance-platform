using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

public sealed class MaintenancePlanStatusChangedMapper : IIntegrationEventMapper<MaintenancePlanStatusChanged>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(MaintenancePlanStatusChanged domainEvent) =>
        [
            new MaintenancePlanStatusChangedV1(
                domainEvent.VehicleId.Value,
                domainEvent.Plate.Value,
                domainEvent.CustomerId.Value,
                domainEvent.Status.ToString(),
                domainEvent.OccurredAt)
        ];
}
