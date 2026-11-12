using RutaViva.ServiceOrders.Domain;

namespace RutaViva.ServiceOrders.Application.Orders;

public static class ServiceOrderStatusGroups
{
    private static readonly IReadOnlyDictionary<ServiceOrderStatusGroup, ServiceOrderStatus[]> Groups =
        new Dictionary<ServiceOrderStatusGroup, ServiceOrderStatus[]>
        {
            [ServiceOrderStatusGroup.PendingApproval] = [ServiceOrderStatus.PendingApproval],
            [ServiceOrderStatusGroup.Open] =
            [
                ServiceOrderStatus.Created,
                ServiceOrderStatus.Quoted,
                ServiceOrderStatus.PendingApproval,
                ServiceOrderStatus.Approved,
                ServiceOrderStatus.InProgress
            ],
            [ServiceOrderStatusGroup.Closed] = [ServiceOrderStatus.Completed, ServiceOrderStatus.Cancelled]
        };

    public static IReadOnlyCollection<ServiceOrderStatus> StatusesOf(ServiceOrderStatusGroup? group) =>
        group is { } value ? Groups[value] : [];
}
