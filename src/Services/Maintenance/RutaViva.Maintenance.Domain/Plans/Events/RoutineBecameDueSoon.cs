using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Maintenance.Domain.Alerts;

namespace RutaViva.Maintenance.Domain.Plans.Events;

public sealed record RoutineBecameDueSoon(
    RoutineStatusSnapshot Snapshot,
    AlertTrigger Trigger,
    bool IsFirstNoticeInCycle,
    DateTimeOffset OccurredAt) : IDomainEvent;
