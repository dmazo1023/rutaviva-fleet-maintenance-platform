using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.Workshops.Application.Workshops;

public sealed record RegisterWorkshopCommand(
    string? Name,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? City) : ICommand<Guid>;
