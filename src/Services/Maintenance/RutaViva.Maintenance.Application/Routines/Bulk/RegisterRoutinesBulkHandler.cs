using FluentValidation;
using FluentValidation.Results;
using RutaViva.BuildingBlocks.Application.Abstractions;
using RutaViva.BuildingBlocks.Application.Bulk;
using RutaViva.BuildingBlocks.Domain.Results;
using RutaViva.IntegrationContracts.Bulk;
using RutaViva.Maintenance.Application.Abstractions;
using RutaViva.Maintenance.Domain.Common;
using RutaViva.Maintenance.Domain.Plans;
using RutaViva.Maintenance.Domain.Plans.ValueObjects;
using RutaViva.Maintenance.Domain.RoutineTypes;
using RutaViva.Maintenance.Domain.RoutineTypes.ValueObjects;

namespace RutaViva.Maintenance.Application.Routines.Bulk;

public sealed class RegisterRoutinesBulkHandler(
    IMaintenancePlanRepository plans,
    IRoutineTypeRepository routineTypes,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<RoutineBulkRow> rowValidator) : ICommandHandler<RegisterRoutinesBulkCommand, IReadOnlyList<BulkRowResult>>
{
    public async Task<Result<IReadOnlyList<BulkRowResult>>> HandleAsync(
        RegisterRoutinesBulkCommand command,
        CancellationToken cancellationToken)
    {
        IReadOnlySet<int> duplicates = BulkDuplicateDetector.FindDuplicateIndexes(command.Rows);
        IReadOnlyDictionary<LicensePlate, MaintenancePlan> plansByPlate =
            await plans.GetByPlatesAsync(CollectPlates(command.Rows), cancellationToken);
        RoutineParameterCatalog catalog = await routineTypes.GetCatalogAsync(cancellationToken);
        DateOnly today = MaintenanceTimeZone.ToLocalDate(clock.UtcNow);

        List<BulkRowResult> results = new(command.Rows.Count);
        for (int index = 0; index < command.Rows.Count; index++)
        {
            results.Add(Process(command.Rows[index], duplicates.Contains(index), plansByPlate, catalog, today));
        }

        Result saved = await BulkExecutionScope.For(command.DryRun).Resolve(unitOfWork).SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return Result<IReadOnlyList<BulkRowResult>>.Success(results);
    }

    private static IReadOnlyCollection<LicensePlate> CollectPlates(IReadOnlyList<RoutineBulkRow> rows) =>
        rows
            .Select(row => LicensePlate.Create(row.Plate))
            .Where(plate => plate.IsSuccess)
            .Select(plate => plate.Value)
            .Distinct()
            .ToList();

    private static BulkRowResult ToRowResult(int rowNumber, Result registration)
    {
        if (registration.IsSuccess)
        {
            return BulkRowResults.Valid(rowNumber);
        }

        if (registration.Error == MaintenanceErrors.RoutineAlreadyExists)
        {
            return BulkRowResults.Skipped(rowNumber);
        }

        return BulkRowResults.Invalid(rowNumber, registration.Error);
    }

    private static Result<RoutineRegistration> BuildRegistration(RoutineBulkRow row, RoutineParameterCatalog catalog)
    {
        Result<RoutineTypeCode> code = RoutineTypeCode.Create(row.RoutineTypeCode);
        if (code.IsFailure)
        {
            return code.Error;
        }

        Result<RoutineTypeId> routineTypeId = catalog.FindId(code.Value);
        if (routineTypeId.IsFailure)
        {
            return routineTypeId.Error;
        }

        Result<RoutineOverrides> overrides = RoutineOverrides.Create(row.IntervalKm, row.IntervalMonths, row.DueSoonThresholdPercent);
        if (overrides.IsFailure)
        {
            return overrides.Error;
        }

        return RoutineRegistration.Create(routineTypeId.Value, row.LastServiceKm, row.LastServiceDate, overrides.Value);
    }

    private static Result Register(
        RoutineBulkRow row,
        IReadOnlyDictionary<LicensePlate, MaintenancePlan> plansByPlate,
        RoutineParameterCatalog catalog,
        DateOnly today)
    {
        Result<LicensePlate> plate = LicensePlate.Create(row.Plate);
        if (plate.IsFailure || !plansByPlate.TryGetValue(plate.Value, out MaintenancePlan? plan))
        {
            return Result.Failure(MaintenanceErrors.MaintenancePlanNotFound);
        }

        Result<RoutineRegistration> registration = BuildRegistration(row, catalog);
        if (registration.IsFailure)
        {
            return Result.Failure(registration.Error);
        }

        return plan.RegisterRoutine(registration.Value, catalog, today);
    }

    private static List<BulkRowError> ToErrors(ValidationResult validation) =>
        validation.Errors.Select(failure => new BulkRowError(failure.ErrorCode, failure.ErrorMessage)).ToList();

    private BulkRowResult Process(
        RoutineBulkRow row,
        bool isDuplicate,
        IReadOnlyDictionary<LicensePlate, MaintenancePlan> plansByPlate,
        RoutineParameterCatalog catalog,
        DateOnly today)
    {
        ValidationResult format = rowValidator.Validate(row);
        if (!format.IsValid)
        {
            return BulkRowResults.Invalid(row.RowNumber, ToErrors(format));
        }

        if (isDuplicate)
        {
            return BulkRowResults.Invalid(row.RowNumber, MaintenanceErrors.BulkDuplicateInBatch);
        }

        return ToRowResult(row.RowNumber, Register(row, plansByPlate, catalog, today));
    }
}
