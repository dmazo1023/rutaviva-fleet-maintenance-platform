using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.Customers;

public sealed record GetCustomerDetailQuery(CustomerId CustomerId) : IQuery<CustomerDetailDto>;
