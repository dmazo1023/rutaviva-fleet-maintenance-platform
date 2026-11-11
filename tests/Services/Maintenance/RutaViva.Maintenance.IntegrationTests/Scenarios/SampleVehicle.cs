namespace RutaViva.Maintenance.IntegrationTests.Scenarios;

internal sealed record SampleVehicle(
    string Plate,
    string? Vin,
    string Brand,
    string Model,
    int ModelYear,
    string OwnerTaxId,
    string WorkshopTaxId,
    long OdometerKm);
