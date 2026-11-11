using FluentValidation;
using RutaViva.EventBus.Application.Validation;
using RutaViva.IntegrationContracts;

namespace RutaViva.EventBus.Application.Queries;

public sealed class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
{
    private const long MinimumOffset = 0;
    private const int MinimumMax = 1;

    public GetEventsQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(query => query.Streams).NotEmpty().WithError(EventBusErrors.StreamsMissing);
        RuleForEach(query => query.Streams)
            .NotEmpty().WithError(EventBusErrors.StreamMissing)
            .MaximumLength(IntegrationEventLimits.StreamMaxLength).WithError(EventBusErrors.StreamTooLong);
        RuleFor(query => query.AfterOffset).GreaterThanOrEqualTo(MinimumOffset).WithError(EventBusErrors.OffsetInvalid);
        RuleFor(query => query.Max).GreaterThanOrEqualTo(MinimumMax).WithError(EventBusErrors.MaxInvalid);
    }
}
