using FluentValidation;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.Domain.Vehicles.ValueObjects;

namespace RutaViva.Fleet.Application.Vehicles;

public sealed class RegisterVehicleCommandValidator : AbstractValidator<RegisterVehicleCommand>
{
    private readonly IClock _clock;

    public RegisterVehicleCommandValidator(IClock clock)
    {
        _clock = clock;
        RuleFor(command => command.Plate).Must(IsValidPlate).WithVehicleError(VehicleErrors.InvalidPlate);
        RuleFor(command => command.Vin).Must(IsValidVin).WithVehicleError(VehicleErrors.InvalidVin).When(HasVin);
        RuleFor(command => command.Brand).NotEmpty().WithVehicleError(VehicleErrors.BrandRequired);
        RuleFor(command => command.Model).NotEmpty().WithVehicleError(VehicleErrors.ModelRequired);
        RuleFor(command => command.ModelYear).Must(IsModelYearInRange).WithVehicleError(VehicleErrors.InvalidModelYear);
        RuleFor(command => command.InitialOdometerKm).Must(IsValidOdometer).WithVehicleError(VehicleErrors.InvalidOdometer);
        RuleFor(command => command.CustomerId).Must(id => id.Value != Guid.Empty).WithVehicleError(VehicleErrors.IdentifierEmpty);
        RuleFor(command => command.WorkshopId).Must(id => id.Value != Guid.Empty).WithVehicleError(VehicleErrors.IdentifierEmpty);
        RuleFor(command => command.ResponsibleApproverId)
            .Must(id => id!.Value.Value != Guid.Empty)
            .WithVehicleError(VehicleErrors.IdentifierEmpty)
            .When(command => command.ResponsibleApproverId is not null);
    }

    private static bool HasVin(RegisterVehicleCommand command) => !string.IsNullOrWhiteSpace(command.Vin);

    private static bool IsValidPlate(string? plate) => Plate.Create(plate).IsSuccess;

    private static bool IsValidVin(string? vin) => Vin.Create(vin).IsSuccess;

    private static bool IsValidOdometer(long kilometers) => Kilometers.Create(kilometers).IsSuccess;

    private bool IsModelYearInRange(int modelYear) =>
        modelYear >= VehicleDescription.MinimumModelYear
        && modelYear <= _clock.UtcNow.Year + VehicleDescription.ModelYearsAheadAllowed;
}
