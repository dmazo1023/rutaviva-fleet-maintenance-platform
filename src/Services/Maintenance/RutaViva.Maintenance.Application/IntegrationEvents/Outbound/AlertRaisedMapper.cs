using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Alerts.Events;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

public sealed class AlertRaisedMapper : IIntegrationEventMapper<AlertRaised>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(AlertRaised domainEvent) =>
        [
            new AlertRaisedV1(
                domainEvent.AlertId.Value,
                domainEvent.VehicleId.Value,
                domainEvent.Plate.Value,
                domainEvent.CustomerId.Value,
                domainEvent.RoutineId.Value,
                domainEvent.RoutineName,
                domainEvent.CycleNumber,
                domainEvent.Type.ToString(),
                domainEvent.Trigger.ToString(),
                domainEvent.Status.ToString(),
                domainEvent.RaisedAt,
                domainEvent.OccurredAt)
        ];
}
