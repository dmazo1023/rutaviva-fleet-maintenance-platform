namespace RutaViva.IntegrationContracts.Bulk;

public sealed record WorkshopBulkRow(int RowNumber, string TaxId, string Name, string Email, string Phone, string Address, string City);
