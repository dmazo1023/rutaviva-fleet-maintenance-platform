using System.Text.Json;
using System.Text.Json.Serialization;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Api.Serialization;

public sealed class FleetIdJsonConverterFactory : JsonConverterFactory
{
    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(VehicleId),
        typeof(CustomerId),
        typeof(WorkshopId),
        typeof(ApproverId)
    ];

    public override bool CanConvert(Type typeToConvert) => SupportedTypes.Contains(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(FleetIdJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
