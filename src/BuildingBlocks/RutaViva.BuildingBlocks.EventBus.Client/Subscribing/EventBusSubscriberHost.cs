using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal sealed class EventBusSubscriberHost(
    IEventBusConnectionFactory connectionFactory,
    IntegrationEventDispatcher dispatcher,
    IServiceScopeFactory scopeFactory,
    EventHandlerRegistry registry,
    SubscriberOptions options,
    ILogger<EventBusSubscriberHost> logger) : BackgroundService
{
    private const int PollIntervalSeconds = 5;
    private const int BatchSize = 100;
    private const int SignalCapacity = 1;
    private const long WakeSignal = 0;
    private const string CycleFailedTemplate = "Event subscriber {Subscriber} cycle failed and will be retried";
    private const string ResubscribeFailedTemplate = "Event subscriber {Subscriber} could not subscribe after reconnecting";
    private const string BatchStoppedTemplate = "Event subscriber {Subscriber} stopped the batch at offset {Offset}: {Error}";
    private const string NoStreamsMessage = "The event subscriber '{0}' has no registered handlers. Register at least one with Handle or HandleFamily.";

    private readonly Channel<long> _signal = Channel.CreateBounded<long>(
        new BoundedChannelOptions(SignalCapacity) { FullMode = BoundedChannelFullMode.DropOldest });

    private readonly string[] _streams = registry.StreamNames();
    private bool _positionInitialized;
    private volatile bool _subscribed;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (_streams.Length == 0)
        {
            throw new InvalidOperationException(string.Format(NoStreamsMessage, options.SubscriberName));
        }

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using HubConnection connection = connectionFactory.Create();
        connection.On<string, long>(EventBusHubMethods.EventsAvailable, (_, _) => Wake());
        connection.Reconnecting += _ => ResetSubscriptionAsync();
        connection.Closed += _ => ResetSubscriptionAsync();
        connection.Reconnected += _ => OnReconnectedAsync(connection, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(connection, stoppingToken);
            await WaitForWakeAsync(stoppingToken);
        }
    }

    private void Wake() => _signal.Writer.TryWrite(WakeSignal);

    private async Task WaitForWakeAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(PollIntervalSeconds));
        try
        {
            await _signal.Reader.WaitToReadAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _signal.Reader.TryRead(out _);
    }

    private Task ResetSubscriptionAsync()
    {
        _subscribed = false;
        return Task.CompletedTask;
    }

    private async Task OnReconnectedAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureSubscribedAsync(connection, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, ResubscribeFailedTemplate, options.SubscriberName);
        }
    }

    private async Task RunCycleAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await EnsureConnectedAsync(connection, cancellationToken);
            await EnsureSubscribedAsync(connection, cancellationToken);
            await InitializePositionAsync(connection, cancellationToken);
            await DrainAsync(connection, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, CycleFailedTemplate, options.SubscriberName);
        }
    }

    private async Task EnsureConnectedAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != HubConnectionState.Disconnected)
        {
            return;
        }

        await connection.StartAsync(cancellationToken);
    }

    private async Task EnsureSubscribedAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        if (_subscribed)
        {
            return;
        }

        await connection.InvokeAsync(EventBusHubMethods.Subscribe, _streams, cancellationToken);
        _subscribed = true;
        Wake();
    }

    private async Task InitializePositionAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        if (_positionInitialized || options.StartFrom != StartPosition.Latest)
        {
            _positionInitialized = true;
            return;
        }

        long latest = await connection.InvokeAsync<long>(EventBusHubMethods.GetLatestOffset, cancellationToken);
        Result saved = await SaveCheckpointAsync(latest, cancellationToken);
        _positionInitialized = saved.IsSuccess;
    }

    private async Task<Result> SaveCheckpointAsync(long offset, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IEventCheckpointStore checkpoints = scope.ServiceProvider.GetRequiredService<IEventCheckpointStore>();
        await checkpoints.SaveAsync(options.SubscriberName, offset, cancellationToken);
        return await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
    }

    private async Task<long> ReadCheckpointAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IEventCheckpointStore checkpoints = scope.ServiceProvider.GetRequiredService<IEventCheckpointStore>();
        return await checkpoints.GetLastOffsetAsync(options.SubscriberName, cancellationToken);
    }

    private async Task DrainAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        long lastOffset = await ReadCheckpointAsync(cancellationToken);
        while (true)
        {
            IReadOnlyList<EventRecord> batch = await connection.InvokeAsync<IReadOnlyList<EventRecord>>(
                EventBusHubMethods.GetEvents, _streams, lastOffset, BatchSize, cancellationToken);
            if (batch.Count == 0 || !await DispatchBatchAsync(connection, batch, cancellationToken))
            {
                return;
            }

            lastOffset = batch[^1].Offset;
        }
    }

    private async Task<bool> DispatchBatchAsync(HubConnection connection, IReadOnlyList<EventRecord> batch, CancellationToken cancellationToken)
    {
        foreach (EventRecord record in batch)
        {
            Result result = await dispatcher.DispatchAsync(connection, record, cancellationToken);
            if (result.IsFailure)
            {
                logger.LogWarning(BatchStoppedTemplate, options.SubscriberName, record.Offset, result.Error.Message);
                return false;
            }
        }

        return true;
    }
}
