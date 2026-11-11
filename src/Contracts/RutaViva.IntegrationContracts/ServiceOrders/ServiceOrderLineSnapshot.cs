namespace RutaViva.IntegrationContracts.ServiceOrders;

public sealed record ServiceOrderLineSnapshot(
    Guid LineId,
    string LineType,
    string Description,
    decimal? Amount,
    string PricingStatus,
    string RequestedBy,
    string ApprovalStatus);
