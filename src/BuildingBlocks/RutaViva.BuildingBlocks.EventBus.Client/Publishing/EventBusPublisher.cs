using Microsoft.AspNetCore.SignalR.Client;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.BuildingBlocks.EventBus.Client.Publishing;

public sealed class EventBusPublisher(IEventBusConnectionFactory connectionFactory) : IIntegrationEventTransport, IAsyncDisposable
{
    private const string StartFailedMessage = "The event bus connection could not be started.";
    private const string ConnectTimeoutMessage = "The event bus connection was not established in time.";
    private const string ConnectionLostMessage = "The event bus connection was lost while waiting to connect.";
    private const int ConnectTimeoutSeconds = 10;
    private const int ConnectPollMilliseconds = 50;

    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(ConnectTimeoutSeconds);
    private static readonly TimeSpan ConnectPollInterval = TimeSpan.FromMilliseconds(ConnectPollMilliseconds);

    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly CancellationTokenSource _disposal = new();
    private HubConnection? _connection;

    public async Task<long> SendAsync(string stream, IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposal.Token);
        HubConnection connection = await EnsureConnectedAsync(linked.Token);
        return await connection.InvokeAsync<long>(EventBusHubMethods.Publish, stream, envelope, linked.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _disposal.CancelAsync();
        await _startLock.WaitAsync();
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _startLock.Dispose();
        _disposal.Dispose();
    }

    private async Task<HubConnection> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        HubConnection? current = _connection;
        if (current is { State: HubConnectionState.Connected })
        {
            return current;
        }

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            HubConnection connection = _connection ??= connectionFactory.Create();
            await StartIfDisconnectedAsync(connection, cancellationToken);
            await WaitUntilConnectedAsync(connection, cancellationToken);
            return connection;
        }
        finally
        {
            _startLock.Release();
        }
    }

    private static async Task StartIfDisconnectedAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != HubConnectionState.Disconnected)
        {
            return;
        }

        try
        {
            await connection.StartAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new EventBusUnavailableException(StartFailedMessage, exception);
        }
    }

    private static async Task WaitUntilConnectedAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ConnectTimeout);
        try
        {
            await PollUntilConnectedAsync(connection, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EventBusUnavailableException(ConnectTimeoutMessage);
        }
    }

    private static async Task PollUntilConnectedAsync(HubConnection connection, CancellationToken cancellationToken)
    {
        while (connection.State != HubConnectionState.Connected)
        {
            if (connection.State == HubConnectionState.Disconnected)
            {
                throw new EventBusUnavailableException(ConnectionLostMessage);
            }

            await Task.Delay(ConnectPollInterval, cancellationToken);
        }
    }
}
