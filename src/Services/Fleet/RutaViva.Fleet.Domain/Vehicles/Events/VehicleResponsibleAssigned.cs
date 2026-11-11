using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Domain.Vehicles.Events;

public sealed record VehicleResponsibleAssigned(
    VehicleId VehicleId,
    ApproverId? ResponsibleApproverId,
    DateTimeOffset OccurredAt) : IDomainEvent;
