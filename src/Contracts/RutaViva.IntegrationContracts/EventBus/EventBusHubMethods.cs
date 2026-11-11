namespace RutaViva.IntegrationContracts.EventBus;

public static class EventBusHubMethods
{
    public const string HubPath = "/internal/hubs/events";

    public const string Publish = "Publish";

    public const string Subscribe = "Subscribe";

    public const string GetEvents = "GetEvents";

    public const string GetLatestOffset = "GetLatestOffset";

    public const string ReportFailure = "ReportFailure";

    public const string EventsAvailable = "EventsAvailable";
}
