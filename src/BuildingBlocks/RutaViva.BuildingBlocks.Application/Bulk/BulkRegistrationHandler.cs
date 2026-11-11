using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Bulk;

namespace RutaViva.BuildingBlocks.Application.Bulk;

public abstract class BulkRegistrationHandler<TRow, TCommand>(ICommandHandler<TCommand, Guid> commandHandler)
    where TCommand : ICommand<Guid>
{
    private const string DuplicateInBatchMessage = "The row repeats a natural key already present in the batch.";

    public async Task<IReadOnlyList<BulkRowResult>> ExecuteAsync(IReadOnlyList<TRow> rows, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);

        HashSet<string> seenKeys = new(StringComparer.OrdinalIgnoreCase);
        List<BulkRowResult> results = new(rows.Count);
        foreach (TRow row in rows)
        {
            results.Add(await ProcessRowAsync(row, seenKeys, cancellationToken));
        }

        return results;
    }

    protected abstract int RowNumberOf(TRow row);

    protected abstract string NaturalKey(TRow row);

    protected abstract Task<Result<TCommand>> ResolveCommandAsync(TRow row, CancellationToken cancellationToken);

    protected abstract Task<bool> ExistsAsync(string naturalKey, CancellationToken cancellationToken);

    private static BulkRowResult Accepted(int rowNumber, BulkRowOutcome outcome) => new(rowNumber, outcome, []);

    private static BulkRowResult Rejected(int rowNumber, string code, string message) =>
        new(rowNumber, BulkRowOutcome.Invalid, [new BulkRowError(code, message)]);

    private static BulkRowResult Rejected(int rowNumber, Error error) => Rejected(rowNumber, error.Code, error.Message);

    private async Task<BulkRowResult> ProcessRowAsync(TRow row, HashSet<string> seenKeys, CancellationToken cancellationToken)
    {
        int rowNumber = RowNumberOf(row);
        string naturalKey = NaturalKey(row);
        if (!seenKeys.Add(naturalKey))
        {
            return Rejected(rowNumber, BulkErrorCodes.DuplicateInBatch, DuplicateInBatchMessage);
        }

        if (await ExistsAsync(naturalKey, cancellationToken))
        {
            return Accepted(rowNumber, BulkRowOutcome.Skipped);
        }

        return await RegisterAsync(row, rowNumber, cancellationToken);
    }

    private async Task<BulkRowResult> RegisterAsync(TRow row, int rowNumber, CancellationToken cancellationToken)
    {
        Result<TCommand> resolved = await ResolveCommandAsync(row, cancellationToken);
        if (resolved.IsFailure)
        {
            return Rejected(rowNumber, resolved.Error);
        }

        Result<Guid> registered = await commandHandler.HandleAsync(resolved.Value, cancellationToken);
        if (registered.IsFailure)
        {
            return Rejected(rowNumber, registered.Error);
        }

        return Accepted(rowNumber, BulkRowOutcome.Valid);
    }
}
