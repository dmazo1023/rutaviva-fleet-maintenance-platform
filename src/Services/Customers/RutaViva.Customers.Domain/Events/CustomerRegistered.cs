using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Domain.Events;

public sealed record CustomerRegistered(
    CustomerId CustomerId,
    TaxId TaxId,
    PersonName Name,
    EmailAddress Email,
    CustomerStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;
