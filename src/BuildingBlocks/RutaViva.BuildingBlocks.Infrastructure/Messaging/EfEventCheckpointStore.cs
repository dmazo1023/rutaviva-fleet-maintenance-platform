using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Application.Abstractions;

namespace RutaViva.BuildingBlocks.Infrastructure.Messaging;

public sealed class EfEventCheckpointStore<TContext>(TContext context, IClock clock) : IEventCheckpointStore
    where TContext : DbContext
{
    private const long NoOffset = 0;

    public async Task<long> GetLastOffsetAsync(string subscriberName, CancellationToken cancellationToken)
    {
        EventCheckpoint? checkpoint = await FindAsync(subscriberName, cancellationToken);
        return checkpoint?.LastOffset ?? NoOffset;
    }

    public async Task SaveAsync(string subscriberName, long offset, CancellationToken cancellationToken)
    {
        EventCheckpoint? checkpoint = await FindAsync(subscriberName, cancellationToken);
        if (checkpoint is null)
        {
            context.Set<EventCheckpoint>().Add(new EventCheckpoint(subscriberName, offset, clock.UtcNow));
            return;
        }

        checkpoint.MoveTo(offset, clock.UtcNow);
    }

    private async Task<EventCheckpoint?> FindAsync(string subscriberName, CancellationToken cancellationToken)
        => await context.Set<EventCheckpoint>().FindAsync([subscriberName], cancellationToken);
}
