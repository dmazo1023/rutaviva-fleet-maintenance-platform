using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

public sealed class RoutineBecameOverdueMapper : IIntegrationEventMapper<RoutineBecameOverdue>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(RoutineBecameOverdue domainEvent) =>
        [
            new MaintenanceRoutineOverdueV1(
                RoutineSnapshotMapper.Map(domainEvent.Snapshot),
                domainEvent.Trigger.ToString(),
                domainEvent.OccurredAt)
        ];
}
