using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Fleet.Application.IntegrationEvents.Inbound;

public sealed class ApproverStatusChangedHandler(
    IFleetReferenceReader reader,
    IFleetReferenceWriter writer,
    IUnitOfWork unitOfWork,
    ILogger<ApproverStatusChangedHandler> logger) : IIntegrationEventHandler<ApproverStatusChangedV1>
{
    private const string ReferenceType = "ApproverReference";

    public async Task<Result> HandleAsync(ApproverStatusChangedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        Result<ApproverId> id = ApproverId.Create(integrationEvent.ApproverId);
        if (id.IsFailure)
        {
            return Result.Failure(id.Error);
        }

        Result<ReferenceStatus> status = ReferenceStatusParser.Parse(integrationEvent.Status);
        if (status.IsFailure)
        {
            return Result.Failure(status.Error);
        }

        ApproverReference? reference = await reader.FindApproverAsync(id.Value, cancellationToken);
        if (reference is null)
        {
            logger.LogReferenceUpdateIgnored(ReferenceType, integrationEvent.ApproverId, integrationEvent.Status);
            return Result.Success();
        }

        return await ApplyAsync(reference, status.Value, integrationEvent.OccurredAt, cancellationToken);
    }

    private async Task<Result> ApplyAsync(ApproverReference reference, ReferenceStatus status, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        reference.Apply(status, occurredAt);
        await writer.UpsertApproverAsync(reference, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
