using FluentValidation;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.EventBus.Application.Validation;

public static class RuleBuilderErrorExtensions
{
    public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Error error) =>
        rule.WithErrorCode(error.Code).WithMessage(error.Message);
}
