namespace RutaViva.ServiceOrders.Domain;

public enum ServiceOrderStatus
{
    Created = 1,
    Quoted = 2,
    PendingApproval = 3,
    Approved = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7
}
