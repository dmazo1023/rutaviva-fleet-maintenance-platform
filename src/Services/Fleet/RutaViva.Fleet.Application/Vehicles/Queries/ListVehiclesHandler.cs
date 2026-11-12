using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed class ListVehiclesHandler(IVehicleQueries vehicles) : IQueryHandler<ListVehiclesQuery, PagedResult<VehicleSummaryDto>>
{
    public async Task<Result<PagedResult<VehicleSummaryDto>>> HandleAsync(ListVehiclesQuery query, CancellationToken cancellationToken)
    {
        VehicleListCriteria criteria = new(query.CustomerId, query.Status, query.Search, query.Page, query.PageSize);
        PagedResult<VehicleSummaryDto> page = await vehicles.ListAsync(criteria, cancellationToken);
        return page;
    }
}
