using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.EventBus.Application.Abstractions;

namespace RutaViva.EventBus.Application.Queries;

public sealed class GetLatestOffsetQueryHandler(IEventStore store) : IQueryHandler<GetLatestOffsetQuery, long>
{
    public async Task<Result<long>> HandleAsync(GetLatestOffsetQuery query, CancellationToken cancellationToken)
    {
        long offset = await store.GetLatestOffsetAsync(cancellationToken);
        return offset;
    }
}
