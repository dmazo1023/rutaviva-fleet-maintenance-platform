using FluentValidation;
using RutaViva.Fleet.Domain.Common;

namespace RutaViva.Fleet.Application.Vehicles.Queries;

public sealed class GetVehicleDetailQueryValidator : AbstractValidator<GetVehicleDetailQuery>
{
    public GetVehicleDetailQueryValidator()
    {
        RuleFor(query => query.VehicleId).Must(id => id.Value != Guid.Empty).WithVehicleError(VehicleErrors.IdentifierEmpty);
    }
}
