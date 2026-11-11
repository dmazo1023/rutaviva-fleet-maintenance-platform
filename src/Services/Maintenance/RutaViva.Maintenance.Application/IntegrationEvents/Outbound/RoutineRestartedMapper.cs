using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Plans.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

public sealed class RoutineRestartedMapper : IIntegrationEventMapper<RoutineRestarted>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(RoutineRestarted domainEvent) =>
        [
            new MaintenanceRoutineRestartedV1(
                RoutineSnapshotMapper.Map(domainEvent.Snapshot),
                domainEvent.ServiceOrderId,
                domainEvent.Service.Kilometers,
                ServicedAtOf(domainEvent.Service),
                domainEvent.OccurredAt)
        ];

    private static DateTimeOffset ServicedAtOf(ServiceRecord service) =>
        new(service.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
