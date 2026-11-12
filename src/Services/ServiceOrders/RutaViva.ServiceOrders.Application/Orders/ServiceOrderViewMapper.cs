using RutaViva.ServiceOrders.Domain;

namespace RutaViva.ServiceOrders.Application.Orders;

public static class ServiceOrderViewMapper
{
    private const string QuoteReason = "Quote";
    private const string NormalPriority = "Normal";
    private const string RoutineLineType = "Routine";
    private const string RoutineLineSuffix = "-routine";
    private const string PricedStatus = "Priced";
    private const string PendingPricingStatus = "PendingPricing";
    private const string WorkshopRequester = "Workshop";
    private const string ApprovedLine = "Approved";
    private const string PendingApprovalLine = "PendingApproval";
    private const string RejectedLine = "Rejected";
    private const string StatusChangedType = "StatusChanged";
    private const string NoIcon = "";

    public static ServiceOrderListItemDto ToListItem(ServiceOrder order) =>
        new(
            order.Id.Value,
            order.Number,
            order.Reference.VehicleId,
            order.Reference.Plate,
            order.Reference.RoutineId,
            order.Reference.RoutineName,
            order.Reference.RoutineTypeCode,
            order.Reference.CustomerId,
            order.Status,
            null,
            ReasonOf(order),
            PriorityOf(order),
            order.QuotedAmount,
            null,
            order.UpdatedAt);

    public static ServiceOrderDetailDto ToDetail(ServiceOrder order, ServiceOrderActor? actor, QuoteApprovalPolicy policy) =>
        new(
            order.Id.Value,
            order.Number,
            order.Reference.VehicleId,
            order.Reference.Plate,
            order.Reference.RoutineId,
            order.Reference.RoutineName,
            order.Reference.RoutineTypeCode,
            NoIcon,
            order.Reference.CustomerId,
            order.Reference.WorkshopId,
            order.Reference.WorkshopName,
            order.Status,
            null,
            ReasonOf(order),
            PriorityOf(order),
            ActionsOf(order, actor),
            [LineOf(order)],
            order.QuotedAmount ?? 0,
            order.AutoApproved,
            order.QuotedAmount is null ? policy.AutoApprovalMaxAmount : order.AutoApprovalMaxAmount,
            null,
            HistoryOf(order),
            order.UpdatedAt);

    private static bool IsPendingApproval(ServiceOrder order) => order.Status == ServiceOrderStatus.PendingApproval;

    private static string? ReasonOf(ServiceOrder order) => IsPendingApproval(order) ? QuoteReason : null;

    private static string? PriorityOf(ServiceOrder order) => IsPendingApproval(order) ? NormalPriority : null;

    private static ServiceOrderActionDto[] ActionsOf(ServiceOrder order, ServiceOrderActor? actor) =>
        AvailableActionsPolicy.For(order.Status, actor).Select(action => new ServiceOrderActionDto(action, true, null)).ToArray();

    private static ServiceLineDto LineOf(ServiceOrder order) =>
        new(
            order.Id.Value + RoutineLineSuffix,
            RoutineLineType,
            order.Reference.RoutineName,
            order.QuotedAmount,
            order.QuotedAmount is null ? PendingPricingStatus : PricedStatus,
            WorkshopRequester,
            LineApprovalOf(order.Status));

    private static string LineApprovalOf(ServiceOrderStatus status) => status switch
    {
        ServiceOrderStatus.Cancelled => RejectedLine,
        ServiceOrderStatus.Created or ServiceOrderStatus.Quoted or ServiceOrderStatus.PendingApproval => PendingApprovalLine,
        _ => ApprovedLine
    };

    private static ServiceOrderHistoryEntryDto[] HistoryOf(ServiceOrder order) =>
        order.History
            .OrderByDescending(entry => entry.ChangedAt)
            .Select(entry => new ServiceOrderHistoryEntryDto(entry.Id, StatusChangedType, entry.Status.ToString(), entry.ChangedBy, entry.ChangedAt, NoIcon))
            .ToArray();
}
