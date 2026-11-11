using RutaViva.Workshops.Domain;

namespace RutaViva.Workshops.Application.Workshops;

public sealed record WorkshopDetailDto(
    Guid Id,
    string Name,
    string TaxId,
    string Email,
    string? Phone,
    string Address,
    string City,
    WorkshopStatus Status,
    DateTimeOffset CreatedAt);
