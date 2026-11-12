using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed class ExecuteServiceOrderActionHandler(
    IServiceOrderRepository orders,
    QuoteApprovalPolicy approvalPolicy,
    IUnitOfWork unitOfWork) : ICommandHandler<ExecuteServiceOrderActionCommand, ServiceOrderId>
{
    public async Task<Result<ServiceOrderId>> HandleAsync(ExecuteServiceOrderActionCommand command, CancellationToken cancellationToken)
    {
        ServiceOrder? order = await orders.GetByIdAsync(command.ServiceOrderId, cancellationToken);
        if (order is null)
        {
            return ServiceOrderErrors.NotFound;
        }

        Result executed = Execute(order, command);
        if (executed.IsFailure)
        {
            return executed.Error;
        }

        Result saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return command.ServiceOrderId;
    }

    private Result Execute(ServiceOrder order, ExecuteServiceOrderActionCommand command) => command.Action switch
    {
        ServiceOrderAction.Quote => order.Quote(command.Amount ?? 0, approvalPolicy, command.Actor),
        ServiceOrderAction.Approve => order.Approve(command.Actor),
        ServiceOrderAction.Reject => order.Reject(command.Actor),
        ServiceOrderAction.Start => order.Start(command.Actor),
        ServiceOrderAction.Complete => order.Complete(command.ServicedKm ?? 0, command.Actor),
        _ => Result.Failure(ServiceOrderErrors.InvalidTransition)
    };
}
