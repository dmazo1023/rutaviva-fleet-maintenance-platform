namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal sealed class EventHandlerRegistry
{
    private const string DuplicateEventMessage = "The event '{0}' already has a registered handler.";

    private readonly Dictionary<string, IEventDispatchStrategy> _strategies = new(StringComparer.Ordinal);
    private readonly HashSet<string> _streams = new(StringComparer.Ordinal);

    public void Register(string eventName, string stream, IEventDispatchStrategy strategy)
    {
        if (!_strategies.TryAdd(eventName, strategy))
        {
            throw new InvalidOperationException(string.Format(DuplicateEventMessage, eventName));
        }

        _streams.Add(stream);
    }

    public IEventDispatchStrategy Resolve(string eventName)
        => _strategies.GetValueOrDefault(eventName, SkippedEventStrategy.Instance);

    public string[] StreamNames() => [.. _streams];
}
