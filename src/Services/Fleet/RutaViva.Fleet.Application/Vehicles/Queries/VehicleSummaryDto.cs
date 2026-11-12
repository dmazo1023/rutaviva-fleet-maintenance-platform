using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed record VehicleSummaryDto(
    VehicleId Id,
    string Plate,
    string? Vin,
    string Brand,
    string Model,
    int ModelYear,
    VehicleStatus Status,
    DateTimeOffset StatusChangedAt,
    CustomerId CustomerId,
    string OwnerName,
    WorkshopId WorkshopId,
    string WorkshopName,
    ApproverId? ResponsibleApproverId,
    string? ResponsibleName);
