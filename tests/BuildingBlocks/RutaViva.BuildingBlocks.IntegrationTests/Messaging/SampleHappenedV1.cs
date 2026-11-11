using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.BuildingBlocks.IntegrationTests.Messaging;

public sealed record SampleHappenedV1(Guid OrderId) : IIntegrationEvent
{
    public const string EventName = "SampleHappened";

    public const string Stream = "testing.order";
}
