using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Domain.RoutineTypes.Events;

public sealed record RoutineTypeParametersChanged(RoutineTypeId RoutineTypeId, DateTimeOffset OccurredAt) : IDomainEvent;
