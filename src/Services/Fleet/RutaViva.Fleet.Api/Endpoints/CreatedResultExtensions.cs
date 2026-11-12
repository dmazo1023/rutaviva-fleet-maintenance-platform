using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Api.Contracts;

namespace RutaViva.Fleet.Api.Endpoints;

internal static class CreatedResultExtensions
{
    private const string RouteSeparator = "/";

    public static IResult ToCreatedResult(this Result<Guid> result, string collectionRoute)
    {
        if (result.IsFailure)
        {
            return result.ToHttpResult();
        }

        return TypedResults.Created(collectionRoute + RouteSeparator + result.Value, new RegisteredVehicleResponse(result.Value));
    }
}
