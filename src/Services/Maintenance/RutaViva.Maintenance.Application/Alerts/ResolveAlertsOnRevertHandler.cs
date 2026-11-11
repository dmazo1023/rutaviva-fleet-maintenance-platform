using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed class ResolveAlertsOnRevertHandler(IAlertRepository alerts) : IDomainEventHandler<RoutineDueStatusReverted>
{
    public Task HandleAsync(RoutineDueStatusReverted domainEvent, CancellationToken cancellationToken) =>
        OpenAlertResolution.ResolveAsync(
            alerts,
            domainEvent.Snapshot.RoutineId,
            AlertType.DueSoon,
            AlertResolutionReason.ParametersChanged,
            domainEvent.OccurredAt,
            cancellationToken);
}
