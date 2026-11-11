using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.EventBus.Application.Queries;

public sealed class GetEventsQueryHandler(IEventStore store) : IQueryHandler<GetEventsQuery, IReadOnlyList<EventRecord>>
{
    public async Task<Result<IReadOnlyList<EventRecord>>> HandleAsync(GetEventsQuery query, CancellationToken cancellationToken)
    {
        int pageSize = Math.Min(query.Max, GetEventsQuery.MaxPageSize);
        IReadOnlyList<EventRecord> records = await store.ReadAsync(query.Streams, query.AfterOffset, pageSize, cancellationToken);
        return Result<IReadOnlyList<EventRecord>>.Success(records);
    }
}
