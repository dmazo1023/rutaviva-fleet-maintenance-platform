using RutaViva.IntegrationContracts;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal sealed class EventHandlerRegistry
{
    private const string DuplicateEventMessage = "The event '{0}' already has a registered handler.";

    private readonly Dictionary<IntegrationEventType, IEventDispatchStrategy> _strategies = [];
    private readonly HashSet<string> _streams = new(StringComparer.Ordinal);

    public void Register(IntegrationEventType eventType, string stream, IEventDispatchStrategy strategy)
    {
        if (!_strategies.TryAdd(eventType, strategy))
        {
            throw new InvalidOperationException(string.Format(DuplicateEventMessage, eventType));
        }

        _streams.Add(stream);
    }

    public IEventDispatchStrategy Resolve(IntegrationEventType eventType)
        => _strategies.GetValueOrDefault(eventType, SkippedEventStrategy.Instance);

    public string[] StreamNames() => [.. _streams];
}
