using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed record VehicleDetailDto(
    VehicleId Id,
    string Plate,
    string? Vin,
    string Brand,
    string Model,
    int ModelYear,
    long InitialOdometerKm,
    VehicleStatus Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset StatusChangedAt,
    VehicleOwnerDto Owner,
    VehicleWorkshopDto Workshop,
    VehicleResponsibleDto? Responsible);
