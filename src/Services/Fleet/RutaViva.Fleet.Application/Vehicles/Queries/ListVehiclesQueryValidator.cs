using FluentValidation;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed class ListVehiclesQueryValidator : AbstractValidator<ListVehiclesQuery>
{
    public ListVehiclesQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(VehicleListDefaults.FirstPage)
            .WithVehicleError(VehicleQueryErrors.InvalidPage);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(VehicleListDefaults.MinimumPageSize, VehicleListDefaults.MaximumPageSize)
            .WithVehicleError(VehicleQueryErrors.InvalidPageSize);
    }
}
