namespace RutaViva.IntegrationContracts.Bulk;

public sealed record ApproverBulkRow(int RowNumber, string CustomerTaxId, string FullName, string Email);
