using FluentValidation;
using RutaViva.Workshops.Domain;
using RutaViva.Workshops.Domain.ValueObjects;

namespace RutaViva.Workshops.Application.Workshops;

public sealed class RegisterWorkshopCommandValidator : AbstractValidator<RegisterWorkshopCommand>
{
    private const string NameRequiredMessage = "The workshop name is required.";
    private const string NameTooLongMessage = "The workshop name exceeds the maximum length.";
    private const string TaxIdInvalidMessage = "The tax id must contain only digits and have between 9 and 10 digits.";
    private const string EmailInvalidMessage = "The email address is not valid.";
    private const string PhoneInvalidMessage = "The phone number must contain between 7 and 15 digits with an optional leading plus sign.";
    private const string AddressRequiredMessage = "The address is required.";
    private const string AddressTooLongMessage = "The address exceeds the maximum length.";
    private const string CityRequiredMessage = "The city is required.";
    private const string CityTooLongMessage = "The city exceeds the maximum length.";

    public RegisterWorkshopCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage(NameRequiredMessage)
            .MaximumLength(Workshop.MaxNameLength).WithMessage(NameTooLongMessage);
        RuleFor(command => command.TaxId)
            .Must(value => TaxId.Create(value).IsSuccess).WithMessage(TaxIdInvalidMessage);
        RuleFor(command => command.Email)
            .Must(value => EmailAddress.Create(value).IsSuccess).WithMessage(EmailInvalidMessage);
        RuleFor(command => command.Phone)
            .Must(value => PhoneNumber.Create(value).IsSuccess).WithMessage(PhoneInvalidMessage)
            .When(command => !string.IsNullOrWhiteSpace(command.Phone));
        RuleFor(command => command.Address)
            .NotEmpty().WithMessage(AddressRequiredMessage)
            .MaximumLength(PostalAddress.MaxAddressLength).WithMessage(AddressTooLongMessage);
        RuleFor(command => command.City)
            .NotEmpty().WithMessage(CityRequiredMessage)
            .MaximumLength(PostalAddress.MaxCityLength).WithMessage(CityTooLongMessage);
    }
}
