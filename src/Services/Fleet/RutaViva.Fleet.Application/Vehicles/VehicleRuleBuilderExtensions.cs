using FluentValidation;
using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Fleet.Application.Vehicles;

internal static class VehicleRuleBuilderExtensions
{
    public static IRuleBuilderOptions<T, TProperty> WithVehicleError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        Error error) =>
        rule.WithErrorCode(error.Code).WithMessage(error.Message);
}
