using RutaViva.Workshops.Domain;

namespace RutaViva.Workshops.Application.Workshops;

public sealed record WorkshopSummaryDto(
    Guid Id,
    string Name,
    string TaxId,
    string City,
    WorkshopStatus Status);
