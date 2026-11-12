using FluentValidation;

namespace RutaViva.Maintenance.Application.Plans;

public sealed class RestartRoutineCommandValidator : AbstractValidator<RestartRoutineCommand>
{
    private const long MinimumServicedKm = 0;

    public RestartRoutineCommandValidator()
    {
        RuleFor(command => command.ServicedKm).GreaterThanOrEqualTo(MinimumServicedKm);
        RuleFor(command => command.ServicedAt).NotEqual(default(DateTimeOffset));
        RuleFor(command => command.ServiceOrderId).NotEqual(Guid.Empty);
    }
}
