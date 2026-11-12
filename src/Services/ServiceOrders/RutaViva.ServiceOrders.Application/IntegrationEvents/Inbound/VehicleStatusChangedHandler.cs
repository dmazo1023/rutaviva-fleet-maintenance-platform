using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Fleet;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;

public sealed class VehicleStatusChangedHandler(IServiceOrderReferenceStore references, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<VehicleStatusChangedV1>
{
    public async Task<Result> HandleAsync(VehicleStatusChangedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        VehicleReference? existing = await references.FindVehicleAsync(integrationEvent.VehicleId, cancellationToken);
        if (existing is null)
        {
            return Result.Success();
        }

        existing.ChangeStatus(ReferenceStatus.IsActive(integrationEvent.Status), integrationEvent.OccurredAt);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
