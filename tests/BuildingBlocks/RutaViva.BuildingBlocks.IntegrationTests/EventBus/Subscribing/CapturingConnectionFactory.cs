using Microsoft.AspNetCore.SignalR.Client;
using RutaViva.BuildingBlocks.EventBus.Client.Connection;

namespace RutaViva.BuildingBlocks.IntegrationTests.EventBus.Subscribing;

public sealed class CapturingConnectionFactory(IEventBusConnectionFactory inner) : IEventBusConnectionFactory
{
    public HubConnection? Connection { get; private set; }

    public HubConnection Create()
    {
        HubConnection created = inner.Create();
        Connection = created;
        return created;
    }
}
