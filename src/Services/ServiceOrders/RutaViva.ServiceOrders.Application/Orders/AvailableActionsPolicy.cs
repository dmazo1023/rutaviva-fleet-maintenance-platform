using RutaViva.ServiceOrders.Domain;

namespace RutaViva.ServiceOrders.Application.Orders;

public static class AvailableActionsPolicy
{
    private static readonly IReadOnlyDictionary<(ServiceOrderStatus Status, ServiceOrderActor Actor), ServiceOrderAction[]> Actions =
        new Dictionary<(ServiceOrderStatus Status, ServiceOrderActor Actor), ServiceOrderAction[]>
        {
            [(ServiceOrderStatus.Created, ServiceOrderActor.Workshop)] = [ServiceOrderAction.Quote],
            [(ServiceOrderStatus.PendingApproval, ServiceOrderActor.Approver)] = [ServiceOrderAction.Approve, ServiceOrderAction.Reject],
            [(ServiceOrderStatus.Approved, ServiceOrderActor.Workshop)] = [ServiceOrderAction.Start],
            [(ServiceOrderStatus.InProgress, ServiceOrderActor.Workshop)] = [ServiceOrderAction.Complete]
        };

    public static IReadOnlyList<ServiceOrderAction> For(ServiceOrderStatus status, ServiceOrderActor? actor)
    {
        if (actor is not { } value)
        {
            return [];
        }

        return Actions.TryGetValue((status, value), out ServiceOrderAction[]? actions) ? actions : [];
    }
}
