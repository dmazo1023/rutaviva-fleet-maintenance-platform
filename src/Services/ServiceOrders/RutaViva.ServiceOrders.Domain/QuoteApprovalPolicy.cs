using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.ServiceOrders.Domain.Common;

namespace RutaViva.ServiceOrders.Domain;

public sealed record QuoteApprovalPolicy
{
    private QuoteApprovalPolicy(decimal autoApprovalMaxAmount) => AutoApprovalMaxAmount = autoApprovalMaxAmount;

    public decimal AutoApprovalMaxAmount { get; }

    public static Result<QuoteApprovalPolicy> Create(decimal autoApprovalMaxAmount)
    {
        if (autoApprovalMaxAmount < 0)
        {
            return ServiceOrderErrors.ThresholdInvalid;
        }

        return new QuoteApprovalPolicy(autoApprovalMaxAmount);
    }

    public bool RequiresApproval(decimal amount) => amount > AutoApprovalMaxAmount;
}
