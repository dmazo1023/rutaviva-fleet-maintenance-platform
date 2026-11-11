using System.Text.Json;
using System.Text.Json.Serialization;
using RutaViva.Maintenance.Domain.Common;

namespace RutaViva.Maintenance.Api.Serialization;

public sealed class MaintenanceIdJsonConverterFactory : JsonConverterFactory
{
    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(VehicleId),
        typeof(RoutineId),
        typeof(RoutineTypeId),
        typeof(AlertId),
        typeof(CustomerId)
    ];

    public override bool CanConvert(Type typeToConvert) => SupportedTypes.Contains(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(MaintenanceIdJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
