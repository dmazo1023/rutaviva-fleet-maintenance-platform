using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.EventBus.Application.Abstractions;

public interface IEventStore
{
    Task<long> AppendAsync(string stream, IntegrationEventEnvelope envelope, CancellationToken cancellationToken);

    Task<IReadOnlyList<EventRecord>> ReadAsync(IReadOnlyCollection<string> streams, long afterOffset, int max, CancellationToken cancellationToken);

    Task<long> GetLatestOffsetAsync(CancellationToken cancellationToken);

    Task AddDeadLetterAsync(string subscriberName, long offset, string error, CancellationToken cancellationToken);
}
