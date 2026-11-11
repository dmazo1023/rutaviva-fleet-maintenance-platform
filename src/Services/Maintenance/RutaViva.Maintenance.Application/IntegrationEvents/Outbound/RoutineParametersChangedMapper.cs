using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

public sealed class RoutineParametersChangedMapper : IIntegrationEventMapper<RoutineParametersChanged>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(RoutineParametersChanged domainEvent) =>
        [new MaintenanceRoutineParametersChangedV1(RoutineSnapshotMapper.Map(domainEvent.Snapshot), domainEvent.OccurredAt)];
}
