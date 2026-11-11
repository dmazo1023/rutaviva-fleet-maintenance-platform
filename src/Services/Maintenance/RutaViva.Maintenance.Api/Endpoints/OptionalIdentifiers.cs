using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Api.Endpoints;

internal static class OptionalIdentifiers
{
    public static Result<VehicleId?> Vehicle(Guid? value)
    {
        if (value is not { } guid)
        {
            return Result<VehicleId?>.Success(null);
        }

        Result<VehicleId> created = VehicleId.Create(guid);
        if (created.IsFailure)
        {
            return created.Error;
        }

        return Result<VehicleId?>.Success(created.Value);
    }

    public static Result<CustomerId?> Customer(Guid? value)
    {
        if (value is not { } guid)
        {
            return Result<CustomerId?>.Success(null);
        }

        Result<CustomerId> created = CustomerId.Create(guid);
        if (created.IsFailure)
        {
            return created.Error;
        }

        return Result<CustomerId?>.Success(created.Value);
    }
}
