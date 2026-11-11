using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RutaViva.BuildingBlocks.Infrastructure.DomainEvents;

public sealed class DomainEventsToOutboxInterceptor(
    DomainEventCollector collector,
    DomainEventHandlerDispatcher dispatcher,
    OutboxEventWriter writer) : SaveChangesInterceptor
{
    public const int MaxRounds = 5;

    private const string CycleMessage = "Domain events were still pending after {0} dispatch rounds; a handler cycle is likely.";
    private const string SynchronousSaveMessage = "Saving aggregates with pending domain events requires SaveChangesAsync.";

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchRoundsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null && collector.HasPending(eventData.Context))
        {
            throw new NotSupportedException(SynchronousSaveMessage);
        }

        return base.SavingChanges(eventData, result);
    }

    private async Task DispatchRoundsAsync(DbContext context, CancellationToken cancellationToken)
    {
        for (int round = 0; round < MaxRounds; round++)
        {
            IReadOnlyList<CollectedDomainEvent> events = collector.CollectAndClear(context);
            if (events.Count == 0)
            {
                return;
            }

            await dispatcher.DispatchAsync(events, cancellationToken);
            writer.Write(context, events);
        }

        EnsureNoPendingEvents(context);
    }

    private void EnsureNoPendingEvents(DbContext context)
    {
        if (collector.HasPending(context))
        {
            throw new InvalidOperationException(string.Format(CycleMessage, MaxRounds));
        }
    }
}
