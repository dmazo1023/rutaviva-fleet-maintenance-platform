using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.Customers.Application.Customers;

public sealed record RegisterCustomerCommand(string Name, string TaxId, string Email, string? Phone) : ICommand<Guid>;
