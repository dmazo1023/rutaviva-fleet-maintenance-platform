namespace RutaViva.Fleet.Api.Contracts;

public sealed record RegisterVehicleRequest(
    string? Plate,
    string? Vin,
    string? Brand,
    string? Model,
    int ModelYear,
    long InitialOdometerKm,
    Guid CustomerId,
    Guid WorkshopId,
    Guid? ResponsibleApproverId);
