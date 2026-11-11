namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed record FamilySecondV1(Guid ItemId) : IFamilyEvent
{
    public const string EventName = "FamilySecond";

    public const string Stream = "testing.family";

    public const int Version = 1;
}
