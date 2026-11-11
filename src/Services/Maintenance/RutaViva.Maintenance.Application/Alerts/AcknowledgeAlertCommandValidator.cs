using FluentValidation;

namespace RutaViva.Maintenance.Application.Alerts;

public sealed class AcknowledgeAlertCommandValidator : AbstractValidator<AcknowledgeAlertCommand>
{
    private const string AcknowledgedByRequiredMessage = "The user who acknowledges the alert is required.";

    public AcknowledgeAlertCommandValidator()
    {
        RuleFor(command => command.AcknowledgedBy).NotEmpty().WithMessage(AcknowledgedByRequiredMessage);
    }
}
