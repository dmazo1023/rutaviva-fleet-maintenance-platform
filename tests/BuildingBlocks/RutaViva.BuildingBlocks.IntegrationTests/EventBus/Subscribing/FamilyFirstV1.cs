namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed record FamilyFirstV1(Guid ItemId) : IFamilyEvent
{
    public const string EventName = "FamilyFirst";

    public const string Stream = "testing.family";
}
