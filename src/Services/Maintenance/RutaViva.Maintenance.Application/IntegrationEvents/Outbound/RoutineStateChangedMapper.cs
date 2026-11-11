using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

public sealed class RoutineStateChangedMapper : IIntegrationEventMapper<RoutineStateChanged>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(RoutineStateChanged domainEvent) =>
        [new MaintenanceRoutineStateChangedV1(RoutineSnapshotMapper.Map(domainEvent.Snapshot), domainEvent.OccurredAt)];
}
