using System.Text.Json;

namespace RutaViva.IntegrationContracts.Tests;

public sealed class IntegrationEventContractTests
{
    private const int ExpectedEventCount = 26;

    public static TheoryData<string> EventNames => new(IntegrationEventCatalog.EventTypes.Select(IntegrationEventCatalog.NameOf));

    [Fact]
    public void Catalog_Should_ContainAllEventsWithSamples_When_DiscoveredByReflection()
    {
        string[] discovered = IntegrationEventCatalog.EventTypes.Select(IntegrationEventCatalog.NameOf).ToArray();

        discovered.Should().HaveCount(ExpectedEventCount);
        discovered.Should().OnlyHaveUniqueItems();
        discovered.Should().BeEquivalentTo(IntegrationEventSamples.EventNames);
    }

    [Theory]
    [MemberData(nameof(EventNames))]
    public void Event_Should_MatchApprovedSnapshot_When_WrappedInEnvelope(string eventName)
    {
        IntegrationEventEnvelope envelope = IntegrationEventSamples.CreateEnvelope(eventName);

        ContractSnapshot.Verify(eventName, ContractSnapshot.Serialize(envelope));
    }

    [Theory]
    [MemberData(nameof(EventNames))]
    public void Event_Should_RoundTrip_When_SerializedAndDeserialized(string eventName)
    {
        IntegrationEventEnvelope envelope = IntegrationEventSamples.CreateEnvelope(eventName);
        string json = ContractSnapshot.Serialize(envelope);

        IntegrationEventEnvelope restored = JsonSerializer.Deserialize<IntegrationEventEnvelope>(json, ContractSnapshot.Options)!;
        object? restoredEvent = restored.Data.Deserialize(IntegrationEventCatalog.TypeOf(eventName), ContractSnapshot.Options);

        Type eventType = IntegrationEventCatalog.TypeOf(eventName);
        string restoredJson = JsonSerializer.Serialize(restoredEvent, eventType, ContractSnapshot.Options);
        string expectedJson = JsonSerializer.Serialize(IntegrationEventSamples.Create(eventName), eventType, ContractSnapshot.Options);

        restoredEvent.Should().BeOfType(eventType);
        restoredJson.Should().Be(expectedJson);
        ContractSnapshot.Serialize(restored).Should().Be(json);
    }
}
