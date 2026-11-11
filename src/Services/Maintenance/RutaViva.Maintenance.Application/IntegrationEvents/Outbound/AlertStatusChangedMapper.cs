using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.Maintenance.Domain.Alerts.Events;

namespace RutaViva.Maintenance.Application.IntegrationEvents.Outbound;

public sealed class AlertStatusChangedMapper : IIntegrationEventMapper<AlertStatusChanged>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(AlertStatusChanged domainEvent) =>
        [
            new AlertStatusChangedV1(
                domainEvent.AlertId.Value,
                domainEvent.VehicleId.Value,
                domainEvent.Plate.Value,
                domainEvent.CustomerId.Value,
                domainEvent.RoutineId.Value,
                domainEvent.Type.ToString(),
                domainEvent.Trigger.ToString(),
                domainEvent.Status.ToString(),
                domainEvent.AcknowledgedAt,
                domainEvent.AcknowledgedBy,
                domainEvent.ResolvedAt,
                domainEvent.ResolutionReason?.ToString(),
                domainEvent.OccurredAt)
        ];
}
