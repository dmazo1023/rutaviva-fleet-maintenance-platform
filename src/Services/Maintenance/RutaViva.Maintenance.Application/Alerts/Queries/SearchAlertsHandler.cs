using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Maintenance.Application.Abstractions;

namespace RutaViva.Maintenance.Application.Alerts.Queries;

public sealed class SearchAlertsHandler(IAlertQueries alerts) : IQueryHandler<SearchAlertsQuery, PagedResult<AlertDto>>
{
    public async Task<Result<PagedResult<AlertDto>>> HandleAsync(SearchAlertsQuery query, CancellationToken cancellationToken)
    {
        AlertSearchCriteria criteria = new(query.Status, query.VehicleId, query.CustomerId, query.Page, query.PageSize);
        PagedResult<AlertDto> page = await alerts.SearchAsync(criteria, cancellationToken);
        return page;
    }
}
