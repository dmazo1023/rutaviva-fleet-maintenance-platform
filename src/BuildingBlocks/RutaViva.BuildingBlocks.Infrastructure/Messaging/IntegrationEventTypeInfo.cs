using System.Collections.Concurrent;
using System.Reflection;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

internal sealed record IntegrationEventTypeInfo(string EventName, string Stream)
{
    private const string EventNameField = "EventName";
    private const string StreamField = "Stream";
    private const string MissingConstantMessage = "The integration event type '{0}' must declare a string constant named '{1}'.";

    private static readonly ConcurrentDictionary<Type, IntegrationEventTypeInfo> Cache = new();

    public static IntegrationEventTypeInfo For(Type eventType) => Cache.GetOrAdd(eventType, Read);

    private static IntegrationEventTypeInfo Read(Type eventType)
        => new(ReadConstant(eventType, EventNameField), ReadConstant(eventType, StreamField));

    private static string ReadConstant(Type eventType, string fieldName)
    {
        FieldInfo? field = eventType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        object? value = field?.GetRawConstantValue();
        return value as string
            ?? throw new InvalidOperationException(string.Format(MissingConstantMessage, eventType.Name, fieldName));
    }
}
