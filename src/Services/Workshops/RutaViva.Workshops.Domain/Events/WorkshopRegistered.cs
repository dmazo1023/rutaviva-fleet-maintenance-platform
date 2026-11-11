using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Workshops.Domain.Common;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Domain.Events;

public sealed record WorkshopRegistered(
    WorkshopId WorkshopId,
    TaxId TaxId,
    string Name,
    string City,
    WorkshopStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;
