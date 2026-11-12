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
  readonly defaultIntervalKm: number | null;
  readonly defaultIntervalMonths: number | null;
  readonly defaultDueSoonPercent: number | null;
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
  readonly status: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly worstDueStatus: DueStatus | null;
  readonly maxConsumedPercent: number;
  readonly routines: readonly RoutineVm[];
}

export type NextMaintenanceKind = 'km' | 'time';

export interface NextMaintenanceVm {
  readonly routineName: string;
  readonly kind: NextMaintenanceKind;
  readonly remaining: number;
  readonly dueStatus: DueStatus;
  readonly triggeredBy: TriggeredBy | null;
}

export interface MaintenancePlanSummaryVm {
  readonly vehicleId: string;
  readonly plate: string;
  readonly status: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly worstDueStatus: DueStatus | null;
  readonly maxConsumedPercent: number;
  readonly upToDateCount: number;
  readonly dueSoonCount: number;
  readonly overdueCount: number;
  readonly inactiveCount: number;
  readonly nextMaintenance: NextMaintenanceVm | null;
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
  readonly type: AlertType;
  readonly triggeredBy: TriggeredBy;
  readonly status: AlertStatus;
  readonly raisedAt: string;
  readonly acknowledgedBy: string | null;
  readonly resolutionReason: AlertResolutionReason | null;
}

export interface CreateServiceOrderRequest {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly workshopId: string;
  readonly workshopName: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode: string;
  readonly createdBy: string;
}

export interface PlanOrderSummaryVm {
  readonly id: string;
  readonly number: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode: string;
  readonly status: ServiceOrderStatus;
  readonly statusBeforeHold: ServiceOrderStatus | null;
  readonly total: number | null;
  readonly appointmentAt: string | null;
}

export interface AlertsPageVm {
  readonly items: readonly AlertVm[];
  readonly totalCount: number;
}

export interface RegisterServiceRequest {
  readonly servicedKm: number;
  readonly servicedAt: string;
}

export interface ReevaluationSummaryVm {
  readonly evaluatedPlans: number;
  readonly dueSoonRoutines: number;
  readonly overdueRoutines: number;
  readonly requestedOrders: number;
}

