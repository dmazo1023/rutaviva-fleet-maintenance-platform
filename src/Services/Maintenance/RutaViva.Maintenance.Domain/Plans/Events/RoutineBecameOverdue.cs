using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Maintenance.Domain.Alerts;

namespace RutaViva.Maintenance.Domain.Plans.Events;

public sealed record RoutineBecameOverdue(
    RoutineStatusSnapshot Snapshot,
    AlertTrigger Trigger,
    DateTimeOffset OccurredAt) : IDomainEvent;
