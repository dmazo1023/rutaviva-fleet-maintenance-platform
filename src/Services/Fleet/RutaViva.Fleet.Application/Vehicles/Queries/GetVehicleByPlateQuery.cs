using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed record GetVehicleByPlateQuery(string Plate) : IQuery<VehicleDetailDto>;
