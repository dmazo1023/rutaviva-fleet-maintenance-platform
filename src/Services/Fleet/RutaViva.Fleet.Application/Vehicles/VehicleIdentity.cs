using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Application.Vehicles;

internal sealed record VehicleIdentity(Plate Plate, Vin? Vin, VehicleDescription Description, Kilometers Odometer);
