using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using RutaViva.IntegrationContracts.EventBus;

namespace RutaViva.BuildingBlocks.EventBus.Client.Connection;

public sealed class EventBusConnectionFactory(IOptions<EventBusClientOptions> options) : IEventBusConnectionFactory
{
    private const string MissingHubUrlMessage = "EventBus:HubUrl is not configured.";

    public HubConnection Create() =>
        new HubConnectionBuilder()
            .WithUrl(HubAddress(), ConfigureTransport)
            .WithAutomaticReconnect()
            .Build();

    private Uri HubAddress()
    {
        Uri baseAddress = options.Value.HubUrl ?? throw new InvalidOperationException(MissingHubUrlMessage);
        return new Uri(baseAddress, EventBusHubMethods.HubPath);
    }

    private void ConfigureTransport(HttpConnectionOptions connection)
    {
        Func<HttpMessageHandler>? handlerFactory = options.Value.HttpMessageHandlerFactory;
        if (handlerFactory is null)
        {
            return;
        }

        connection.HttpMessageHandlerFactory = _ => handlerFactory();
        connection.Transports = HttpTransportType.LongPolling;
    }
}
