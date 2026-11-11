using FluentValidation;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.Workshops;

public sealed class ListWorkshopsQueryValidator : AbstractValidator<ListWorkshopsQuery>
{
    private const string StatusInvalidMessage = "The workshop status filter is not valid.";
    private const string CityTooLongMessage = "The city filter exceeds the maximum length.";

    public ListWorkshopsQueryValidator()
    {
        RuleFor(query => query.Status).IsInEnum().WithMessage(StatusInvalidMessage);
        RuleFor(query => query.City).MaximumLength(PostalAddress.MaxCityLength).WithMessage(CityTooLongMessage);
    }
}
