using RutaViva.Customers.Domain;

namespace RutaViva.Customers.Application.Customers;

public sealed record CustomerDetailDto(
    Guid Id,
    string Name,
    string TaxId,
    string Email,
    string? Phone,
    CustomerStatus Status,
    IReadOnlyList<ApproverDto> Approvers);
