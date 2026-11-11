using FluentValidation;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Application.Routines.Bulk;

public sealed class BulkRoutineRowValidator : AbstractValidator<RoutineBulkRow>
{
    private const long MinimumKilometers = 0;

    public BulkRoutineRowValidator()
    {
        RuleFor(row => row.Plate)
            .Must(plate => LicensePlate.Create(plate).IsSuccess)
            .WithMaintenanceError(MaintenanceErrors.LicensePlateInvalid);
        RuleFor(row => row.RoutineTypeCode)
            .Must(code => RoutineTypeCode.Create(code).IsSuccess)
            .WithMaintenanceError(MaintenanceErrors.RoutineTypeCodeInvalid);
        RuleFor(row => row.LastServiceKm)
            .GreaterThanOrEqualTo(MinimumKilometers).When(row => row.LastServiceKm.HasValue)
            .WithMaintenanceError(MaintenanceErrors.ServiceRecordKilometersNegative);
    }
}
