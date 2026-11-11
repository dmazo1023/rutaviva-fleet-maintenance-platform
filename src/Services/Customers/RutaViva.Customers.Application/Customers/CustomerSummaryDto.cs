using RutaViva.Customers.Domain;

namespace RutaViva.Customers.Application.Customers;

public sealed record CustomerSummaryDto(Guid Id, string Name, string TaxId, string Email, CustomerStatus Status);
