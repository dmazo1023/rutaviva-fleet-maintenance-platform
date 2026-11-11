using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Workshops.Domain;

namespace RutaViva.Workshops.Application.Workshops;

public sealed record ListWorkshopsQuery(WorkshopStatus? Status, string? City) : IQuery<IReadOnlyList<WorkshopSummaryDto>>;
