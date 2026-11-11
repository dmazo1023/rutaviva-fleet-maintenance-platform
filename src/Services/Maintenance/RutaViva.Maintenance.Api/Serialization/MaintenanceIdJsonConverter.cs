using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RutaViva.Maintenance.Api.Serialization;

internal sealed class MaintenanceIdJsonConverter<TId> : JsonConverter<TId>
    where TId : struct
{
    private const string ValuePropertyName = "Value";
    private const string InvalidIdentifierMessage = "The identifier must be a non-empty GUID.";

    private static readonly PropertyInfo ValueProperty = typeof(TId).GetProperty(ValuePropertyName)!;

    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!reader.TryGetGuid(out Guid value) || value == Guid.Empty)
        {
            throw new JsonException(InvalidIdentifierMessage);
        }

        return (TId)Activator.CreateInstance(typeof(TId), value)!;
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue((Guid)ValueProperty.GetValue(value)!);
    }
}
