using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Api.Contracts;

public sealed record ChangeVehicleStatusRequest(VehicleStatus? Status);
