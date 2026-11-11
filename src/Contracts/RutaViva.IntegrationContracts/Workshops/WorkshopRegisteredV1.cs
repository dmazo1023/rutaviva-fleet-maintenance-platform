using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Workshops;

public sealed record WorkshopRegisteredV1(Guid WorkshopId, string TaxId, string Name, string City, string Status, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "WorkshopRegistered";

    public const string Stream = Streams.WorkshopsWorkshop;
}
