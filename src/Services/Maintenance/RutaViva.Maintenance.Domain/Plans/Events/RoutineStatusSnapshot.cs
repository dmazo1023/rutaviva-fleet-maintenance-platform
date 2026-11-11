using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans.Events;

public sealed record RoutineStatusSnapshot(
    RoutineId RoutineId,
    VehicleId VehicleId,
    LicensePlate Plate,
    CustomerId CustomerId,
    RoutineTypeCode RoutineTypeCode,
    string RoutineName,
    RoutineState State,
    RoutineDueStatus DueStatus,
    int CycleNumber,
    long CycleStartKm,
    DateOnly CycleStartDate,
    int? IntervalKm,
    int? IntervalMonths,
    decimal EffectiveDueSoonPercent,
    NextDue NextDue,
    long OdometerKm,
    decimal ConsumedPercent);
