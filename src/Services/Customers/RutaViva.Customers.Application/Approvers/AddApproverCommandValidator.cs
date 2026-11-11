using FluentValidation;
using RutaViva.Customers.Application.Customers;

namespace RutaViva.Customers.Application.Approvers;

public sealed class AddApproverCommandValidator : AbstractValidator<AddApproverCommand>
{
    public AddApproverCommandValidator()
    {
        RuleFor(command => command.FullName).Cascade(CascadeMode.Stop).MustBeValidPersonName();
        RuleFor(command => command.Email).Cascade(CascadeMode.Stop).MustBeValidEmail();
    }
}
