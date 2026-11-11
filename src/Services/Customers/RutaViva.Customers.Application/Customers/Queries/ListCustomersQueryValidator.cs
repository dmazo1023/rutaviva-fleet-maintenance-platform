using FluentValidation;

namespace RutaViva.Customers.Application.Customers.Queries;

public sealed class ListCustomersQueryValidator : AbstractValidator<ListCustomersQuery>
{
    private const string StatusInvalidMessage = "The customer status filter is not valid.";

    public ListCustomersQueryValidator()
    {
        RuleFor(query => query.Status).IsInEnum().WithMessage(StatusInvalidMessage).When(query => query.Status.HasValue);
    }
}
