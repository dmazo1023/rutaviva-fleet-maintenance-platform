using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;

namespace RutaViva.Maintenance.Domain.Plans.Events;

public sealed record RoutineRestarted(
    RoutineStatusSnapshot Snapshot,
    Guid ServiceOrderId,
    ServiceRecord Service,
    DateTimeOffset OccurredAt) : IDomainEvent;
