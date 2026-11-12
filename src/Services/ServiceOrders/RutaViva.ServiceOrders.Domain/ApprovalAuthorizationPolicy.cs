namespace RutaViva.ServiceOrders.Domain;

public static class ApprovalAuthorizationPolicy
{
    public static bool CanApprove(ServiceOrderReference reference, ServiceOrderApprover approver) =>
        approver.IsActive && (approver.CustomerId == reference.CustomerId || approver.Id == reference.ResponsibleApproverId);
}
