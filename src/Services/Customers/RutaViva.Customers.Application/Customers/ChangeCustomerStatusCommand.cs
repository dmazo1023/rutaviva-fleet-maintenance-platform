using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Customers.Domain;
using RutaViva.Customers.Domain.Common;

namespace RutaViva.Customers.Application.Customers;

public sealed record ChangeCustomerStatusCommand(CustomerId CustomerId, CustomerStatus TargetStatus)
    : ICommand<CustomerId>;
