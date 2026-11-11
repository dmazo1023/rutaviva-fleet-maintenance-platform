using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Infrastructure.Persistence.Queries;

internal sealed record RoutineReadRow(
    VehicleId VehicleId,
    RoutineId RoutineId,
    RoutineTypeCode TypeCode,
    string Name,
    RoutineState State,
    RoutineDueStatus DueStatus,
    AlertTrigger? TriggeredBy,
    int CycleNumber,
    long CycleStartKm,
    DateOnly CycleStartDate,
    int? OverrideIntervalKm,
    int? OverrideIntervalMonths,
    decimal? OverrideDueSoonPercent,
    int? TypeIntervalKm,
    int? TypeIntervalMonths,
    DueSoonThreshold TypeThreshold);
