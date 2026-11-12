using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Infrastructure.Persistence.Queries;

internal sealed record CustomerReadRow(
    CustomerId Id,
    PersonName Name,
    TaxId TaxId,
    EmailAddress Email,
    PhoneNumber? Phone,
    CustomerStatus Status);
