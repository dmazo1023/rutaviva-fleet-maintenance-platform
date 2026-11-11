using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;

namespace RutaViva.Maintenance.Application.Plans.Queries;

public sealed class GetMaintenancePlanSummariesHandler(IMaintenancePlanQueries plans)
    : IQueryHandler<GetMaintenancePlanSummariesQuery, IReadOnlyList<MaintenancePlanSummaryDto>>
{
    public async Task<Result<IReadOnlyList<MaintenancePlanSummaryDto>>> HandleAsync(
        GetMaintenancePlanSummariesQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MaintenancePlanSummaryDto> summaries = await plans.GetSummariesAsync(query.CustomerId, cancellationToken);
        return Result<IReadOnlyList<MaintenancePlanSummaryDto>>.Success(summaries);
    }
}
