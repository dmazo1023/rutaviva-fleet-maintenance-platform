using FluentValidation;
using RutaViva.EventBus.Application.Validation;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.Publishing;

public sealed class PublishEventCommandValidator : AbstractValidator<PublishEventCommand>
{
    public PublishEventCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(command => command.Stream)
            .NotEmpty().WithError(EventBusErrors.StreamMissing)
            .MaximumLength(IntegrationEventLimits.StreamMaxLength).WithError(EventBusErrors.StreamTooLong);
        RuleFor(command => command.Envelope)
            .NotNull().WithError(EventBusErrors.EnvelopeMissing)
            .SetValidator(new IntegrationEventEnvelopeValidator());
    }
}
