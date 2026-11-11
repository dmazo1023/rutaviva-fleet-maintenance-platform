using Microsoft.AspNetCore.Http;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.Api.Results;

public static class ResultExtensions
{
    private const string CodeExtension = "code";

    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsFailure ? ToProblem(result.Error) : TypedResults.NoContent();
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsFailure ? ToProblem(result.Error) : TypedResults.Ok(result.Value);
    }

    private static IResult ToProblem(Error error)
    {
        return TypedResults.Problem(
            detail: error.Message,
            statusCode: MapStatusCode(error.Type),
            title: error.Type.ToString(),
            extensions: new Dictionary<string, object?> { [CodeExtension] = error.Code });
    }

    private static int MapStatusCode(ErrorType type)
    {
        return type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
