using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Application.Orders.Queries;

public sealed class GetServiceOrderDetailHandler(IServiceOrderQueries orders, QuoteApprovalPolicy approvalPolicy)
    : IQueryHandler<GetServiceOrderDetailQuery, ServiceOrderDetailDto>
{
    public async Task<Result<ServiceOrderDetailDto>> HandleAsync(GetServiceOrderDetailQuery query, CancellationToken cancellationToken)
    {
        ServiceOrder? order = await orders.FindAsync(query.ServiceOrderId, cancellationToken);
        if (order is null)
        {
            return ServiceOrderErrors.NotFound;
        }

        return ServiceOrderViewMapper.ToDetail(order, query.Actor, approvalPolicy);
    }
}
