using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Customers;

public sealed record ApproverStatusChangedV1(Guid ApproverId, Guid CustomerId, string Status, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "ApproverStatusChanged";

    public const int Version = 1;

    public const string Stream = Streams.CustomersCustomer;
}
