using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

public sealed class RoutineRegisteredMapper : IIntegrationEventMapper<RoutineRegistered>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(RoutineRegistered domainEvent) =>
        [new MaintenanceRoutineRegisteredV1(RoutineSnapshotMapper.Map(domainEvent.Snapshot), domainEvent.OccurredAt)];
}
