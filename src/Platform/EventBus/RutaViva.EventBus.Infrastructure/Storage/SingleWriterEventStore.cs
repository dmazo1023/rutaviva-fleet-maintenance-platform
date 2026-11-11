using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.EventBus.Application.Abstractions;
using RutaViva.EventBus.Infrastructure.Persistence;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.EventBus.Infrastructure.Storage;

public sealed class SingleWriterEventStore(IDbContextFactory<EventBusDbContext> contextFactory, IClock clock) : IEventStore, IDisposable
{
    private const int SingleWriter = 1;
    private const long NoEvents = 0;
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    private const string DuplicateWithoutRowMessage = "A duplicate message id was reported but no stored event was found.";
    private const string InvalidPayloadMessage = "The stored event payload is not a valid envelope.";

    private readonly SemaphoreSlim _writeGate = new(SingleWriter, SingleWriter);

    public async Task<long> AppendAsync(string stream, IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            return await AppendSerializedAsync(stream, envelope, cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<IReadOnlyList<EventRecord>> ReadAsync(IReadOnlyCollection<string> streams, long afterOffset, int max, CancellationToken cancellationToken)
    {
        string[] streamNames = [.. streams];
        await using EventBusDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        List<StoredEvent> rows = await context.Events
            .AsNoTracking()
            .Where(stored => streamNames.Contains(stored.Stream) && stored.Offset > afterOffset)
            .OrderBy(stored => stored.Offset)
            .Take(max)
            .ToListAsync(cancellationToken);
        return [.. rows.Select(ToRecord)];
    }

    public async Task<long> GetLatestOffsetAsync(CancellationToken cancellationToken)
    {
        await using EventBusDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        long? latest = await context.Events.MaxAsync(stored => (long?)stored.Offset, cancellationToken);
        return latest ?? NoEvents;
    }

    public async Task AddDeadLetterAsync(string subscriberName, long offset, string error, CancellationToken cancellationToken)
    {
        await using EventBusDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.DeadLetters.Add(DeadLetter.Create(subscriberName, offset, error, clock.UtcNow));
        await context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose() => _writeGate.Dispose();

    private static bool IsDuplicateMessage(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };

    private static string Serialize(IntegrationEventEnvelope envelope) =>
        JsonSerializer.Serialize(envelope, IntegrationEventJson.Options);

    private static IntegrationEventEnvelope Deserialize(string payload) =>
        JsonSerializer.Deserialize<IntegrationEventEnvelope>(payload, IntegrationEventJson.Options)
        ?? throw new InvalidOperationException(InvalidPayloadMessage);

    private static EventRecord ToRecord(StoredEvent stored) =>
        new(stored.Offset, stored.Stream, Deserialize(stored.Payload));

    private async Task<long> AppendSerializedAsync(string stream, IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        long? existing = await FindOffsetAsync(envelope.Id, cancellationToken);
        if (existing is not null)
        {
            return existing.Value;
        }

        return await InsertOrReadExistingAsync(stream, envelope, cancellationToken);
    }

    private async Task<long> InsertOrReadExistingAsync(string stream, IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        try
        {
            return await InsertAsync(stream, envelope, cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateMessage(exception))
        {
            long? existing = await FindOffsetAsync(envelope.Id, cancellationToken);
            return existing ?? throw new InvalidOperationException(DuplicateWithoutRowMessage);
        }
    }

    private async Task<long> InsertAsync(string stream, IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        await using EventBusDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        StoredEvent stored = StoredEvent.Create(stream, envelope, Serialize(envelope), clock.UtcNow);
        context.Events.Add(stored);
        await context.SaveChangesAsync(cancellationToken);
        return stored.Offset;
    }

    private async Task<long?> FindOffsetAsync(Guid messageId, CancellationToken cancellationToken)
    {
        await using EventBusDbContext context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Events
            .AsNoTracking()
            .Where(stored => stored.MessageId == messageId)
            .Select(stored => (long?)stored.Offset)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
