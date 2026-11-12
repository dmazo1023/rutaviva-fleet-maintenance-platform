using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Maintenance;
using RutaViva.ServiceOrders.Application.Abstractions;
using RutaViva.ServiceOrders.Domain.References;

namespace RutaViva.ServiceOrders.Application.IntegrationEvents.Inbound;

public sealed class MaintenanceRoutineRegisteredHandler(IServiceOrderReferenceStore references, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<MaintenanceRoutineRegisteredV1>
{
    public async Task<Result> HandleAsync(MaintenanceRoutineRegisteredV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        RoutineSnapshot routine = integrationEvent.Routine;
        RoutineReference incoming = RoutineReference.Create(routine.RoutineId, routine.VehicleId, routine.RoutineName, routine.RoutineTypeCode, integrationEvent.OccurredAt);
        RoutineReference? existing = await references.FindRoutineAsync(incoming.Id, cancellationToken);
        if (existing is null)
        {
            references.AddRoutine(incoming);
        }
        else
        {
            existing.Apply(incoming);
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
