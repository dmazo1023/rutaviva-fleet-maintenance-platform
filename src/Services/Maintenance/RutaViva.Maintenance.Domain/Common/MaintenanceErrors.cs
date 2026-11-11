using RutaViva.BuildingBlocks.Domain.Results;

namespace RutaViva.Maintenance.Domain.Common;

public static class MaintenanceErrors
{
    private const string MaintenancePlanNotFoundCode = "MaintenancePlan.NotFound";
    private const string RoutineTypeNotFoundCode = "RoutineType.NotFound";
    private const string RoutineNotFoundCode = "Routine.NotFound";
    private const string AlertNotFoundCode = "Alert.NotFound";
    private const string RoutineAlreadyExistsCode = "Routine.AlreadyExists";
    private const string RoutineLastServiceKmAboveOdometerCode = "Routine.LastServiceKmAboveOdometer";
    private const string RoutineIncompleteLastServiceCode = "Routine.IncompleteLastService";
    private const string BulkDuplicateInBatchCode = "Bulk.DuplicateInBatch";
    private const string AlertInvalidTransitionCode = "Alert.InvalidTransition";
    private const string AlertAcknowledgedByRequiredCode = "Alert.AcknowledgedByRequired";
    private const string UpdatedByRequiredCode = "Audit.UpdatedByRequired";
    private const string RoutineTypeNameRequiredCode = "RoutineType.NameRequired";
    private const string IdentifierEmptyCode = "Identifier.Empty";
    private const string LicensePlateInvalidCode = "LicensePlate.Invalid";
    private const string RoutineTypeCodeInvalidCode = "RoutineTypeCode.Invalid";
    private const string OdometerKilometersNegativeCode = "Odometer.KilometersNegative";
    private const string OdometerReadingKilometersNegativeCode = "OdometerReading.KilometersNegative";
    private const string MaintenanceIntervalEmptyCode = "MaintenanceInterval.Empty";
    private const string MaintenanceIntervalKilometersNotPositiveCode = "MaintenanceInterval.KilometersNotPositive";
    private const string MaintenanceIntervalMonthsNotPositiveCode = "MaintenanceInterval.MonthsNotPositive";
    private const string MaintenanceIntervalMonthsTooLargeCode = "MaintenanceInterval.MonthsTooLarge";
    private const string DueSoonThresholdOutOfRangeCode = "DueSoonThreshold.OutOfRange";
    private const string MaintenanceCycleNumberInvalidCode = "MaintenanceCycle.NumberInvalid";
    private const string MaintenanceCycleStartKmNegativeCode = "MaintenanceCycle.StartKmNegative";
    private const string ServiceRecordKilometersNegativeCode = "ServiceRecord.KilometersNegative";
    private const string EvaluationContextKilometersNegativeCode = "EvaluationContext.KilometersNegative";

    private const string MaintenancePlanNotFoundMessage = "The maintenance plan was not found.";
    private const string RoutineTypeNotFoundMessage = "The routine type was not found.";
    private const string RoutineNotFoundMessage = "The routine was not found in the maintenance plan.";
    private const string AlertNotFoundMessage = "The alert was not found.";
    private const string RoutineAlreadyExistsMessage = "The vehicle already has a routine of this type.";
    private const string RoutineLastServiceKmAboveOdometerMessage = "The last service kilometers exceed the vehicle odometer.";
    private const string RoutineIncompleteLastServiceMessage = "The last service requires both kilometers and date, or neither.";
    private const string BulkDuplicateInBatchMessage = "Another row of the batch has the same plate and routine type code.";
    private const string AlertInvalidTransitionMessage = "The alert is not in a state that allows this transition.";
    private const string AlertAcknowledgedByRequiredMessage = "The user who acknowledges the alert is required.";
    private const string UpdatedByRequiredMessage = "The user who performs the change is required.";
    private const string RoutineTypeNameRequiredMessage = "The routine type name is required.";
    private const string IdentifierEmptyMessage = "The identifier cannot be an empty value.";
    private const string LicensePlateInvalidMessage = "The license plate must have three letters, two digits and one digit or letter.";
    private const string RoutineTypeCodeInvalidMessage = "The routine type code must be upper snake case and up to 30 characters.";
    private const string OdometerKilometersNegativeMessage = "The odometer kilometers cannot be negative.";
    private const string OdometerReadingKilometersNegativeMessage = "The odometer reading kilometers cannot be negative.";
    private const string MaintenanceIntervalEmptyMessage = "The interval requires kilometers, months or both.";
    private const string MaintenanceIntervalKilometersNotPositiveMessage = "The interval kilometers must be greater than zero.";
    private const string MaintenanceIntervalMonthsNotPositiveMessage = "The interval months must be greater than zero.";
    private const string MaintenanceIntervalMonthsTooLargeMessage = "The interval months exceed the maximum supported.";
    private const string DueSoonThresholdOutOfRangeMessage = "The due soon percent must be greater than 0 and lower than 100.";
    private const string MaintenanceCycleNumberInvalidMessage = "The cycle number must be at least 1.";
    private const string MaintenanceCycleStartKmNegativeMessage = "The cycle start kilometers cannot be negative.";
    private const string ServiceRecordKilometersNegativeMessage = "The service kilometers cannot be negative.";
    private const string EvaluationContextKilometersNegativeMessage = "The evaluation odometer kilometers cannot be negative.";

