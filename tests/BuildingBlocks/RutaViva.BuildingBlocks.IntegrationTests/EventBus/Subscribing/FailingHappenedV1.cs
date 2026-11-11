using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed record FailingHappenedV1(Guid ItemId) : IIntegrationEvent
{
    public const string EventName = "FailingHappened";

    public const string Stream = "testing.failing";

    public const int Version = 1;
}
