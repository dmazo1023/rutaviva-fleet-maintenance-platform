namespace RutaViva.BuildingBlocks.EventBus.Client.Publishing;

public sealed class EventBusUnavailableException : Exception
{
    public EventBusUnavailableException(string message)
        : base(message)
    {
    }

    public EventBusUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
