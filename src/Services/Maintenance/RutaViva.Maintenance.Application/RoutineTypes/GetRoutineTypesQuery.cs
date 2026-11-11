using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.Maintenance.Application.RoutineTypes;

public sealed record GetRoutineTypesQuery : IQuery<IReadOnlyList<RoutineTypeDto>>;
