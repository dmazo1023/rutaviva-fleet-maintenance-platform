using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Customers.Domain.Events;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Customers.Application.IntegrationEvents.Outbound;

public sealed class ApproverStatusChangedMapper : IIntegrationEventMapper<ApproverStatusChanged>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(ApproverStatusChanged domainEvent) =>
        [
            new ApproverStatusChangedV1(
                domainEvent.ApproverId.Value,
                domainEvent.CustomerId.Value,
                domainEvent.Status.ToString(),
                domainEvent.OccurredAt)
        ];
}
