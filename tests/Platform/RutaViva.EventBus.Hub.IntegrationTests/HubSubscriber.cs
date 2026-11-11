using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.EventBus.Hub.IntegrationTests;

internal sealed class HubSubscriber : IAsyncDisposable
{
    private const int ReadTimeoutSeconds = 10;

    private readonly Channel<Notification> _notifications = Channel.CreateUnbounded<Notification>();
    private readonly IDisposable _registration;

    private HubSubscriber(HubConnection connection)
    {
        Connection = connection;
        _registration = connection.On<string, long>(EventBusHubMethods.EventsAvailable, OnEventsAvailable);
    }

    public HubConnection Connection { get; }

    public static async Task<HubSubscriber> ConnectAsync(EventBusHubFactory factory, string stream, CancellationToken cancellationToken)
    {
        HubSubscriber subscriber = new(BuildConnection(factory));
        await subscriber.Connection.StartAsync(cancellationToken);
        await subscriber.Connection.InvokeAsync(EventBusHubMethods.Subscribe, new[] { stream }, cancellationToken);
        return subscriber;
    }

    public async Task<Notification> NextAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(ReadTimeoutSeconds));
        return await _notifications.Reader.ReadAsync(timeout.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _registration.Dispose();
        await Connection.DisposeAsync();
    }

    private static HubConnection BuildConnection(EventBusHubFactory factory)
    {
        Uri address = new(factory.Server.BaseAddress, EventBusHubMethods.HubPath);
        return new HubConnectionBuilder()
            .WithUrl(address, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }

    private void OnEventsAvailable(string stream, long offset) =>
        _notifications.Writer.TryWrite(new Notification(stream, offset));
}
