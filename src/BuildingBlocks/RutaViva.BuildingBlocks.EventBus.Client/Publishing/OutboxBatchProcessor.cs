using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Infrastructure.Messaging;
using RutaViva.IntegrationContracts;

namespace RutaViva.BuildingBlocks.EventBus.Client.Publishing;

public sealed class OutboxBatchProcessor<TContext>(
    TContext context,
    IIntegrationEventTransport transport,
    IClock clock,
    ILogger<OutboxBatchProcessor<TContext>> logger)
    where TContext : DbContext
{
    public const int BatchSize = 50;

    private const string PublishedTemplate = "Outbox message {MessageId} published to stream {Stream} at offset {Offset}";
    private const string FailedTemplate = "Outbox message {MessageId} failed on attempt {Attempts}";
    private const string InvalidPayloadMessage = "The outbox payload is not a valid integration event envelope.";

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<OutboxMessage> pending = await ReadPendingAsync(cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        await DeliverInOrderAsync(pending, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<OutboxMessage>> ReadPendingAsync(CancellationToken cancellationToken) =>
        await context.Set<OutboxMessage>()
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

    private async Task DeliverInOrderAsync(IReadOnlyList<OutboxMessage> pending, CancellationToken cancellationToken)
    {
        foreach (OutboxMessage message in pending)
        {
            bool delivered = await TryDeliverAsync(message, cancellationToken);
            if (!delivered)
            {
                return;
            }
        }
    }

    private async Task<bool> TryDeliverAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            IntegrationEventEnvelope envelope = Deserialize(message);
            long offset = await transport.SendAsync(message.Stream, envelope, cancellationToken);
            message.MarkProcessed(clock.UtcNow);
            logger.LogDebug(PublishedTemplate, message.Id, message.Stream, offset);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            RegisterFailure(message, exception);
            return false;
        }
    }

    private void RegisterFailure(OutboxMessage message, Exception exception)
    {
        string error = string.IsNullOrWhiteSpace(exception.Message) ? exception.GetType().Name : exception.Message;
        message.RegisterFailure(error);
        logger.LogWarning(exception, FailedTemplate, message.Id, message.Attempts);
    }

    private static IntegrationEventEnvelope Deserialize(OutboxMessage message) =>
        JsonSerializer.Deserialize<IntegrationEventEnvelope>(message.Payload, IntegrationEventJson.Options)
        ?? throw new InvalidOperationException(InvalidPayloadMessage);
}
