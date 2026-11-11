using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.RoutineTypes;

public sealed record UpdateRoutineTypeParametersCommand(
    RoutineTypeId RoutineTypeId,
    int? IntervalKm,
    int? IntervalMonths,
    decimal DueSoonPercent,
    string UpdatedBy) : ICommand<RoutineTypeId>;
