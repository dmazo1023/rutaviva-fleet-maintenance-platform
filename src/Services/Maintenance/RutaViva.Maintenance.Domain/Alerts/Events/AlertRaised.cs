using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Alerts.Events;

public sealed record AlertRaised(
    AlertId AlertId,
    VehicleId VehicleId,
    LicensePlate Plate,
    CustomerId CustomerId,
    RoutineId RoutineId,
    string RoutineName,
    int CycleNumber,
    AlertType Type,
    AlertTrigger Trigger,
    AlertStatus Status,
    DateTimeOffset RaisedAt,
    DateTimeOffset OccurredAt) : IDomainEvent;
