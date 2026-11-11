using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Customers;

public sealed record CustomerStatusChangedV1(Guid CustomerId, string Status, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "CustomerStatusChanged";

    public const string Stream = Streams.CustomersCustomer;
}
