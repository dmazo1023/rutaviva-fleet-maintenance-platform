using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Plans.Events;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed class RaiseDueSoonAlertHandler(IAlertRepository alerts) : IDomainEventHandler<RoutineBecameDueSoon>
{
    public Task HandleAsync(RoutineBecameDueSoon domainEvent, CancellationToken cancellationToken)
    {
        if (!domainEvent.IsFirstNoticeInCycle)
        {
            return Task.CompletedTask;
        }

        Alert alert = Alert.Raise(
            AlertOriginFactory.From(domainEvent.Snapshot),
            AlertType.DueSoon,
            domainEvent.Trigger,
            domainEvent.OccurredAt);
        alerts.Add(alert);
        return Task.CompletedTask;
    }
}
