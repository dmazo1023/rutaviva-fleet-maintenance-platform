using System.Collections.Concurrent;
using System.Reflection;
using RutaViva.IntegrationContracts.Abstractions;

namespace RutaViva.IntegrationContracts;

public sealed record IntegrationEventDescriptor(Type ClrType, string Stream, IntegrationEventType EventType)
{
    private const string EventNameField = "EventName";
    private const string StreamField = "Stream";
    private const string VersionField = "Version";
    private const string MissingConstantMessage = "The integration event type '{0}' must declare a public constant named '{1}'.";
    private const string NotIntegrationEventMessage = "The type '{0}' is not a concrete IIntegrationEvent.";

    private static readonly ConcurrentDictionary<Type, IntegrationEventDescriptor> Cache = new();

    public static IntegrationEventDescriptor For<TEvent>()
        where TEvent : IIntegrationEvent => For(typeof(TEvent));

    public static IntegrationEventDescriptor For(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return Cache.GetOrAdd(eventType, Read);
    }

    public static bool IsDescribable(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return IsConcreteIntegrationEvent(type)
            && HasConstant(type, EventNameField)
            && HasConstant(type, StreamField)
            && HasConstant(type, VersionField);
    }

    private static IntegrationEventDescriptor Read(Type eventType)
    {
        if (!IsConcreteIntegrationEvent(eventType))
        {
            throw new ArgumentException(string.Format(NotIntegrationEventMessage, eventType.Name), nameof(eventType));
        }

        string stream = ReadConstant<string>(eventType, StreamField);
        string name = ReadConstant<string>(eventType, EventNameField);
        int version = ReadConstant<int>(eventType, VersionField);
        return new IntegrationEventDescriptor(eventType, stream, IntegrationEventType.ForStream(stream, name, version));
    }

    private static bool IsConcreteIntegrationEvent(Type type) =>
        type is { IsClass: true, IsAbstract: false } && typeof(IIntegrationEvent).IsAssignableFrom(type);

    private static bool HasConstant(Type type, string fieldName) => FindConstant(type, fieldName) is not null;

    private static T ReadConstant<T>(Type eventType, string fieldName) =>
        FindConstant(eventType, fieldName) is T value
            ? value
            : throw new InvalidOperationException(string.Format(MissingConstantMessage, eventType.Name, fieldName));

    private static object? FindConstant(Type type, string fieldName) =>
        type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue();
}
