using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.EventBus.Application;

public static class EventBusErrors
{
    private const string EnvelopeMissingCode = "EventBus.Envelope.Missing";
    private const string EnvelopeIdMissingCode = "EventBus.Envelope.IdMissing";
    private const string EnvelopeSourceMissingCode = "EventBus.Envelope.SourceMissing";
    private const string EnvelopeTypeMissingCode = "EventBus.Envelope.TypeMissing";
    private const string EnvelopeTypeInvalidCode = "EventBus.Envelope.TypeInvalid";
    private const string EnvelopeSubjectMissingCode = "EventBus.Envelope.SubjectMissing";
    private const string EnvelopeTimeMissingCode = "EventBus.Envelope.TimeMissing";
    private const string EnvelopeDataMissingCode = "EventBus.Envelope.DataMissing";
    private const string StreamMissingCode = "EventBus.Stream.Missing";
    private const string StreamsMissingCode = "EventBus.Streams.Missing";
    private const string OffsetInvalidCode = "EventBus.Offset.Invalid";
    private const string MaxInvalidCode = "EventBus.Max.Invalid";
    private const string SubscriberMissingCode = "EventBus.Subscriber.Missing";
    private const string FailureErrorMissingCode = "EventBus.FailureError.Missing";

    public static Error EnvelopeMissing { get; } = Validation(EnvelopeMissingCode, "The envelope is required.");

    public static Error EnvelopeIdMissing { get; } = Validation(EnvelopeIdMissingCode, "The envelope id is required.");

    public static Error EnvelopeSourceMissing { get; } = Validation(EnvelopeSourceMissingCode, "The envelope source is required.");

    public static Error EnvelopeTypeMissing { get; } = Validation(EnvelopeTypeMissingCode, "The envelope type is required.");

    public static Error EnvelopeTypeInvalid { get; } = Validation(EnvelopeTypeInvalidCode, "The envelope type must match rutaviva.{service}.{EventName}.v{n}.");

    public static Error EnvelopeSubjectMissing { get; } = Validation(EnvelopeSubjectMissingCode, "The envelope subject is required.");

    public static Error EnvelopeTimeMissing { get; } = Validation(EnvelopeTimeMissingCode, "The envelope time is required.");

    public static Error EnvelopeDataMissing { get; } = Validation(EnvelopeDataMissingCode, "The envelope data is required.");

    public static Error StreamMissing { get; } = Validation(StreamMissingCode, "The stream is required.");

    public static Error StreamsMissing { get; } = Validation(StreamsMissingCode, "At least one stream is required.");

    public static Error OffsetInvalid { get; } = Validation(OffsetInvalidCode, "The offset must not be negative.");

    public static Error MaxInvalid { get; } = Validation(MaxInvalidCode, "The max value must be greater than zero.");

    public static Error SubscriberMissing { get; } = Validation(SubscriberMissingCode, "The subscriber name is required.");

    public static Error FailureErrorMissing { get; } = Validation(FailureErrorMissingCode, "The failure error text is required.");

    private static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
}
