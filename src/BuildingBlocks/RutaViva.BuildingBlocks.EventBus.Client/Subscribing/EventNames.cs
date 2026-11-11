using System.Reflection;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal static class EventNames
{
    private const string EventNameField = "EventName";
    private const char Separator = '.';
    private const int EnvelopeTypeSegments = 4;
    private const int EventNameSegment = 2;
    private const string MissingConstantMessage = "The integration event type '{0}' must declare a string constant named '{1}'.";

    public static string FromEnvelopeType(string envelopeType)
    {
        string[] segments = envelopeType.Split(Separator);
        return segments.Length == EnvelopeTypeSegments ? segments[EventNameSegment] : envelopeType;
    }

    public static string FromEventType(Type eventType)
        => Find(eventType) ?? throw new InvalidOperationException(string.Format(MissingConstantMessage, eventType.Name, EventNameField));

    public static bool IsDeclaredBy(Type eventType) => Find(eventType) is not null;

    private static string? Find(Type eventType)
    {
        FieldInfo? field = eventType.GetField(EventNameField, BindingFlags.Public | BindingFlags.Static);
        return field?.GetRawConstantValue() as string;
    }
}
