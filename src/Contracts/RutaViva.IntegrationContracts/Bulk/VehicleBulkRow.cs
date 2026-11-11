namespace RutaViva.IntegrationContracts.Bulk;

public sealed record VehicleBulkRow(
    int RowNumber,
    string Plate,
    string? Vin,
    string Brand,
    string Model,
    int ModelYear,
    string OwnerTaxId,
    string WorkshopTaxId,
    string? ResponsibleApproverEmail,
    long? InitialOdometerKm);
