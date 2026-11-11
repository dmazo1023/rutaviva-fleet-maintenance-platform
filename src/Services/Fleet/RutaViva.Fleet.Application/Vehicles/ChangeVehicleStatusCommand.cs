using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.Vehicles;

public sealed record ChangeVehicleStatusCommand(VehicleId VehicleId, VehicleStatus Target) : ICommand<VehicleId>;
