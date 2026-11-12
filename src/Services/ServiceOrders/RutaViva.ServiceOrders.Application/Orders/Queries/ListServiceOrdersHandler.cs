using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain;

namespace RutaViva.ServiceOrders.Application.Orders.Queries;

public sealed class ListServiceOrdersHandler(IServiceOrderQueries orders)
    : IQueryHandler<ListServiceOrdersQuery, IReadOnlyList<ServiceOrderListItemDto>>
{
    public async Task<Result<IReadOnlyList<ServiceOrderListItemDto>>> HandleAsync(ListServiceOrdersQuery query, CancellationToken cancellationToken)
    {
        ServiceOrderFilter filter = new(query.CustomerId, query.VehicleId, query.RoutineId, ServiceOrderStatusGroups.StatusesOf(query.StatusGroup));
        IReadOnlyList<ServiceOrder> found = await orders.ListAsync(filter, cancellationToken);
        IReadOnlyList<ServiceOrderListItemDto> items = found.Select(ServiceOrderViewMapper.ToListItem).ToList();
        return Result<IReadOnlyList<ServiceOrderListItemDto>>.Success(items);
    }
}
