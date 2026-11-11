using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Maintenance.Application.Telemetry;

public static class SingleRetryOnConcurrencyConflict
{
    private const string ConflictCode = "Concurrency.Conflict";

    public static async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken)
    {
        Result<T> firstAttempt = await operation(cancellationToken);
        if (!IsConflict(firstAttempt))
        {
            return firstAttempt;
        }

        return await operation(cancellationToken);
    }

    private static bool IsConflict<T>(Result<T> result) =>
        result.IsFailure && result.Error.Code == ConflictCode;
}
