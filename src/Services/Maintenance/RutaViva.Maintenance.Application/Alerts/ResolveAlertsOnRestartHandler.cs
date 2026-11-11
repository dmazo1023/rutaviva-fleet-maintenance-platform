using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed class ResolveAlertsOnRestartHandler(IAlertRepository alerts) : IDomainEventHandler<RoutineRestarted>
{
    public Task HandleAsync(RoutineRestarted domainEvent, CancellationToken cancellationToken) =>
        OpenAlertResolution.ResolveAsync(
            alerts,
            domainEvent.Snapshot.RoutineId,
            null,
            AlertResolutionReason.RoutineRestarted,
            domainEvent.OccurredAt,
            cancellationToken);
}
