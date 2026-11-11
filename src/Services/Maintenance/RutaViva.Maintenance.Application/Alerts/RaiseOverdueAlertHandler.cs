using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed class RaiseOverdueAlertHandler(IAlertRepository alerts) : IDomainEventHandler<RoutineBecameOverdue>
{
    public async Task HandleAsync(RoutineBecameOverdue domainEvent, CancellationToken cancellationToken)
    {
        await OpenAlertResolution.ResolveAsync(
            alerts,
            domainEvent.Snapshot.RoutineId,
            AlertType.DueSoon,
            AlertResolutionReason.EscalatedToOverdue,
            domainEvent.OccurredAt,
            cancellationToken);

        Alert alert = Alert.Raise(
            AlertOriginFactory.From(domainEvent.Snapshot),
            AlertType.Overdue,
            domainEvent.Trigger,
            domainEvent.OccurredAt);
        alerts.Add(alert);
    }
}
