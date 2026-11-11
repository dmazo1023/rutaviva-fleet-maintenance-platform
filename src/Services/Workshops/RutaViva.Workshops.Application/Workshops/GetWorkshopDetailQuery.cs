using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Application.Workshops;

public sealed record GetWorkshopDetailQuery(WorkshopId WorkshopId) : IQuery<WorkshopDetailDto>;
