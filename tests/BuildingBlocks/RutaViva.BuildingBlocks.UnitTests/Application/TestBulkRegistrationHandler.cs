using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class TestBulkRegistrationHandler(
    ICommandHandler<BulkCommand, Guid> commandHandler,
    IReadOnlySet<string> existingKeys,
    Error? resolveError = null) : BulkRegistrationHandler<BulkRow, BulkCommand>(commandHandler)
{
    protected override int RowNumberOf(BulkRow row) => row.RowNumber;

    protected override string NaturalKey(BulkRow row) => row.Code;

    protected override Task<Result<BulkCommand>> ResolveCommandAsync(BulkRow row, CancellationToken cancellationToken)
    {
        if (resolveError is not null)
        {
            return Task.FromResult(Result<BulkCommand>.Failure(resolveError));
        }

        return Task.FromResult(Result<BulkCommand>.Success(new BulkCommand(row.Code)));
    }

    protected override Task<bool> ExistsAsync(string naturalKey, CancellationToken cancellationToken) =>
        Task.FromResult(existingKeys.Contains(naturalKey));
}
