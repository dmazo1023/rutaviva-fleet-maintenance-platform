using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Domain.Vehicles.Events;

public sealed record VehicleRegistered(
    VehicleId VehicleId,
    Plate Plate,
    Vin? Vin,
    VehicleDescription Description,
    CustomerId CustomerId,
    WorkshopId WorkshopId,
    ApproverId? ResponsibleApproverId,
    VehicleStatus Status,
    Kilometers InitialOdometer,
    DateTimeOffset RegisteredAt,
    DateTimeOffset OccurredAt) : IDomainEvent;
