using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Application.Abstractions;
using RutaViva.Workshops.Domain.Common;

namespace RutaViva.Workshops.Application.Workshops.Queries;

public sealed class GetWorkshopDetailHandler(IWorkshopQueries workshops)
    : IQueryHandler<GetWorkshopDetailQuery, WorkshopDetailDto>
{
    public async Task<Result<WorkshopDetailDto>> HandleAsync(GetWorkshopDetailQuery query, CancellationToken cancellationToken)
    {
        WorkshopDetailDto? detail = await workshops.GetDetailAsync(query.WorkshopId, cancellationToken);
        if (detail is null)
        {
            return WorkshopErrors.NotFound;
        }

        return detail;
    }
}
