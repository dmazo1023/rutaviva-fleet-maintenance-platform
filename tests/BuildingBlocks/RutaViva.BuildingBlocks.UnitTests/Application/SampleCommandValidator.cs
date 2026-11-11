using FluentValidation;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class SampleCommandValidator : AbstractValidator<SampleCommand>
{
    public const string NameRequiredMessage = "Name is required.";

    public SampleCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().WithMessage(NameRequiredMessage);
    }
}
