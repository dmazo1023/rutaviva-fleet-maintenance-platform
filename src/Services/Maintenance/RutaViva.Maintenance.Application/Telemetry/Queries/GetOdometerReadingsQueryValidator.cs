using FluentValidation;

namespace RutaViva.Maintenance.Application.Telemetry.Queries;

public sealed class GetOdometerReadingsQueryValidator : AbstractValidator<GetOdometerReadingsQuery>
{
    private const int MinimumLimit = 1;
    private const int MaximumLimit = 200;

    private const string LimitOutOfRangeMessage = "The limit must be between 1 and 200.";

    public GetOdometerReadingsQueryValidator()
    {
        RuleFor(query => query.Limit).InclusiveBetween(MinimumLimit, MaximumLimit).WithMessage(LimitOutOfRangeMessage);
    }
}
