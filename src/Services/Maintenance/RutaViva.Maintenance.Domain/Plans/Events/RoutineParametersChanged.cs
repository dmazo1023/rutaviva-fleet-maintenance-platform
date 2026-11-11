using RutaViva.BuildingBlocks.Domain.Events;

namespace RutaViva.Maintenance.Domain.Plans.Events;

public sealed record RoutineParametersChanged(RoutineStatusSnapshot Snapshot, DateTimeOffset OccurredAt) : IDomainEvent;
