using RutaViva.Customers.Domain;

namespace RutaViva.Customers.Application.Customers;

public sealed record ApproverDto(Guid Id, string FullName, string Email, ApproverStatus Status);
