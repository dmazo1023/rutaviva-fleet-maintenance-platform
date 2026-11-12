import { ENUM_LABELS } from './enum-labels';
import {
  ALERT_RESOLUTION_REASON_VALUES,
  ALERT_STATUS_VALUES,
  ALERT_TRIGGER_VALUES,
  ALERT_TYPE_VALUES,
  APPROVAL_PRIORITY_VALUES,
  APPROVAL_REASON_VALUES,
  APPROVER_STATUS_VALUES,
  CANCELLATION_REASON_VALUES,
  CUSTOMER_STATUS_VALUES,
  FILE_ENCODING_VALUES,
  IMPORT_ENTITY_TYPE_VALUES,
  IMPORT_JOB_STATUS_VALUES,
  IMPORT_ROW_OUTCOME_VALUES,
  IMPORT_STAGE_STATUS_VALUES,
  MAINTENANCE_PLAN_STATUS_VALUES,
  ODOMETER_READING_IGNORED_REASON_VALUES,
  ODOMETER_READING_OUTCOME_VALUES,
  PRICING_STATUS_VALUES,
  PROFILE_VALUES,
  REPORT_CODE_VALUES,
  REPORT_FORMAT_VALUES,
  ROUTINE_DUE_STATUS_VALUES,
  ROUTINE_STATE_VALUES,
  ROUTINE_TYPE_STATUS_VALUES,
  SERVICE_LINE_APPROVAL_STATUS_VALUES,
  SERVICE_LINE_REQUESTER_VALUES,
  SERVICE_LINE_TYPE_VALUES,
  SERVICE_ORDER_ACTION_VALUES,
  SERVICE_ORDER_BLOCKED_REASON_VALUES,
  SERVICE_ORDER_CHANGE_TYPE_VALUES,
  SERVICE_ORDER_ORIGIN_VALUES,
  SERVICE_ORDER_STATUS_VALUES,
  VEHICLE_STATUS_VALUES,
  WORKSHOP_STATUS_VALUES,
} from './enums';

describe('ENUM_LABELS', () => {
  it('should provide non-empty Spanish labels for every value of every enum', () => {
    const groups: readonly {
      readonly values: readonly string[];
      readonly map: Readonly<Record<string, string>>;
    }[] = [
      { values: CUSTOMER_STATUS_VALUES, map: ENUM_LABELS.customerStatus },
      { values: APPROVER_STATUS_VALUES, map: ENUM_LABELS.approverStatus },
      { values: WORKSHOP_STATUS_VALUES, map: ENUM_LABELS.workshopStatus },
      { values: VEHICLE_STATUS_VALUES, map: ENUM_LABELS.vehicleStatus },
      { values: ROUTINE_TYPE_STATUS_VALUES, map: ENUM_LABELS.routineTypeStatus },
      { values: MAINTENANCE_PLAN_STATUS_VALUES, map: ENUM_LABELS.maintenancePlanStatus },
      { values: ROUTINE_STATE_VALUES, map: ENUM_LABELS.routineState },
      { values: ROUTINE_DUE_STATUS_VALUES, map: ENUM_LABELS.routineDueStatus },
      { values: ALERT_TYPE_VALUES, map: ENUM_LABELS.alertType },
      { values: ALERT_TRIGGER_VALUES, map: ENUM_LABELS.alertTrigger },
      { values: ALERT_STATUS_VALUES, map: ENUM_LABELS.alertStatus },
      { values: ODOMETER_READING_OUTCOME_VALUES, map: ENUM_LABELS.odometerReadingOutcome },
      {
        values: ODOMETER_READING_IGNORED_REASON_VALUES,
        map: ENUM_LABELS.odometerReadingIgnoredReason,
      },
      { values: ALERT_RESOLUTION_REASON_VALUES, map: ENUM_LABELS.alertResolutionReason },
      { values: SERVICE_ORDER_STATUS_VALUES, map: ENUM_LABELS.serviceOrderStatus },
      { values: SERVICE_ORDER_ORIGIN_VALUES, map: ENUM_LABELS.serviceOrderOrigin },
      { values: CANCELLATION_REASON_VALUES, map: ENUM_LABELS.cancellationReason },
      { values: APPROVAL_REASON_VALUES, map: ENUM_LABELS.approvalReason },
      { values: APPROVAL_PRIORITY_VALUES, map: ENUM_LABELS.approvalPriority },
      { values: SERVICE_LINE_TYPE_VALUES, map: ENUM_LABELS.serviceLineType },
      { values: PRICING_STATUS_VALUES, map: ENUM_LABELS.pricingStatus },
      { values: SERVICE_LINE_APPROVAL_STATUS_VALUES, map: ENUM_LABELS.serviceLineApprovalStatus },
      { values: SERVICE_LINE_REQUESTER_VALUES, map: ENUM_LABELS.serviceLineRequester },
      { values: SERVICE_ORDER_CHANGE_TYPE_VALUES, map: ENUM_LABELS.serviceOrderChangeType },
      { values: IMPORT_JOB_STATUS_VALUES, map: ENUM_LABELS.importJobStatus },
      { values: IMPORT_ENTITY_TYPE_VALUES, map: ENUM_LABELS.importEntityType },
      { values: IMPORT_STAGE_STATUS_VALUES, map: ENUM_LABELS.importStageStatus },
      { values: FILE_ENCODING_VALUES, map: ENUM_LABELS.fileEncoding },
      { values: IMPORT_ROW_OUTCOME_VALUES, map: ENUM_LABELS.importRowOutcome },
      { values: REPORT_CODE_VALUES, map: ENUM_LABELS.reportCode },
      { values: REPORT_FORMAT_VALUES, map: ENUM_LABELS.reportFormat },
      { values: SERVICE_ORDER_ACTION_VALUES, map: ENUM_LABELS.serviceOrderAction },
      { values: SERVICE_ORDER_BLOCKED_REASON_VALUES, map: ENUM_LABELS.serviceOrderBlockedReason },
      { values: PROFILE_VALUES, map: ENUM_LABELS.profile },
    ];

    for (const group of groups) {
      expect(Object.keys(group.map).length).toBe(group.values.length);
      for (const value of group.values) {
        const label: string | undefined = group.map[value];
        expect(label).toBeDefined();
        expect(label?.trim().length).toBeGreaterThan(0);
      }
    }

    expect(ENUM_LABELS.serviceOrderStatus.OnSite).toBe('En sitio');
    expect(ENUM_LABELS.alertTrigger.DistanceAndTime).toBe('Por km y tiempo');
  });
});

