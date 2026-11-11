using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed class ResolveAlertsOnDeactivationHandler(IAlertRepository alerts) : IDomainEventHandler<RoutineStateChanged>
{
    public Task HandleAsync(RoutineStateChanged domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.Snapshot.State != RoutineState.Inactive)
        {
            return Task.CompletedTask;
        }

        return OpenAlertResolution.ResolveAsync(
            alerts,
            domainEvent.Snapshot.RoutineId,
            null,
            AlertResolutionReason.RoutineDeactivated,
            domainEvent.OccurredAt,
            cancellationToken);
    }
}
