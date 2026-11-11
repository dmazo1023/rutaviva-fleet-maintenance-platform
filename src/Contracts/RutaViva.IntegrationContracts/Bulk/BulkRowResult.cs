namespace RutaViva.IntegrationContracts.Bulk;

public sealed record BulkRowResult(int RowNumber, BulkRowOutcome Outcome, IReadOnlyList<BulkRowError> Errors);
