using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.EventBus.Application;

public static class EventBusErrors
{
    private const string EnvelopeMissingCode = "EventBus.Envelope.Missing";
    private const string EnvelopeIdMissingCode = "EventBus.Envelope.IdMissing";
    private const string EnvelopeSourceMissingCode = "EventBus.Envelope.SourceMissing";
    private const string EnvelopeSourceTooLongCode = "EventBus.Envelope.SourceTooLong";
    private const string EnvelopeTypeMissingCode = "EventBus.Envelope.TypeMissing";
    private const string EnvelopeTypeInvalidCode = "EventBus.Envelope.TypeInvalid";
    private const string EnvelopeTypeTooLongCode = "EventBus.Envelope.TypeTooLong";
    private const string EnvelopeSubjectMissingCode = "EventBus.Envelope.SubjectMissing";
    private const string EnvelopeSubjectTooLongCode = "EventBus.Envelope.SubjectTooLong";
    private const string EnvelopeTimeMissingCode = "EventBus.Envelope.TimeMissing";
    private const string EnvelopeDataMissingCode = "EventBus.Envelope.DataMissing";
    private const string StreamMissingCode = "EventBus.Stream.Missing";
    private const string StreamTooLongCode = "EventBus.Stream.TooLong";
    private const string StreamsMissingCode = "EventBus.Streams.Missing";
    private const string OffsetInvalidCode = "EventBus.Offset.Invalid";
    private const string MaxInvalidCode = "EventBus.Max.Invalid";
    private const string SubscriberMissingCode = "EventBus.Subscriber.Missing";
    private const string SubscriberTooLongCode = "EventBus.Subscriber.TooLong";
    private const string FailureErrorMissingCode = "EventBus.FailureError.Missing";
    private const string FailureErrorTooLongCode = "EventBus.FailureError.TooLong";

    private const string EnvelopeMissingMessage = "The envelope is required.";
    private const string EnvelopeIdMissingMessage = "The envelope id is required.";
    private const string EnvelopeSourceMissingMessage = "The envelope source is required.";
    private const string EnvelopeSourceTooLongMessage = "The envelope source exceeds the maximum allowed length.";
    private const string EnvelopeTypeMissingMessage = "The envelope type is required.";
    private const string EnvelopeTypeInvalidMessage = "The envelope type must follow the format rutaviva.service.EventName.vN.";
    private const string EnvelopeTypeTooLongMessage = "The envelope type exceeds the maximum allowed length.";
    private const string EnvelopeSubjectMissingMessage = "The envelope subject is required.";
    private const string EnvelopeSubjectTooLongMessage = "The envelope subject exceeds the maximum allowed length.";
    private const string EnvelopeTimeMissingMessage = "The envelope time is required.";
    private const string EnvelopeDataMissingMessage = "The envelope data is required.";
    private const string StreamMissingMessage = "The stream is required.";
    private const string StreamTooLongMessage = "The stream exceeds the maximum allowed length.";
    private const string StreamsMissingMessage = "At least one stream is required.";
    private const string OffsetInvalidMessage = "The offset must not be negative.";
    private const string MaxInvalidMessage = "The max value must be greater than zero.";
    private const string SubscriberMissingMessage = "The subscriber name is required.";
    private const string SubscriberTooLongMessage = "The subscriber name exceeds the maximum allowed length.";
    private const string FailureErrorMissingMessage = "The failure error text is required.";
    private const string FailureErrorTooLongMessage = "The failure error text exceeds the maximum allowed length.";

    public static Error EnvelopeMissing { get; } = Validation(EnvelopeMissingCode, EnvelopeMissingMessage);

    public static Error EnvelopeIdMissing { get; } = Validation(EnvelopeIdMissingCode, EnvelopeIdMissingMessage);

    public static Error EnvelopeSourceMissing { get; } = Validation(EnvelopeSourceMissingCode, EnvelopeSourceMissingMessage);

    public static Error EnvelopeSourceTooLong { get; } = Validation(EnvelopeSourceTooLongCode, EnvelopeSourceTooLongMessage);

    public static Error EnvelopeTypeMissing { get; } = Validation(EnvelopeTypeMissingCode, EnvelopeTypeMissingMessage);

    public static Error EnvelopeTypeInvalid { get; } = Validation(EnvelopeTypeInvalidCode, EnvelopeTypeInvalidMessage);

    public static Error EnvelopeTypeTooLong { get; } = Validation(EnvelopeTypeTooLongCode, EnvelopeTypeTooLongMessage);

    public static Error EnvelopeSubjectMissing { get; } = Validation(EnvelopeSubjectMissingCode, EnvelopeSubjectMissingMessage);

    public static Error EnvelopeSubjectTooLong { get; } = Validation(EnvelopeSubjectTooLongCode, EnvelopeSubjectTooLongMessage);

    public static Error EnvelopeTimeMissing { get; } = Validation(EnvelopeTimeMissingCode, EnvelopeTimeMissingMessage);

    public static Error EnvelopeDataMissing { get; } = Validation(EnvelopeDataMissingCode, EnvelopeDataMissingMessage);

    public static Error StreamMissing { get; } = Validation(StreamMissingCode, StreamMissingMessage);

    public static Error StreamTooLong { get; } = Validation(StreamTooLongCode, StreamTooLongMessage);

    public static Error StreamsMissing { get; } = Validation(StreamsMissingCode, StreamsMissingMessage);

    public static Error OffsetInvalid { get; } = Validation(OffsetInvalidCode, OffsetInvalidMessage);

    public static Error MaxInvalid { get; } = Validation(MaxInvalidCode, MaxInvalidMessage);

    public static Error SubscriberMissing { get; } = Validation(SubscriberMissingCode, SubscriberMissingMessage);

    public static Error SubscriberTooLong { get; } = Validation(SubscriberTooLongCode, SubscriberTooLongMessage);

    public static Error FailureErrorMissing { get; } = Validation(FailureErrorMissingCode, FailureErrorMissingMessage);

    public static Error FailureErrorTooLong { get; } = Validation(FailureErrorTooLongCode, FailureErrorTooLongMessage);

    private static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
}
