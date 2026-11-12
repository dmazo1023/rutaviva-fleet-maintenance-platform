namespace RutaViva.Workshops.Api.Contracts;

public sealed record RegisterWorkshopRequest(
    string? Name,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? City);
