using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.ServiceOrders.Domain.Common;

public static class ServiceOrderErrors
{
    private const string NotFoundCode = "ServiceOrder.NotFound";
    private const string IdentifierEmptyCode = "ServiceOrderId.Empty";
    private const string InvalidTransitionCode = "ServiceOrder.InvalidTransition";
    private const string AmountInvalidCode = "ServiceOrder.AmountInvalid";
    private const string ServicedKmInvalidCode = "ServiceOrder.ServicedKmInvalid";
    private const string ReferenceRequiredCode = "ServiceOrder.ReferenceRequired";
    private const string TextRequiredCode = "ServiceOrder.TextRequired";
    private const string TextTooLongCode = "ServiceOrder.TextTooLong";
    private const string OpenOrderExistsCode = "ServiceOrder.OpenOrderExists";
    private const string ThresholdInvalidCode = "ServiceOrder.ApprovalThresholdInvalid";

    private const string NotFoundMessage = "The service order was not found.";
    private const string IdentifierEmptyMessage = "The service order identifier cannot be an empty value.";
    private const string InvalidTransitionMessage = "The service order cannot perform this action in its current status.";
    private const string AmountInvalidMessage = "The quoted amount must be greater than zero.";
    private const string ServicedKmInvalidMessage = "The serviced kilometers cannot be negative.";
    private const string ReferenceRequiredMessage = "The vehicle, routine, customer and workshop identifiers are required.";
    private const string TextRequiredMessage = "The plate, routine, workshop and actor names are required.";
    private const string TextTooLongMessage = "A text value exceeds the maximum length.";
    private const string OpenOrderExistsMessage = "The routine already has an open service order.";
    private const string ThresholdInvalidMessage = "The auto approval threshold cannot be negative.";

    public static readonly Error NotFound = new(NotFoundCode, NotFoundMessage, ErrorType.NotFound);

    public static readonly Error IdentifierEmpty = new(IdentifierEmptyCode, IdentifierEmptyMessage, ErrorType.Validation);

    public static readonly Error InvalidTransition = new(InvalidTransitionCode, InvalidTransitionMessage, ErrorType.Conflict);

    public static readonly Error AmountInvalid = new(AmountInvalidCode, AmountInvalidMessage, ErrorType.Validation);

    public static readonly Error ServicedKmInvalid = new(ServicedKmInvalidCode, ServicedKmInvalidMessage, ErrorType.Validation);

    public static readonly Error ReferenceRequired = new(ReferenceRequiredCode, ReferenceRequiredMessage, ErrorType.Validation);

    public static readonly Error TextRequired = new(TextRequiredCode, TextRequiredMessage, ErrorType.Validation);

    public static readonly Error TextTooLong = new(TextTooLongCode, TextTooLongMessage, ErrorType.Validation);

    public static readonly Error OpenOrderExists = new(OpenOrderExistsCode, OpenOrderExistsMessage, ErrorType.Conflict);

    public static readonly Error ThresholdInvalid = new(ThresholdInvalidCode, ThresholdInvalidMessage, ErrorType.Validation);
}
