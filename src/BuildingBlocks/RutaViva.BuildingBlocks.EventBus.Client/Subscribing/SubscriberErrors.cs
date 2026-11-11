using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.BuildingBlocks.EventBus.Client.Subscribing;

internal static class SubscriberErrors
{
    private const string PayloadInvalidCode = "EventBus.Subscriber.PayloadInvalid";
    private const string PayloadInvalidMessage = "The event payload could not be deserialized into the registered event type.";
    private const string HandlerThrewCode = "EventBus.Subscriber.HandlerThrew";

    public static readonly Error PayloadInvalid = new(PayloadInvalidCode, PayloadInvalidMessage, ErrorType.Validation);

    public static Error HandlerThrew(Exception exception) => new(HandlerThrewCode, exception.Message, ErrorType.BusinessRule);
}
