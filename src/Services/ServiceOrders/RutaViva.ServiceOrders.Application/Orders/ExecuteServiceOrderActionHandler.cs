using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain;
using RutaViva.ServiceOrders.Domain.Common;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Application.Orders;

public sealed class ExecuteServiceOrderActionHandler(
    IServiceOrderRepository orders,
    IServiceOrderReferenceStore references,
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

        Result executed = await ExecuteAsync(order, command, cancellationToken);
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

    private async Task<Result> ExecuteAsync(ServiceOrder order, ExecuteServiceOrderActionCommand command, CancellationToken cancellationToken) =>
        command.Action switch
        {
            ServiceOrderAction.Approve => await DecideAsync(command, approver => order.Approve(approver), cancellationToken),
            ServiceOrderAction.Reject => await DecideAsync(command, approver => order.Reject(approver), cancellationToken),
            _ => Execute(order, command)
        };

    private Result Execute(ServiceOrder order, ExecuteServiceOrderActionCommand command) => command.Action switch
    {
        ServiceOrderAction.Quote => order.Quote(command.Amount ?? 0, approvalPolicy, command.WorkshopId ?? Guid.Empty, command.Actor),
        ServiceOrderAction.Start => order.Start(command.WorkshopId ?? Guid.Empty, command.Actor),
        ServiceOrderAction.Complete => order.Complete(command.ServicedKm ?? 0, command.WorkshopId ?? Guid.Empty, command.Actor),
        ServiceOrderAction.Cancel => order.Cancel(command.Actor),
        _ => Result.Failure(ServiceOrderErrors.InvalidTransition)
    };

    private async Task<Result> DecideAsync(
        ExecuteServiceOrderActionCommand command,
        Func<ServiceOrderApprover, Result> decide,
        CancellationToken cancellationToken)
    {
        ApproverReference? approver = await references.FindApproverAsync(command.ApproverId ?? Guid.Empty, cancellationToken);
        if (approver is null)
        {
            return Result.Failure(ServiceOrderErrors.ApproverNotFound);
        }

        return decide(approver.ToApprover());
    }
}
