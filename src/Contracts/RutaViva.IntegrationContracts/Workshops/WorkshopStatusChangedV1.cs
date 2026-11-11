using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts.Workshops;

public sealed record WorkshopStatusChangedV1(Guid WorkshopId, string Status, DateTimeOffset OccurredAt) : IIntegrationEvent
{
    public const string EventName = "WorkshopStatusChanged";

    public const string Stream = Streams.WorkshopsWorkshop;
}
