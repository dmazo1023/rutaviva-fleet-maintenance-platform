using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed class CreateServiceOrderHandler(IServiceOrderRepository orders, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateServiceOrderCommand, ServiceOrderId>
{
    public async Task<Result<ServiceOrderId>> HandleAsync(CreateServiceOrderCommand command, CancellationToken cancellationToken)
    {
        Result<ServiceOrder> order = ServiceOrder.Create(command.Reference, command.CreatedBy);
        if (order.IsFailure)
        {
            return order.Error;
        }

        orders.Add(order.Value);
        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return order.Value.Id;
    }
}
