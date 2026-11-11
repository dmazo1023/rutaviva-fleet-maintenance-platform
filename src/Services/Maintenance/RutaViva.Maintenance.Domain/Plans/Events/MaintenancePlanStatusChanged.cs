using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans.Events;

public sealed record MaintenancePlanStatusChanged(
    VehicleId VehicleId,
    LicensePlate Plate,
    CustomerId CustomerId,
    MaintenancePlanStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;
