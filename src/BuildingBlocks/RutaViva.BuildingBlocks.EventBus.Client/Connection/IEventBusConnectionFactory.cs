using Microsoft.AspNetCore.SignalR.Client;

namespace RutaViva.BuildingBlocks.EventBus.Client.Connection;

public interface IEventBusConnectionFactory
{
    HubConnection Create();
}
