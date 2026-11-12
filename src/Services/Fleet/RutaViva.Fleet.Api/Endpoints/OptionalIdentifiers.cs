using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Api.Endpoints;

internal static class OptionalIdentifiers
{
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

    public static Result<ApproverId?> Approver(Guid? value)
    {
        if (value is not { } guid)
        {
            return Result<ApproverId?>.Success(null);
        }

        Result<ApproverId> created = ApproverId.Create(guid);
        if (created.IsFailure)
        {
            return created.Error;
        }

        return Result<ApproverId?>.Success(created.Value);
    }
}
