export const HTTP_BAD_REQUEST = 400;
export const HTTP_NOT_FOUND = 404;
export const HTTP_UNPROCESSABLE = 422;

export type MockErrorType = 'Validation' | 'NotFound' | 'BusinessRule';

export class MockApiError extends Error {
  constructor(
    readonly status: number,
    readonly errorType: MockErrorType,
    readonly code: string,
    readonly detail: string
  ) {
    super(detail);
  }
}

export function validationError(code: string, detail: string): MockApiError {
  return new MockApiError(HTTP_BAD_REQUEST, 'Validation', code, detail);
}

export function notFoundError(code: string, detail: string): MockApiError {
  return new MockApiError(HTTP_NOT_FOUND, 'NotFound', code, detail);
}

export function businessRuleError(code: string, detail: string): MockApiError {
  return new MockApiError(HTTP_UNPROCESSABLE, 'BusinessRule', code, detail);
}

export const NOT_IMPLEMENTED_ROUTE: MockApiError = new MockApiError(HTTP_NOT_FOUND, 'NotFound', '', '');

export const ERRORS = {
  planNotFound: notFoundError('MaintenancePlan.NotFound', 'The maintenance plan was not found.'),
  routineNotFound: notFoundError('Routine.NotFound', 'The routine was not found in the maintenance plan.'),
  routineTypeNotFound: notFoundError('RoutineType.NotFound', 'The routine type was not found.'),
  alertNotFound: notFoundError('Alert.NotFound', 'The alert was not found.'),
  vehicleNotFound: notFoundError('Vehicle.NotFound', 'The vehicle was not found.'),
  customerNotFound: notFoundError('Customer.NotFound', 'The customer was not found.'),
  workshopNotFound: notFoundError('Workshop.NotFound', 'The workshop was not found.'),
  updatedByRequired: validationError('Audit.UpdatedByRequired', 'The user who performs the change is required.'),
  acknowledgedByRequired: validationError('Alert.AcknowledgedByRequired', 'The user who acknowledges the alert is required.'),
  alertInvalidTransition: businessRuleError('Alert.InvalidTransition', 'The alert is not in a state that allows this transition.'),
  stateRequired: validationError('Routine.StateRequired', 'The routine state is required.'),
  serviceDataRequired: validationError('Routine.ServiceDataRequired', 'The serviced kilometers and date are required.'),
  vehicleStatusInvalid: validationError('Vehicle.InvalidStatus', 'The target vehicle status is not supported.'),
  intervalEmpty: validationError('MaintenanceInterval.Empty', 'The interval requires kilometers, months or both.'),
  intervalKmNotPositive: validationError('MaintenanceInterval.KilometersNotPositive', 'The interval kilometers must be greater than zero.'),
  intervalMonthsNotPositive: validationError('MaintenanceInterval.MonthsNotPositive', 'The interval months must be greater than zero.'),
  dueSoonOutOfRange: validationError('DueSoonThreshold.OutOfRange', 'The due soon percent must be greater than 0 and lower than 100.'),
  invalidAlertPage: validationError('Validation.Failed', 'The page must be greater than or equal to 1.'),
  invalidAlertPageSize: validationError('Validation.Failed', 'The page size must be between 1 and 100.'),
  invalidVehiclePage: validationError('Vehicle.InvalidPage', 'The page must be greater than or equal to 1.'),
  invalidVehiclePageSize: validationError('Vehicle.InvalidPageSize', 'The page size must be between 1 and 200.'),
  plateRequired: validationError('Validation.Failed', 'The plate is required.'),
} as const;
