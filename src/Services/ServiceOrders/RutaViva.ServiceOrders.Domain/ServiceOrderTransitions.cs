namespace RutaViva.ServiceOrders.Domain;

public static class ServiceOrderTransitions
{
    private static readonly IReadOnlyDictionary<ServiceOrderStatus, ServiceOrderStatus[]> Allowed =
        new Dictionary<ServiceOrderStatus, ServiceOrderStatus[]>
        {
            [ServiceOrderStatus.Created] = [ServiceOrderStatus.Quoted],
            [ServiceOrderStatus.Quoted] = [ServiceOrderStatus.PendingApproval, ServiceOrderStatus.Approved],
            [ServiceOrderStatus.PendingApproval] = [ServiceOrderStatus.Approved, ServiceOrderStatus.Cancelled],
            [ServiceOrderStatus.Approved] = [ServiceOrderStatus.InProgress],
            [ServiceOrderStatus.InProgress] = [ServiceOrderStatus.Completed],
            [ServiceOrderStatus.Completed] = [],
            [ServiceOrderStatus.Cancelled] = []
        };

    public static bool CanMove(ServiceOrderStatus from, ServiceOrderStatus to) => Allowed[from].Contains(to);
}
