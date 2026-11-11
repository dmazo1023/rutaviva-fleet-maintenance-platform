using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Customers.Domain.Events;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.Customers;

namespace RutaViva.Customers.Application.IntegrationEvents.Outbound;

public sealed class CustomerRegisteredMapper : IIntegrationEventMapper<CustomerRegistered>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(CustomerRegistered domainEvent) =>
        [
            new CustomerRegisteredV1(
                domainEvent.CustomerId.Value,
                domainEvent.TaxId.Value,
                domainEvent.Name.Value,
                domainEvent.Email.Value,
                domainEvent.Status.ToString(),
                domainEvent.OccurredAt)
        ];
}
