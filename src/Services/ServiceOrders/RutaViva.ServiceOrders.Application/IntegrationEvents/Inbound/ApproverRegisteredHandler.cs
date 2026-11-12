using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;

public sealed class ApproverRegisteredHandler(IServiceOrderReferenceStore references, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<ApproverRegisteredV1>
{
    public async Task<Result> HandleAsync(ApproverRegisteredV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        ApproverReference incoming = ApproverReference.Create(
            integrationEvent.ApproverId,
            integrationEvent.CustomerId,
            integrationEvent.FullName,
            ReferenceStatus.IsActive(integrationEvent.Status),
            integrationEvent.OccurredAt);
        ApproverReference? existing = await references.FindApproverAsync(incoming.Id, cancellationToken);
        if (existing is null)
        {
            references.AddApprover(incoming);
        }
        else
        {
            existing.Apply(incoming);
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
