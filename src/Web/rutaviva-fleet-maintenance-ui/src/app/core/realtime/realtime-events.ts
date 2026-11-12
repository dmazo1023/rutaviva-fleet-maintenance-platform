import {
  AlertResolutionReason,
  AlertStatus,
  AlertTrigger,
  AlertType,
  ApprovalPriority,
  ApprovalReason,
  DueStatus,
  MaintenancePlanStatus,
  OdometerReadingIgnoredReason,
  OdometerReadingOutcome,
  RoutineState,
  ServiceOrderStatus,
  TriggeredBy,
  VehicleStatus,
} from '../models/enums';

export interface RoutineChangedEvent {
  readonly routineId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly routineTypeCode: string;
  readonly routineName: string;
  readonly state: RoutineState;
  readonly dueStatus: DueStatus;
  readonly triggeredBy: TriggeredBy | null;
  readonly consumedPercent: number;
  readonly effectiveDueSoonPercent: number;
  readonly nextDueKm: number | null;
  readonly nextDueDate: string | null;
  readonly odometerKm: number;
  readonly occurredAt: string;
}

export interface OdometerReadingRecordedEvent {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly kilometers: number;
  readonly readAt: string;
  readonly outcome: OdometerReadingOutcome;
  readonly ignoredReason: OdometerReadingIgnoredReason | null;
  readonly currentOdometerKm: number;
  readonly occurredAt: string;
}

export interface VehicleChangedEvent {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly status: VehicleStatus;
  readonly planStatus: MaintenancePlanStatus;
  readonly occurredAt: string;
}

export interface AlertChangedEvent {
  readonly alertId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly type: AlertType;
  readonly triggeredBy: AlertTrigger;
  readonly status: AlertStatus;
  readonly raisedAt: string;
  readonly acknowledgedBy: string | null;
  readonly resolutionReason: AlertResolutionReason | null;
  readonly occurredAt: string;
}

export interface ServiceOrderChangedEvent {
  readonly serviceOrderId: string;
  readonly number: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly customerId: string;
  readonly workshopId: string;
  readonly status: ServiceOrderStatus;
  readonly priority: ApprovalPriority | null;
  readonly reason: ApprovalReason | null;
  readonly totalAmount: number | null;
  readonly autoApproved: boolean;
  readonly appointmentAt: string | null;
  readonly updatedAt: string;
}

export interface ApprovalRequestedEvent {
  readonly serviceOrderId: string;
  readonly number: string;
  readonly plate: string;
  readonly routineName: string;
  readonly customerId: string;
  readonly reason: ApprovalReason;
  readonly priority: ApprovalPriority;
  readonly totalAmount: number | null;
  readonly occurredAt: string;
}

