using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Application.Plans;

public sealed record GetMaintenancePlanQuery(VehicleId VehicleId) : IQuery<MaintenancePlanDetailDto>;
