using FluentValidation;

namespace RutaViva.BuildingBlocks.UnitTests.Application;

public sealed class SampleQueryValidator : AbstractValidator<SampleQuery>
{
    public const string NameRequiredMessage = "Query name is required.";

    public SampleQueryValidator()
    {
        RuleFor(query => query.Name).NotEmpty().WithMessage(NameRequiredMessage);
    }
}
