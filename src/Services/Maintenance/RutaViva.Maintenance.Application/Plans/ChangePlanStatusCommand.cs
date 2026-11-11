using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;

namespace RutaViva.Maintenance.Application.Plans;

public sealed record ChangePlanStatusCommand(VehicleId VehicleId, MaintenancePlanStatus TargetStatus)
    : ICommand<VehicleId>;
