using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;

namespace RutaViva.Maintenance.Application.RoutineTypes.Queries;

public sealed class GetRoutineTypesHandler(IRoutineTypeQueries routineTypes)
    : IQueryHandler<GetRoutineTypesQuery, IReadOnlyList<RoutineTypeDto>>
{
    public async Task<Result<IReadOnlyList<RoutineTypeDto>>> HandleAsync(GetRoutineTypesQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<RoutineTypeDto> all = await routineTypes.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<RoutineTypeDto>>.Success(all);
    }
}
