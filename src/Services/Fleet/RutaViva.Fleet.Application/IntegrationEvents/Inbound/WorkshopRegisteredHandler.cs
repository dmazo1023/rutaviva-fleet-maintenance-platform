using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Workshops;

namespace RutaViva.Fleet.Application.IntegrationEvents.Inbound;

public sealed class WorkshopRegisteredHandler(IFleetReferenceWriter writer, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<WorkshopRegisteredV1>
{
    public async Task<Result> HandleAsync(WorkshopRegisteredV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        Result<WorkshopReference> reference = ToReference(integrationEvent);
        if (reference.IsFailure)
        {
            return Result.Failure(reference.Error);
        }

        await writer.UpsertWorkshopAsync(reference.Value, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Result<WorkshopReference> ToReference(WorkshopRegisteredV1 integrationEvent)
    {
        Result<WorkshopId> id = WorkshopId.Create(integrationEvent.WorkshopId);
        if (id.IsFailure)
        {
            return id.Error;
        }

        Result<ReferenceStatus> status = ReferenceStatusParser.Parse(integrationEvent.Status);
        if (status.IsFailure)
        {
            return status.Error;
        }

        return WorkshopReference.Create(id.Value, integrationEvent.TaxId, integrationEvent.Name, status.Value, integrationEvent.OccurredAt);
    }
}
