using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Customers.Domain.Events;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Customers.Application.IntegrationEvents.Outbound;

public sealed class ApproverRegisteredMapper : IIntegrationEventMapper<ApproverRegistered>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(ApproverRegistered domainEvent) =>
        [
            new ApproverRegisteredV1(
                domainEvent.ApproverId.Value,
                domainEvent.CustomerId.Value,
                domainEvent.FullName.Value,
                domainEvent.Email.Value,
                domainEvent.Status.ToString(),
                domainEvent.OccurredAt)
        ];
}
