namespace RutaViva.IntegrationContracts.Bulk;

public sealed record CustomerBulkRow(int RowNumber, string TaxId, string Name, string Email, string Phone);
