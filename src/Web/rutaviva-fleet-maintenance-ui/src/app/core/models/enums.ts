export const CUSTOMER_STATUS_VALUES = ['Active', 'Inactive'] as const;
export type CustomerStatus = (typeof CUSTOMER_STATUS_VALUES)[number];

export const APPROVER_STATUS_VALUES = ['Active', 'Inactive'] as const;
export type ApproverStatus = (typeof APPROVER_STATUS_VALUES)[number];

export const WORKSHOP_STATUS_VALUES = ['Active', 'Inactive'] as const;
export type WorkshopStatus = (typeof WORKSHOP_STATUS_VALUES)[number];

export const VEHICLE_STATUS_VALUES = ['Active', 'Inactive'] as const;
export type VehicleStatus = (typeof VEHICLE_STATUS_VALUES)[number];

export const ROUTINE_TYPE_STATUS_VALUES = ['Active', 'Inactive'] as const;
export type RoutineTypeStatus = (typeof ROUTINE_TYPE_STATUS_VALUES)[number];

export const MAINTENANCE_PLAN_STATUS_VALUES = ['Active', 'Suspended'] as const;
export type MaintenancePlanStatus = (typeof MAINTENANCE_PLAN_STATUS_VALUES)[number];

export const ROUTINE_STATE_VALUES = ['Active', 'Inactive'] as const;
export type RoutineState = (typeof ROUTINE_STATE_VALUES)[number];

export const ROUTINE_DUE_STATUS_VALUES = ['UpToDate', 'DueSoon', 'Overdue'] as const;
export type RoutineDueStatus = (typeof ROUTINE_DUE_STATUS_VALUES)[number];
export type DueStatus = RoutineDueStatus;

export const ALERT_TYPE_VALUES = ['DueSoon', 'Overdue'] as const;
export type AlertType = (typeof ALERT_TYPE_VALUES)[number];

export const ALERT_TRIGGER_VALUES = ['Distance', 'Time', 'DistanceAndTime'] as const;
export type AlertTrigger = (typeof ALERT_TRIGGER_VALUES)[number];
export type TriggeredBy = AlertTrigger;

export const ALERT_STATUS_VALUES = ['Active', 'Acknowledged', 'Resolved'] as const;
export type AlertStatus = (typeof ALERT_STATUS_VALUES)[number];

export const ODOMETER_READING_OUTCOME_VALUES = ['Accepted', 'Ignored'] as const;
export type OdometerReadingOutcome = (typeof ODOMETER_READING_OUTCOME_VALUES)[number];

export const ODOMETER_READING_IGNORED_REASON_VALUES = [
  'NotGreaterThanCurrent',
  'UnknownVehicle',
] as const;
export type OdometerReadingIgnoredReason = (typeof ODOMETER_READING_IGNORED_REASON_VALUES)[number];

export const ALERT_RESOLUTION_REASON_VALUES = [
  'RoutineRestarted',
  'EscalatedToOverdue',
  'ParametersChanged',
  'RoutineDeactivated',
] as const;
export type AlertResolutionReason = (typeof ALERT_RESOLUTION_REASON_VALUES)[number];

export const SERVICE_ORDER_STATUS_VALUES = [
  'Created',
  'Quoted',
  'PendingApproval',
  'Approved',
  'OnSite',
  'InProgress',
  'OnHold',
  'Completed',
  'Cancelled',
] as const;
export type ServiceOrderStatus = (typeof SERVICE_ORDER_STATUS_VALUES)[number];

export const SERVICE_ORDER_ORIGIN_VALUES = ['Automatic', 'Manual'] as const;
export type ServiceOrderOrigin = (typeof SERVICE_ORDER_ORIGIN_VALUES)[number];

export const CANCELLATION_REASON_VALUES = ['QuoteRejected', 'CancelledByCustomer'] as const;
export type CancellationReason = (typeof CANCELLATION_REASON_VALUES)[number];

export const APPROVAL_REASON_VALUES = ['Quote', 'AdditionalServices'] as const;
export type ApprovalReason = (typeof APPROVAL_REASON_VALUES)[number];

