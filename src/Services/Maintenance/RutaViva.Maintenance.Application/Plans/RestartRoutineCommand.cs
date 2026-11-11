using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Plans;

public sealed record RestartRoutineCommand(
    VehicleId VehicleId,
    RoutineId RoutineId,
    long ServicedKm,
    DateTimeOffset ServicedAt,
    Guid ServiceOrderId) : ICommand<RoutineId>;
