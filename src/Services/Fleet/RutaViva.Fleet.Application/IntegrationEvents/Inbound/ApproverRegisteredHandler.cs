using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Fleet.Application.IntegrationEvents.Inbound;

public sealed class ApproverRegisteredHandler(IFleetReferenceWriter writer, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<ApproverRegisteredV1>
{
    public async Task<Result> HandleAsync(ApproverRegisteredV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        Result<ApproverReference> reference = ToReference(integrationEvent);
        if (reference.IsFailure)
        {
            return Result.Failure(reference.Error);
        }

        await writer.UpsertApproverAsync(reference.Value, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Result<ApproverReference> ToReference(ApproverRegisteredV1 integrationEvent)
    {
        Result<ApproverId> id = ApproverId.Create(integrationEvent.ApproverId);
        if (id.IsFailure)
        {
            return id.Error;
        }

        Result<CustomerId> customerId = CustomerId.Create(integrationEvent.CustomerId);
        if (customerId.IsFailure)
        {
            return customerId.Error;
        }

        Result<ReferenceStatus> status = ReferenceStatusParser.Parse(integrationEvent.Status);
        if (status.IsFailure)
        {
            return status.Error;
        }

        return ApproverReference.Create(
            id.Value,
            customerId.Value,
            integrationEvent.FullName,
            integrationEvent.Email,
            status.Value,
            integrationEvent.OccurredAt);
    }
}
