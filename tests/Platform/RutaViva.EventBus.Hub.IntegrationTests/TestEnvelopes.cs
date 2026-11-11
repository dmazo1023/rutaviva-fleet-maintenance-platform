using System.Text.Json;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Hub.IntegrationTests;

internal static class TestEnvelopes
{
    private const string Source = "rutaviva/tests";
    private const string EventType = "rutaviva.fleet.VehicleRegistered.v1";
    private const string Subject = "vehicle/1";
    private const string EmptyJson = "{}";

    public static IntegrationEventEnvelope Create() => Create(Guid.NewGuid());

    public static IntegrationEventEnvelope Create(Guid id) => new()
    {
        Id = id,
        Source = Source,
        Type = EventType,
        Subject = Subject,
        Time = DateTimeOffset.UtcNow,
        Data = JsonDocument.Parse(EmptyJson).RootElement.Clone(),
    };
}
