namespace RutaViva.BuildingBlocks.EventBus.Client.Connection;

public sealed class EventBusClientOptions
{
    public const string SectionName = "EventBus";

    public Uri? HubUrl { get; set; }

    public Func<HttpMessageHandler>? HttpMessageHandlerFactory { get; set; }
}
