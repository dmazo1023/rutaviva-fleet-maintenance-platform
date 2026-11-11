using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Fleet.Application.IntegrationEvents.Inbound;

public sealed class CustomerStatusChangedHandler(
    IFleetReferenceReader reader,
    IFleetReferenceWriter writer,
    IUnitOfWork unitOfWork,
    ILogger<CustomerStatusChangedHandler> logger) : IIntegrationEventHandler<CustomerStatusChangedV1>
{
    private const string ReferenceType = "CustomerReference";

    public async Task<Result> HandleAsync(CustomerStatusChangedV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        Result<CustomerId> id = CustomerId.Create(integrationEvent.CustomerId);
        if (id.IsFailure)
        {
            return Result.Failure(id.Error);
        }

        Result<ReferenceStatus> status = ReferenceStatusParser.Parse(integrationEvent.Status);
        if (status.IsFailure)
        {
            return Result.Failure(status.Error);
        }

        CustomerReference? reference = await reader.FindCustomerAsync(id.Value, cancellationToken);
        if (reference is null)
        {
            logger.LogReferenceUpdateIgnored(ReferenceType, integrationEvent.CustomerId, integrationEvent.Status);
            return Result.Success();
        }

        return await ApplyAsync(reference, status.Value, integrationEvent.OccurredAt, cancellationToken);
    }

    private async Task<Result> ApplyAsync(CustomerReference reference, ReferenceStatus status, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        reference.Apply(status, occurredAt);
        await writer.UpsertCustomerAsync(reference, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
