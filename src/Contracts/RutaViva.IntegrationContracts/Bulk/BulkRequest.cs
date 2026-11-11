namespace RutaViva.IntegrationContracts.Bulk;

public sealed record BulkRequest<TRow>(IReadOnlyList<TRow> Rows);
