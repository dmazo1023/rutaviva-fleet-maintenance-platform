using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Customers.Domain;

namespace RutaViva.Customers.Application.Customers;

public sealed record ListCustomersQuery(CustomerStatus? Status) : IQuery<IReadOnlyList<CustomerSummaryDto>>;
