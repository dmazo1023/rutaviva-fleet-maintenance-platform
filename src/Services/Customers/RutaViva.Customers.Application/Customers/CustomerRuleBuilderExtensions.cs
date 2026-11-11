using FluentValidation;
using RutaViva.Customers.Domain.ValueObjects;

namespace RutaViva.Customers.Application.Customers;

internal static class CustomerRuleBuilderExtensions
{
    private const string NameRequiredMessage = "The name is required.";
    private const string NameTooLongMessage = "The name exceeds the maximum length.";
    private const string EmailRequiredMessage = "The email is required.";
    private const string EmailTooLongMessage = "The email exceeds the maximum length.";
    private const string EmailInvalidMessage = "The email does not have a valid format.";

    public static IRuleBuilderOptions<T, string> MustBeValidPersonName<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage(NameRequiredMessage)
            .MaximumLength(PersonName.MaxLength).WithMessage(NameTooLongMessage);

    public static IRuleBuilderOptions<T, string> MustBeValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage(EmailRequiredMessage)
            .MaximumLength(EmailAddress.MaxLength).WithMessage(EmailTooLongMessage)
            .Must(value => EmailAddress.Create(value).IsSuccess).WithMessage(EmailInvalidMessage);
}
