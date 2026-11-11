using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Abstractions;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal sealed class IntegrationEventDispatcher(
    IServiceScopeFactory scopeFactory,
    EventHandlerRegistry registry,
    SubscriberOptions options,
    TimeProvider timeProvider,
    ILogger<IntegrationEventDispatcher> logger)
{
    private const string FailureFormat = "{0}: {1}";
    private const string FailureReportedTemplate = "Event at offset {Offset} failed for subscriber {Subscriber} after all retries and was reported: {Error}";

    private readonly ResiliencePipeline<Result> _pipeline = SubscriberRetryPipeline.Create(timeProvider);

    public async Task<Result> DispatchAsync(HubConnection connection, EventRecord record, CancellationToken cancellationToken)
    {
        IEventDispatchStrategy strategy = registry.Resolve(EventNames.FromEnvelopeType(record.Envelope.Type));
        Result outcome = await _pipeline.ExecuteAsync(token => new ValueTask<Result>(AttemptAsync(strategy, record, token)), cancellationToken);
        return outcome.IsSuccess ? outcome : await RecordFailureAsync(connection, record, outcome.Error, cancellationToken);
    }

    private static EventMetadata ToMetadata(EventRecord record)
        => new(record.Envelope.Id, record.Offset, record.Envelope.Type, record.Envelope.Subject, record.Envelope.Time);

    private static string FormatError(Error error) => string.Format(FailureFormat, error.Code, error.Message);

    private async Task<Result> AttemptAsync(IEventDispatchStrategy strategy, EventRecord record, CancellationToken cancellationToken)
    {
        try
        {
            return await ProcessAsync(strategy, record, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result.Failure(SubscriberErrors.HandlerThrew(exception));
        }
    }

    private async Task<Result> ProcessAsync(IEventDispatchStrategy strategy, EventRecord record, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        IEventCheckpointStore checkpoints = services.GetRequiredService<IEventCheckpointStore>();
        long current = await checkpoints.GetLastOffsetAsync(options.SubscriberName, cancellationToken);
        if (record.Offset <= current)
        {
            return Result.Success();
        }

        Result handled = await strategy.ExecuteAsync(services, record.Envelope, ToMetadata(record), cancellationToken);
        return handled.IsFailure ? handled : await CommitAsync(services, record.Offset, cancellationToken);
    }

    private async Task<Result> CommitAsync(IServiceProvider services, long offset, CancellationToken cancellationToken)
    {
        IEventCheckpointStore checkpoints = services.GetRequiredService<IEventCheckpointStore>();
        await checkpoints.SaveAsync(options.SubscriberName, offset, cancellationToken);
        return await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
    }

    private async Task<Result> RecordFailureAsync(HubConnection connection, EventRecord record, Error error, CancellationToken cancellationToken)
    {
        string description = FormatError(error);
        await connection.InvokeAsync(EventBusHubMethods.ReportFailure, options.SubscriberName, record.Offset, description, cancellationToken);
        logger.LogError(FailureReportedTemplate, record.Offset, options.SubscriberName, description);
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        return await CommitAsync(scope.ServiceProvider, record.Offset, cancellationToken);
    }
}
