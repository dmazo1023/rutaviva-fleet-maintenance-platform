using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;

public sealed class VehicleRegisteredHandler(IServiceOrderReferenceStore references, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<VehicleRegisteredV1>
{
    public async Task<Result> HandleAsync(VehicleRegisteredV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        VehicleReference incoming = VehicleReference.Create(
            integrationEvent.VehicleId,
            integrationEvent.Plate,
            integrationEvent.CustomerId,
            integrationEvent.WorkshopId,
            integrationEvent.ResponsibleApproverId,
            ReferenceStatus.IsActive(integrationEvent.Status),
            integrationEvent.OccurredAt);
        VehicleReference? existing = await references.FindVehicleAsync(incoming.Id, cancellationToken);
        if (existing is null)
        {
            references.AddVehicle(incoming);
        }
        else
        {
            existing.Apply(incoming);
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
