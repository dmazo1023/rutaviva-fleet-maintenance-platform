using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.Fleet.Application.References;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.References;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Fleet.Application.IntegrationEvents.Inbound;

public sealed class CustomerRegisteredHandler(IFleetReferenceWriter writer, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<CustomerRegisteredV1>
{
    public async Task<Result> HandleAsync(CustomerRegisteredV1 integrationEvent, EventMetadata metadata, CancellationToken cancellationToken)
    {
        Result<CustomerReference> reference = ToReference(integrationEvent);
        if (reference.IsFailure)
        {
            return Result.Failure(reference.Error);
        }

        await writer.UpsertCustomerAsync(reference.Value, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Result<CustomerReference> ToReference(CustomerRegisteredV1 integrationEvent)
    {
        Result<CustomerId> id = CustomerId.Create(integrationEvent.CustomerId);
        if (id.IsFailure)
        {
            return id.Error;
        }

        Result<ReferenceStatus> status = ReferenceStatusParser.Parse(integrationEvent.Status);
        if (status.IsFailure)
        {
            return status.Error;
        }

        return CustomerReference.Create(id.Value, integrationEvent.TaxId, integrationEvent.Name, status.Value, integrationEvent.OccurredAt);
    }
}
