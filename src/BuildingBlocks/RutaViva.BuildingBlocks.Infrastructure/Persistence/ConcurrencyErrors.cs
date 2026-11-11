using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.Infrastructure.Persistence;

public static class ConcurrencyErrors
{
    private const string ConflictCode = "Concurrency.Conflict";
    private const string ConflictMessage = "The record was modified by another process. Reload it and try again.";

    public static readonly Error Conflict = new(ConflictCode, ConflictMessage, ErrorType.Conflict);
}
