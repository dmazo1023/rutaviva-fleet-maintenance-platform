using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Plans;

public sealed record CreateMaintenancePlanCommand(
    VehicleId VehicleId,
    string Plate,
    CustomerId CustomerId,
    MaintenancePlanStatus Status,
    long OdometerKm,
    DateTimeOffset RegisteredAt) : ICommand<VehicleId>;
