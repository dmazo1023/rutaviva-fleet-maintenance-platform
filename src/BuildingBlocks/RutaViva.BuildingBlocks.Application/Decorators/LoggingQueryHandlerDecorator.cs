using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.Application.Decorators;

public sealed class LoggingQueryHandlerDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    ILogger<LoggingQueryHandlerDecorator<TQuery, TResult>> logger) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private const string StartedTemplate = "Query {QueryName} started";
    private const string SucceededTemplate = "Query {QueryName} succeeded";
    private const string FailedTemplate = "Query {QueryName} failed with {ErrorCode} ({ErrorType})";

    private static readonly string QueryName = typeof(TQuery).Name;

    public async Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation(StartedTemplate, QueryName);
        Result<TResult> result = await inner.HandleAsync(query, cancellationToken);
        LogOutcome(result);
        return result;
    }

    private void LogOutcome(Result<TResult> result)
    {
        if (result.IsSuccess)
        {
            logger.LogInformation(SucceededTemplate, QueryName);
            return;
        }

        logger.LogWarning(FailedTemplate, QueryName, result.Error.Code, result.Error.Type);
    }
}
