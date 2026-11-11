using RutaViva.BuildingBlocks.Api.Results;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Maintenance.Api.Endpoints;

internal static class NoContentResultExtensions
{
    public static IResult ToNoContentResult<T>(this Result<T> result)
    {
        Result outcome = result;
        return outcome.ToHttpResult();
    }
}
