using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Workshops;

namespace RutaViva.Fleet.Application.IntegrationEvents.Inbound;

public sealed class WorkshopStatusChangedHandler(
    IFleetReferenceReader reader,
    IFleetReferenceWriter writer,
    IUnitOfWork unitOfWork,
    ILogger<WorkshopStatusChangedHandler> logger) : IIntegrationEventHandler<WorkshopStatusChangedV1>
{
    private const string ReferenceType = "WorkshopReference";

    public async Task<Result> HandleAsync(WorkshopStatusChangedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        Result<WorkshopId> id = WorkshopId.Create(integrationEvent.WorkshopId);
        if (id.IsFailure)
        {
            return Result.Failure(id.Error);
        }

        Result<ReferenceStatus> status = ReferenceStatusParser.Parse(integrationEvent.Status);
        if (status.IsFailure)
        {
            return Result.Failure(status.Error);
        }

        WorkshopReference? reference = await reader.FindWorkshopAsync(id.Value, cancellationToken);
        if (reference is null)
        {
            logger.LogReferenceUpdateIgnored(ReferenceType, integrationEvent.WorkshopId, integrationEvent.Status);
            return Result.Success();
        }

        return await ApplyAsync(reference, status.Value, integrationEvent.OccurredAt, cancellationToken);
    }

    private async Task<Result> ApplyAsync(WorkshopReference reference, ReferenceStatus status, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        reference.Apply(status, occurredAt);
        await writer.UpsertWorkshopAsync(reference, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
