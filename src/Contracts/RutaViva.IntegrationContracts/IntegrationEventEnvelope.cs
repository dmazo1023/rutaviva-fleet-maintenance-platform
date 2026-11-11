using System.Text.Json;
using System.Text.Json.Serialization;

namespace RutaViva.IntegrationContracts;

public sealed record IntegrationEventEnvelope
{
    public const string CloudEventsSpecVersion = "1.0";

    public const string JsonContentType = "application/json";

    [JsonPropertyName("specversion")]
    public string SpecVersion { get; init; } = CloudEventsSpecVersion;

    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    [JsonPropertyName("time")]
    public required DateTimeOffset Time { get; init; }

    [JsonPropertyName("datacontenttype")]
    public string DataContentType { get; init; } = JsonContentType;

    [JsonPropertyName("traceparent")]
    public string? TraceParent { get; init; }

    [JsonPropertyName("data")]
    public required JsonElement Data { get; init; }
}
