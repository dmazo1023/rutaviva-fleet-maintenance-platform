using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed record AlertDto(
    AlertId AlertId,
    VehicleId VehicleId,
    string Plate,
    CustomerId CustomerId,
    RoutineId RoutineId,
    string RoutineName,
    int CycleNumber,
    AlertType Type,
    AlertTrigger TriggeredBy,
    AlertStatus Status,
    DateTimeOffset RaisedAt,
    DateTimeOffset? AcknowledgedAt,
    string? AcknowledgedBy,
    DateTimeOffset? ResolvedAt,
    AlertResolutionReason? ResolutionReason);
