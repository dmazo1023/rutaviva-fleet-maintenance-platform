using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Customers.Api.Contracts;

namespace RutaViva.Customers.Api.Endpoints;

internal static class CreatedResultExtensions
{
    public static IResult ToCreatedResult(this Result<Guid> result, Func<Guid, string> locationOf)
    {
        if (result.IsFailure)
        {
            return result.ToHttpResult();
        }

        return TypedResults.Created(locationOf(result.Value), new CreatedResponse(result.Value));
    }
}
