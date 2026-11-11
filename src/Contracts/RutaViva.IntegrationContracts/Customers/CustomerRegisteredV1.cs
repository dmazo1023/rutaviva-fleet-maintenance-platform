using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Customers;

public sealed record CustomerRegisteredV1(Guid CustomerId, string TaxId, string Name, string Email, string Status, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "CustomerRegistered";

    public const string Stream = Streams.CustomersCustomer;
}
