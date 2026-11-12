using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Domain.Events;

public sealed record ServiceOrderCompleted(
    ServiceOrderId ServiceOrderId,
    string Number,
    ServiceOrderReference Reference,
    decimal TotalAmount,
    bool AutoApproved,
    long ServicedKm,
    DateTimeOffset CreatedAt,
    DateTimeOffset OccurredAt) : IDomainEvent;
