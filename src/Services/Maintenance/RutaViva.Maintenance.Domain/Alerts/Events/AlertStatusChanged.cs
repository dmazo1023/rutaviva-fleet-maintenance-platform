using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Alerts.Events;

public sealed record AlertStatusChanged(
    AlertId AlertId,
    VehicleId VehicleId,
    LicensePlate Plate,
    CustomerId CustomerId,
    RoutineId RoutineId,
    AlertType Type,
    AlertTrigger Trigger,
    AlertStatus Status,
    DateTimeOffset? AcknowledgedAt,
    string? AcknowledgedBy,
    DateTimeOffset? ResolvedAt,
    AlertResolutionReason? ResolutionReason,
    DateTimeOffset OccurredAt) : IDomainEvent;
