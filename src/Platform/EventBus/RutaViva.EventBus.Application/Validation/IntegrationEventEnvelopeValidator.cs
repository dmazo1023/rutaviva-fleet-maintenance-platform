using System.Text.Json;
using FluentValidation;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.Validation;

public sealed class IntegrationEventEnvelopeValidator : AbstractValidator<IntegrationEventEnvelope>
{
    public IntegrationEventEnvelopeValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(envelope => envelope.Id).NotEmpty().WithError(EventBusErrors.EnvelopeIdMissing);
        RuleFor(envelope => envelope.Source)
            .NotEmpty().WithError(EventBusErrors.EnvelopeSourceMissing)
            .MaximumLength(IntegrationEventLimits.SourceMaxLength).WithError(EventBusErrors.EnvelopeSourceTooLong);
        RuleFor(envelope => envelope.Type)
            .NotEmpty().WithError(EventBusErrors.EnvelopeTypeMissing)
            .MaximumLength(IntegrationEventLimits.TypeMaxLength).WithError(EventBusErrors.EnvelopeTypeTooLong)
            .Must(IsParsableType).WithError(EventBusErrors.EnvelopeTypeInvalid);
        RuleFor(envelope => envelope.Subject)
            .NotEmpty().WithError(EventBusErrors.EnvelopeSubjectMissing)
            .MaximumLength(IntegrationEventLimits.SubjectMaxLength).WithError(EventBusErrors.EnvelopeSubjectTooLong);
        RuleFor(envelope => envelope.Time).NotEmpty().WithError(EventBusErrors.EnvelopeTimeMissing);
        RuleFor(envelope => envelope.Data).Must(HasData).WithError(EventBusErrors.EnvelopeDataMissing);
    }

    private static bool IsParsableType(string type) => IntegrationEventType.TryParse(type, out _);

    private static bool HasData(JsonElement data) => data.ValueKind != JsonValueKind.Undefined;
}