    public static readonly Error MaintenancePlanNotFound = new(MaintenancePlanNotFoundCode, MaintenancePlanNotFoundMessage, ErrorType.NotFound);

    public static readonly Error RoutineTypeNotFound = new(RoutineTypeNotFoundCode, RoutineTypeNotFoundMessage, ErrorType.NotFound);

    public static readonly Error RoutineNotFound = new(RoutineNotFoundCode, RoutineNotFoundMessage, ErrorType.NotFound);

    public static readonly Error AlertNotFound = new(AlertNotFoundCode, AlertNotFoundMessage, ErrorType.NotFound);

    public static readonly Error RoutineAlreadyExists = new(RoutineAlreadyExistsCode, RoutineAlreadyExistsMessage, ErrorType.Conflict);

    public static readonly Error RoutineLastServiceKmAboveOdometer = new(RoutineLastServiceKmAboveOdometerCode, RoutineLastServiceKmAboveOdometerMessage, ErrorType.BusinessRule);

    public static readonly Error RoutineIncompleteLastService = new(RoutineIncompleteLastServiceCode, RoutineIncompleteLastServiceMessage, ErrorType.Validation);

    public static readonly Error BulkDuplicateInBatch = new(BulkDuplicateInBatchCode, BulkDuplicateInBatchMessage, ErrorType.Validation);

    public static readonly Error AlertInvalidTransition = new(AlertInvalidTransitionCode, AlertInvalidTransitionMessage, ErrorType.BusinessRule);

    public static readonly Error AlertAcknowledgedByRequired = new(AlertAcknowledgedByRequiredCode, AlertAcknowledgedByRequiredMessage, ErrorType.Validation);

    public static readonly Error UpdatedByRequired = new(UpdatedByRequiredCode, UpdatedByRequiredMessage, ErrorType.Validation);

    public static readonly Error RoutineTypeNameRequired = new(RoutineTypeNameRequiredCode, RoutineTypeNameRequiredMessage, ErrorType.Validation);

    public static readonly Error IdentifierEmpty = new(IdentifierEmptyCode, IdentifierEmptyMessage, ErrorType.Validation);

    public static readonly Error LicensePlateInvalid = new(LicensePlateInvalidCode, LicensePlateInvalidMessage, ErrorType.Validation);

    public static readonly Error RoutineTypeCodeInvalid = new(RoutineTypeCodeInvalidCode, RoutineTypeCodeInvalidMessage, ErrorType.Validation);

    public static readonly Error OdometerKilometersNegative = new(OdometerKilometersNegativeCode, OdometerKilometersNegativeMessage, ErrorType.Validation);

    public static readonly Error OdometerReadingKilometersNegative = new(OdometerReadingKilometersNegativeCode, OdometerReadingKilometersNegativeMessage, ErrorType.Validation);

    public static readonly Error MaintenanceIntervalEmpty = new(MaintenanceIntervalEmptyCode, MaintenanceIntervalEmptyMessage, ErrorType.Validation);

    public static readonly Error MaintenanceIntervalKilometersNotPositive = new(MaintenanceIntervalKilometersNotPositiveCode, MaintenanceIntervalKilometersNotPositiveMessage, ErrorType.Validation);

    public static readonly Error MaintenanceIntervalMonthsNotPositive = new(MaintenanceIntervalMonthsNotPositiveCode, MaintenanceIntervalMonthsNotPositiveMessage, ErrorType.Validation);

    public static readonly Error MaintenanceIntervalMonthsTooLarge = new(MaintenanceIntervalMonthsTooLargeCode, MaintenanceIntervalMonthsTooLargeMessage, ErrorType.Validation);

    public static readonly Error DueSoonThresholdOutOfRange = new(DueSoonThresholdOutOfRangeCode, DueSoonThresholdOutOfRangeMessage, ErrorType.Validation);

    public static readonly Error MaintenanceCycleNumberInvalid = new(MaintenanceCycleNumberInvalidCode, MaintenanceCycleNumberInvalidMessage, ErrorType.Validation);

    public static readonly Error MaintenanceCycleStartKmNegative = new(MaintenanceCycleStartKmNegativeCode, MaintenanceCycleStartKmNegativeMessage, ErrorType.Validation);

    public static readonly Error ServiceRecordKilometersNegative = new(ServiceRecordKilometersNegativeCode, ServiceRecordKilometersNegativeMessage, ErrorType.Validation);

    public static readonly Error EvaluationContextKilometersNegative = new(EvaluationContextKilometersNegativeCode, EvaluationContextKilometersNegativeMessage, ErrorType.Validation);
}
