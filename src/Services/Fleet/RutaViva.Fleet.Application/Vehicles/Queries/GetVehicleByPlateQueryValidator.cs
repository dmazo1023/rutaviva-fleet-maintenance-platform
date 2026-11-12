using FluentValidation;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed class GetVehicleByPlateQueryValidator : AbstractValidator<GetVehicleByPlateQuery>
{
    public GetVehicleByPlateQueryValidator()
    {
        RuleFor(query => query.Plate).Must(IsValidPlate).WithVehicleError(VehicleErrors.InvalidPlate);
    }

    private static bool IsValidPlate(string? plate) => Plate.Create(plate).IsSuccess;
}
