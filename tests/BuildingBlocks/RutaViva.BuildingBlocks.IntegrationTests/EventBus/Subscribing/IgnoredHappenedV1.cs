using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed record IgnoredHappenedV1(Guid ItemId) : IIntegrationEvent
{
    public const string EventName = "IgnoredHappened";

    public const string Stream = "testing.ignored";
}
