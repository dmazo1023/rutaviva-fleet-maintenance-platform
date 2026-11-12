using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed record GetVehicleDetailQuery(VehicleId VehicleId) : IQuery<VehicleDetailDto>;
