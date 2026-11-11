using System.Text.Json;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public static class IntegrationEventJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
