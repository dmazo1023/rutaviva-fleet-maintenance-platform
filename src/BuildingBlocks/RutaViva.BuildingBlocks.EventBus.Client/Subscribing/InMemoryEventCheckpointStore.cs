using System.Collections.Concurrent;
using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

public sealed class InMemoryEventCheckpointStore : IEventCheckpointStore
{
    private const long NoOffset = 0;

    private readonly ConcurrentDictionary<string, long> _offsets = new(StringComparer.Ordinal);

    public Task<long> GetLastOffsetAsync(string subscriberName, CancellationToken cancellationToken)
        => Task.FromResult(_offsets.GetValueOrDefault(subscriberName, NoOffset));

    public Task SaveAsync(string subscriberName, long offset, CancellationToken cancellationToken)
    {
        _offsets[subscriberName] = offset;
        return Task.CompletedTask;
    }
}
