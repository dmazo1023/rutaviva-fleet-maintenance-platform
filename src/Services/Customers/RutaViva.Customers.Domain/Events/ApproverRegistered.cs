using RutaViva.BuildingBlocks.Domain.Events;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Domain.Events;

public sealed record ApproverRegistered(
    ApproverId ApproverId,
    CustomerId CustomerId,
    PersonName FullName,
    EmailAddress Email,
    ApproverStatus Status,
    DateTimeOffset OccurredAt) : IDomainEvent;
