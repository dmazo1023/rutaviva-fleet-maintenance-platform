using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.Vehicles;

public sealed record RegisterVehicleCommand(
    string Plate,
    string? Vin,
    string Brand,
    string Model,
    int ModelYear,
    long InitialOdometerKm,
    CustomerId CustomerId,
    WorkshopId WorkshopId,
    ApproverId? ResponsibleApproverId) : ICommand<Guid>;
