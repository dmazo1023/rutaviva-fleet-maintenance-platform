using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Application.Vehicles;

internal static class VehicleIdentityFactory
{
    public static Result<VehicleIdentity> Create(RegisterVehicleCommand command, int currentYear)
    {
        Result<Plate> plate = Plate.Create(command.Plate);
        if (plate.IsFailure)
        {
            return plate.Error;
        }

        Result<Vin?> vin = ParseVin(command.Vin);
        if (vin.IsFailure)
        {
            return vin.Error;
        }

        Result<VehicleDescription> description = VehicleDescription.Create(command.Brand, command.Model, command.ModelYear, currentYear);
        if (description.IsFailure)
        {
            return description.Error;
        }

        Result<Kilometers> odometer = Kilometers.Create(command.InitialOdometerKm);
        if (odometer.IsFailure)
        {
            return odometer.Error;
        }

        return new VehicleIdentity(plate.Value, vin.Value, description.Value, odometer.Value);
    }

    private static Result<Vin?> ParseVin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Vin?>.Success(null);
        }

        Result<Vin> vin = Vin.Create(value);
        if (vin.IsFailure)
        {
            return vin.Error;
        }

        return Result<Vin?>.Success(vin.Value);
    }
}
