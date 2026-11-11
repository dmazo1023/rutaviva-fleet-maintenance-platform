import {
  AlertResolutionReason,
  AlertStatus,
  AlertType,
  DueStatus,
  MaintenancePlanStatus,
  OdometerReadingIgnoredReason,
  OdometerReadingOutcome,
  RoutineState,
  ServiceOrderStatus,
  TriggeredBy,
} from '../../../core/models/enums';
import { RoutineTrackVm } from '../../../shared/models/routine-track.vm';

export type ThresholdSource = 'Type' | 'Routine';

export interface RoutineVm {
  readonly routineId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly name: string;
  readonly typeCode: string;
  readonly icon: string;
  readonly state: RoutineState;
  readonly dueStatus: DueStatus;
  readonly triggeredBy: TriggeredBy | null;
  readonly tracks: readonly RoutineTrackVm[];
  readonly nextDueKm: number | null;
  readonly nextDueDate: string | null;
  readonly cycleStartKm: number;
  readonly cycleStartDate: string;
  readonly intervalKm: number | null;
  readonly intervalMonths: number | null;
  readonly defaultIntervalKm: number;
  readonly defaultIntervalMonths: number;
  readonly defaultDueSoonPercent: number;
  readonly intervalKmOverride: number | null;
  readonly intervalMonthsOverride: number | null;
  readonly dueSoonPercentOverride: number | null;
  readonly effectiveDueSoonPercent: number;
  readonly thresholdSource: ThresholdSource;
  readonly openServiceOrderId: string | null;
  readonly openServiceOrderNumber: string | null;
  readonly openServiceOrderStatus: ServiceOrderStatus | null;
  readonly canCreateOrder: boolean;
}

export interface MaintenancePlanDetailVm {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly status: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly routines: readonly RoutineVm[];
}

export interface MaintenancePlanSummaryVm {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly status: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly worstDueStatus: DueStatus | null;
  readonly worstConsumedPercent: number;
  readonly worstTriggeredBy: TriggeredBy | null;
  readonly nextRoutineName: string | null;
  readonly nextRemainingKind: 'km' | 'time' | null;
  readonly nextRemainingValue: number | null;
}

export interface OdometerReadingVm {
  readonly kilometers: number;
  readonly readAt: string;
  readonly outcome: OdometerReadingOutcome;
  readonly ignoredReason: OdometerReadingIgnoredReason | null;
}

export interface AlertVm {
  readonly alertId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode: string;
  readonly type: AlertType;
  readonly triggeredBy: TriggeredBy;
  readonly status: AlertStatus;
  readonly raisedAt: string;
  readonly acknowledgedBy: string | null;
  readonly resolutionReason: AlertResolutionReason | null;
}

export interface ReevaluationSummaryVm {
  readonly evaluatedPlans: number;
  readonly dueSoonRoutines: number;
  readonly overdueRoutines: number;
  readonly requestedOrders: number;
}

