import {
  AlertResolutionReason,
  AlertStatus,
  AlertTrigger,
  AlertType,
  ApproverStatus,
  CustomerStatus,
  DueStatus,
  MaintenancePlanStatus,
  OdometerReadingIgnoredReason,
  OdometerReadingOutcome,
  RoutineState,
  RoutineTypeStatus,
  VehicleStatus,
  WorkshopStatus,
} from '../models/enums';

export type ParameterSourceDto = 'RoutineType' | 'Override';

export interface PagedResultDto<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

export interface ApproverDto {
  readonly id: string;
  readonly fullName: string;
  readonly email: string;
  readonly status: ApproverStatus;
}

export interface CustomerSummaryDto {
  readonly id: string;
  readonly name: string;
  readonly taxId: string;
  readonly email: string;
  readonly status: CustomerStatus;
}

export interface CustomerDetailDto extends CustomerSummaryDto {
  readonly phone: string | null;
  readonly approvers: readonly ApproverDto[];
}

export interface WorkshopSummaryDto {
  readonly id: string;
  readonly name: string;
  readonly taxId: string;
  readonly city: string;
  readonly status: WorkshopStatus;
}

export interface WorkshopDetailDto extends WorkshopSummaryDto {
  readonly email: string;
  readonly phone: string | null;
  readonly address: string;
  readonly createdAt: string;
}

export interface VehicleSummaryDto {
  readonly id: string;
  readonly plate: string;
  readonly vin: string | null;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly status: VehicleStatus;
  readonly statusChangedAt: string;
  readonly customerId: string;
  readonly ownerName: string;
  readonly customerName: string;
  readonly workshopId: string;
  readonly workshopName: string;
  readonly responsibleApproverId: string | null;
  readonly responsibleName: string | null;
  readonly responsibleApproverName: string | null;
}

export interface VehicleOwnerDto {
  readonly id: string;
  readonly name: string;
  readonly taxId: string;
}

export interface VehicleWorkshopDto {
  readonly id: string;
  readonly name: string;
}

export interface VehicleResponsibleDto {
  readonly id: string;
  readonly name: string;
  readonly email: string;
}

export interface VehicleDetailDto {
  readonly id: string;
  readonly plate: string;
  readonly vin: string | null;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly initialOdometerKm: number;
  readonly status: VehicleStatus;
  readonly registeredAt: string;
  readonly statusChangedAt: string;
  readonly owner: VehicleOwnerDto;
  readonly workshop: VehicleWorkshopDto;
  readonly responsible: VehicleResponsibleDto | null;
}

export interface RoutineTypeDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly defaultIntervalKm: number | null;
  readonly defaultIntervalMonths: number | null;
  readonly dueSoonPercent: number;
  readonly status: RoutineTypeStatus;
  readonly updatedBy: string;
  readonly updatedAt: string;
}

export interface RoutineTrackDto {
  readonly kind: 'km' | 'time';
  readonly consumedPercent: number;
  readonly dueSoonPercent: number;
  readonly remaining: number;
  readonly status: DueStatus;
}

export interface RoutineDetailDto {
  readonly routineId: string;
  readonly routineTypeCode: string;
  readonly name: string;
  readonly state: RoutineState;
  readonly dueStatus: DueStatus;
  readonly triggeredBy: AlertTrigger | null;
  readonly cycleNumber: number;
  readonly cycleStartKm: number;
  readonly cycleStartDate: string;
  readonly intervalKm: number | null;
  readonly intervalKmSource: ParameterSourceDto;
  readonly intervalMonths: number | null;
  readonly intervalMonthsSource: ParameterSourceDto;
  readonly dueSoonPercent: number;
  readonly dueSoonPercentSource: ParameterSourceDto;
  readonly nextDueKm: number | null;
  readonly nextDueDate: string | null;
  readonly consumedPercent: number;
  readonly distanceTrack: RoutineTrackDto | null;
  readonly timeTrack: RoutineTrackDto | null;
}

export interface PlanSummaryDto {
  readonly vehicleId: string;
  readonly plate: string;
  readonly planStatus: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly worstDueStatus: DueStatus | null;
  readonly maxConsumedPercent: number;
  readonly upToDateCount: number;
  readonly dueSoonCount: number;
  readonly overdueCount: number;
  readonly inactiveCount: number;
}

export interface PlanDetailDto extends PlanSummaryDto {
  readonly routines: readonly RoutineDetailDto[];
}

export interface OdometerReadingDto {
  readonly kilometers: number;
  readonly readAt: string;
  readonly receivedAt: string;
  readonly outcome: OdometerReadingOutcome;
  readonly ignoredReason: OdometerReadingIgnoredReason | null;
}

export interface TelemetryResponseDto {
  readonly outcome: OdometerReadingOutcome;
  readonly reason: OdometerReadingIgnoredReason | null;
}

export interface ReevaluationSummaryDto {
  readonly evaluatedPlans: number;
  readonly dueSoon: number;
  readonly overdue: number;
  readonly ordersRequested: number;
}

export interface AlertDto {
  readonly alertId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly cycleNumber: number;
  readonly type: AlertType;
  readonly triggeredBy: AlertTrigger;
  readonly status: AlertStatus;
  readonly raisedAt: string;
  readonly acknowledgedAt: string | null;
  readonly acknowledgedBy: string | null;
  readonly resolvedAt: string | null;
  readonly resolutionReason: AlertResolutionReason | null;
}
