using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

public sealed class RoutineDueStatusRevertedMapper : IIntegrationEventMapper<RoutineDueStatusReverted>
{
    private const AlertResolutionReason RevertReason = AlertResolutionReason.ParametersChanged;

    public IReadOnlyCollection<IIntegrationEvent> Map(RoutineDueStatusReverted domainEvent) =>
        [
            new MaintenanceRoutineDueStatusRevertedV1(
                RoutineSnapshotMapper.Map(domainEvent.Snapshot),
                RevertReason.ToString(),
                domainEvent.OccurredAt)
        ];
}
