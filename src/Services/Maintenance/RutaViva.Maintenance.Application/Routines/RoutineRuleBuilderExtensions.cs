using FluentValidation;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Maintenance.Application.Routines;

internal static class RoutineRuleBuilderExtensions
{
    public static IRuleBuilderOptions<T, TProperty> WithMaintenanceError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        Error error) =>
        rule.WithErrorCode(error.Code).WithMessage(error.Message);
}
