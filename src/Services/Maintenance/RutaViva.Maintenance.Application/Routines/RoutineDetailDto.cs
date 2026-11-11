using RutaViva.Maintenance.Domain.Alerts;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Routines;

public sealed record RoutineDetailDto(
    RoutineId RoutineId,
    string RoutineTypeCode,
    string Name,
    RoutineState State,
    RoutineDueStatus DueStatus,
    AlertTrigger? TriggeredBy,
    int CycleNumber,
    long CycleStartKm,
    DateOnly CycleStartDate,
    int? IntervalKm,
    ParameterSource IntervalKmSource,
    int? IntervalMonths,
    ParameterSource IntervalMonthsSource,
    decimal DueSoonPercent,
    ParameterSource DueSoonPercentSource,
    long? NextDueKm,
    DateOnly? NextDueDate,
    decimal ConsumedPercent,
    RoutineTrackDto? DistanceTrack,
    RoutineTrackDto? TimeTrack);
