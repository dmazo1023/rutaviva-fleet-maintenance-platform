using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Domain.Events;

public sealed record CustomerStatusChanged(
    CustomerId CustomerId,
    CustomerStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;
