using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;

public sealed class ApproverStatusChangedHandler(IServiceOrderReferenceStore references, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<ApproverStatusChangedV1>
{
    public async Task<Result> HandleAsync(ApproverStatusChangedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        ApproverReference? existing = await references.FindApproverAsync(integrationEvent.ApproverId, cancellationToken);
        if (existing is null)
        {
            return Result.Success();
        }

        existing.ChangeStatus(ReferenceStatus.IsActive(integrationEvent.Status), integrationEvent.OccurredAt);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
