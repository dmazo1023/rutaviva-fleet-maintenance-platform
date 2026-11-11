using FluentValidation;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Application.Customers;

public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    private const string TaxIdRequiredMessage = "The tax id is required.";
    private const string TaxIdInvalidMessage = "The tax id does not have a valid format.";
    private const string PhoneInvalidMessage = "The phone does not have a valid format.";

    public RegisterCustomerCommandValidator()
    {
        RuleFor(command => command.Name).Cascade(CascadeMode.Stop).MustBeValidPersonName();
        RuleFor(command => command.Email).Cascade(CascadeMode.Stop).MustBeValidEmail();
        RuleFor(command => command.TaxId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(TaxIdRequiredMessage)
            .Must(value => TaxId.Create(value).IsSuccess).WithMessage(TaxIdInvalidMessage);
        RuleFor(command => command.Phone)
            .Must(value => PhoneNumber.Create(value).IsSuccess).WithMessage(PhoneInvalidMessage)
            .When(command => !string.IsNullOrWhiteSpace(command.Phone));
    }
}
