using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Workshops.Application.Abstractions;

namespace RutaViva.Workshops.Application.Workshops.Queries;

public sealed class ListWorkshopsHandler(IWorkshopQueries workshops)
    : IQueryHandler<ListWorkshopsQuery, IReadOnlyList<WorkshopSummaryDto>>
{
    public async Task<Result<IReadOnlyList<WorkshopSummaryDto>>> HandleAsync(ListWorkshopsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkshopSummaryDto> summaries = await workshops.ListAsync(query.Status, query.City, cancellationToken);
        return Result<IReadOnlyList<WorkshopSummaryDto>>.Success(summaries);
    }
}
