using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Domain.Events;

public sealed record WorkshopStatusChanged(
    WorkshopId WorkshopId,
    WorkshopStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;
