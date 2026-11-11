using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Customers.Domain.Events;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Customers.Application.IntegrationEvents.Outbound;

public sealed class CustomerStatusChangedMapper : IIntegrationEventMapper<CustomerStatusChanged>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(CustomerStatusChanged domainEvent) =>
        [
            new CustomerStatusChangedV1(
                domainEvent.CustomerId.Value,
                domainEvent.Status.ToString(),
                domainEvent.OccurredAt)
        ];
}
