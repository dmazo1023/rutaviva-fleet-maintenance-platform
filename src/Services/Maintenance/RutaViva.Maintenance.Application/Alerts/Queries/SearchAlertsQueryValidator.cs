using FluentValidation;

namespace RutaViva.Maintenance.Application.Alerts.Queries;

public sealed class SearchAlertsQueryValidator : AbstractValidator<SearchAlertsQuery>
{
    private const int MinimumPage = 1;
    private const int MinimumPageSize = 1;
    private const int MaximumPageSize = 100;

    private const string PageOutOfRangeMessage = "The page must be greater than or equal to 1.";
    private const string PageSizeOutOfRangeMessage = "The page size must be between 1 and 100.";

    public SearchAlertsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(MinimumPage).WithMessage(PageOutOfRangeMessage);
        RuleFor(query => query.PageSize).InclusiveBetween(MinimumPageSize, MaximumPageSize).WithMessage(PageSizeOutOfRangeMessage);
    }
}
