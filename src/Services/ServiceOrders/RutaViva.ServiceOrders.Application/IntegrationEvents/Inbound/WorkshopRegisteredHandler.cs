using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Workshops;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;

public sealed class WorkshopRegisteredHandler(IServiceOrderReferenceStore references, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<WorkshopRegisteredV1>
{
    public async Task<Result> HandleAsync(WorkshopRegisteredV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        WorkshopReference incoming = WorkshopReference.Create(integrationEvent.WorkshopId, integrationEvent.Name, integrationEvent.OccurredAt);
        WorkshopReference? existing = await references.FindWorkshopAsync(incoming.Id, cancellationToken);
        if (existing is null)
        {
            references.AddWorkshop(incoming);
        }
        else
        {
            existing.Apply(incoming);
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
