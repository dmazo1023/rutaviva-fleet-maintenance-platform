using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Routines;

public sealed record UpdateRoutineParametersCommand(
    VehicleId VehicleId,
    RoutineId RoutineId,
    int? IntervalKm,
    int? IntervalMonths,
    decimal? DueSoonPercent,
    string UpdatedBy) : ICommand<RoutineId>;
