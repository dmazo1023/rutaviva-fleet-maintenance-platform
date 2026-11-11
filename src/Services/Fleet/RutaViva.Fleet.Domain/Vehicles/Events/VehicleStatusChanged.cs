using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Domain.Vehicles.Events;

public sealed record VehicleStatusChanged(
    VehicleId VehicleId,
    Plate Plate,
    CustomerId CustomerId,
    VehicleStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;