export const APPROVAL_PRIORITY_VALUES = ['Normal', 'High'] as const;
export type ApprovalPriority = (typeof APPROVAL_PRIORITY_VALUES)[number];

export const SERVICE_LINE_TYPE_VALUES = ['Routine', 'Additional'] as const;
export type ServiceLineType = (typeof SERVICE_LINE_TYPE_VALUES)[number];

export const PRICING_STATUS_VALUES = ['Priced', 'PendingPricing'] as const;
export type PricingStatus = (typeof PRICING_STATUS_VALUES)[number];

export const SERVICE_LINE_APPROVAL_STATUS_VALUES = [
  'Approved',
  'PendingApproval',
  'Rejected',
] as const;
export type ServiceLineApprovalStatus = (typeof SERVICE_LINE_APPROVAL_STATUS_VALUES)[number];

export const SERVICE_LINE_REQUESTER_VALUES = ['Workshop', 'Approver'] as const;
export type ServiceLineRequester = (typeof SERVICE_LINE_REQUESTER_VALUES)[number];

export const SERVICE_ORDER_CHANGE_TYPE_VALUES = [
  'StatusChanged',
  'AppointmentScheduled',
  'AppointmentRescheduled',
  'AdditionalServiceAdded',
] as const;
export type ServiceOrderChangeType = (typeof SERVICE_ORDER_CHANGE_TYPE_VALUES)[number];

export const IMPORT_JOB_STATUS_VALUES = [
  'Validating',
  'PendingConfirmation',
  'Importing',
  'Completed',
  'CompletedWithErrors',
  'Failed',
] as const;
export type ImportJobStatus = (typeof IMPORT_JOB_STATUS_VALUES)[number];

export const IMPORT_ENTITY_TYPE_VALUES = [
  'Workshops',
  'Customers',
  'Approvers',
  'Vehicles',
  'Routines',
] as const;
export type ImportEntityType = (typeof IMPORT_ENTITY_TYPE_VALUES)[number];

export const IMPORT_STAGE_STATUS_VALUES = ['Pending', 'Validated', 'Imported', 'Failed'] as const;
export type ImportStageStatus = (typeof IMPORT_STAGE_STATUS_VALUES)[number];

export const FILE_ENCODING_VALUES = ['Utf8', 'Utf8Bom', 'Windows1252'] as const;
export type FileEncoding = (typeof FILE_ENCODING_VALUES)[number];

export const IMPORT_ROW_OUTCOME_VALUES = [
  'Valid',
  'Invalid',
  'Skipped',
  'Imported',
  'Failed',
] as const;
export type ImportRowOutcome = (typeof IMPORT_ROW_OUTCOME_VALUES)[number];

export const REPORT_CODE_VALUES = [
  'FleetMaintenanceStatus',
  'ServiceOrders',
  'MaintenanceCosts',
  'AlertsAttention',
] as const;
export type ReportCode = (typeof REPORT_CODE_VALUES)[number];

export const REPORT_FORMAT_VALUES = ['Xlsx', 'Csv'] as const;
export type ReportFormat = (typeof REPORT_FORMAT_VALUES)[number];

export const SERVICE_ORDER_ACTION_VALUES = [
  'Approve',
  'Reject',
  'RequestAdditionalService',
  'Cancel',
  'Quote',
  'PriceAdditionalService',
  'ScheduleAppointment',
  'MarkOnSite',
  'AddAdditionalService',
  'Start',
  'Complete',
] as const;
export type ServiceOrderAction = (typeof SERVICE_ORDER_ACTION_VALUES)[number];

export const SERVICE_ORDER_BLOCKED_REASON_VALUES = [
  'WaitingForPrice',
  'AppointmentRequired',
] as const;
export type ServiceOrderBlockedReason = (typeof SERVICE_ORDER_BLOCKED_REASON_VALUES)[number];

export const PROFILE_VALUES = ['Operator', 'Approver', 'Workshop'] as const;
export type Profile = (typeof PROFILE_VALUES)[number];

