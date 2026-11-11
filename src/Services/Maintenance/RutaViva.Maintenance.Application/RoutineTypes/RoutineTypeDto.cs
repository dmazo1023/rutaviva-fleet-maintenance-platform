using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.RoutineTypes;

namespace RutaViva.Maintenance.Application.RoutineTypes;

public sealed record RoutineTypeDto(
    RoutineTypeId Id,
    string Code,
    string Name,
    int? DefaultIntervalKm,
    int? DefaultIntervalMonths,
    decimal DueSoonPercent,
    RoutineTypeStatus Status,
    string UpdatedBy,
    DateTimeOffset UpdatedAt);
