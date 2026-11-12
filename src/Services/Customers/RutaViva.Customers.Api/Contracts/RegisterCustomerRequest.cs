namespace RutaViva.Customers.Api.Contracts;

public sealed record RegisterCustomerRequest(string? Name, string? TaxId, string? Email, string? Phone);
