using FluentValidation;

namespace RutaViva.Maintenance.Application.Telemetry;

public sealed class IngestTelemetryReadingCommandValidator : AbstractValidator<IngestTelemetryReadingCommand>
{
    private const long MinimumKilometers = 0;
    private const string PlateRequiredMessage = "The plate is required.";
    private const string KilometersNegativeMessage = "The odometer kilometers cannot be negative.";
    private const string ReadAtRequiredMessage = "The reading date is required.";

    public IngestTelemetryReadingCommandValidator()
    {
        RuleFor(command => command.Plate).NotEmpty().WithMessage(PlateRequiredMessage);
        RuleFor(command => command.OdometerKm).GreaterThanOrEqualTo(MinimumKilometers).WithMessage(KilometersNegativeMessage);
        RuleFor(command => command.ReadAt).NotEqual(default(DateTimeOffset)).WithMessage(ReadAtRequiredMessage);
    }
}
