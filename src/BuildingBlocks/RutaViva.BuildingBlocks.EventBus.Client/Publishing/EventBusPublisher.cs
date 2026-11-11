using Microsoft.AspNetCore.SignalR.Client;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;
using RutaViva.IntegrationContracts;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.BuildingBlocks.EventBus.Client.Publishing;

public sealed class EventBusPublisher : IIntegrationEventTransport, IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly SemaphoreSlim _startLock = new(1, 1);

    public EventBusPublisher(IEventBusConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.Create();
    }

    public async Task<long> SendAsync(string stream, IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        await EnsureStartedAsync(cancellationToken);
        return await _connection.InvokeAsync<long>(EventBusHubMethods.Publish, stream, envelope, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        _startLock.Dispose();
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_connection.State == HubConnectionState.Connected)
        {
            return;
        }

        await _startLock.WaitAsync(cancellationToken);
        try
        {
            await StartWhenDisconnectedAsync(cancellationToken);
        }
        finally
        {
            _startLock.Release();
        }
    }

    private Task StartWhenDisconnectedAsync(CancellationToken cancellationToken) =>
        _connection.State == HubConnectionState.Disconnected
            ? _connection.StartAsync(cancellationToken)
            : Task.CompletedTask;
}
