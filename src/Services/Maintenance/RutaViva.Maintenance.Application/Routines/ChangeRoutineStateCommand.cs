using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Routines;

public sealed record ChangeRoutineStateCommand(
    VehicleId VehicleId,
    RoutineId RoutineId,
    RoutineState State,
    string UpdatedBy) : ICommand<RoutineId>;
