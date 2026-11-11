using FluentValidation;
using RutaViva.EventBus.Application.Validation;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.Failures;

public sealed class ReportFailureCommandValidator : AbstractValidator<ReportFailureCommand>
{
    private const long MinimumOffset = 0;

    public ReportFailureCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(command => command.Subscriber)
            .NotEmpty().WithError(EventBusErrors.SubscriberMissing)
            .MaximumLength(IntegrationEventLimits.SubscriberNameMaxLength).WithError(EventBusErrors.SubscriberTooLong);
        RuleFor(command => command.Offset).GreaterThanOrEqualTo(MinimumOffset).WithError(EventBusErrors.OffsetInvalid);
        RuleFor(command => command.Error)
            .NotEmpty().WithError(EventBusErrors.FailureErrorMissing)
            .MaximumLength(IntegrationEventLimits.FailureMaxLength).WithError(EventBusErrors.FailureErrorTooLong);
    }
}
