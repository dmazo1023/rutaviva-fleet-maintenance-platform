using RutaViva.BuildingBlocks.Domain.Events;

namespace RutaViva.Maintenance.Domain.Plans.Events;

public sealed record RoutineDueStatusReverted(RoutineStatusSnapshot Snapshot, DateTimeOffset OccurredAt) : IDomainEvent;
