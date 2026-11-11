import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { AlertsQuery, ApiRoutes } from '../../../core/api/api-routes';
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
import {
  AlertVm,
  MaintenancePlanDetailVm,
  MaintenancePlanSummaryVm,
  OdometerReadingVm,
  ReevaluationSummaryVm,
  RoutineVm,
} from '../models/maintenance.vm';

export interface MaintenancePlanSummaryDto {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly status: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly worstDueStatus: DueStatus | null;
  readonly worstConsumedPercent: number;
  readonly worstTriggeredBy?: TriggeredBy | null;
  readonly nextRoutineName?: string | null;
  readonly nextRemainingKind?: 'km' | 'time' | null;
  readonly nextRemainingValue?: number | null;
}

export interface RoutineDto {
  readonly routineId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly name: string;
  readonly typeCode: string;
  readonly icon?: string;
  readonly state: RoutineState;
  readonly dueStatus: DueStatus;
  readonly triggeredBy: TriggeredBy | null;
  readonly tracks: readonly RoutineTrackVm[];
  readonly nextDueKm: number | null;
  readonly nextDueDate: string | null;
  readonly cycleStartKm?: number;
  readonly cycleStartDate?: string;
  readonly intervalKm?: number | null;
  readonly intervalMonths?: number | null;
  readonly defaultIntervalKm?: number;
  readonly defaultIntervalMonths?: number;
  readonly defaultDueSoonPercent?: number;
  readonly intervalKmOverride?: number | null;
  readonly intervalMonthsOverride?: number | null;
  readonly dueSoonPercentOverride?: number | null;
  readonly effectiveDueSoonPercent: number;
  readonly thresholdSource: 'Type' | 'Routine';
  readonly openServiceOrderId: string | null;
  readonly openServiceOrderNumber?: string | null;
  readonly openServiceOrderStatus?: ServiceOrderStatus | null;
  readonly canCreateOrder: boolean;
}

export interface MaintenancePlanDetailDto {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly status: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly routines: readonly RoutineDto[];
}

export interface OdometerReadingDto {
  readonly kilometers: number;
  readonly readAt: string;
  readonly outcome: OdometerReadingOutcome;
  readonly ignoredReason: OdometerReadingIgnoredReason | null;
}

export interface AlertDto {
  readonly alertId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode?: string;
  readonly type: AlertType;
  readonly triggeredBy: TriggeredBy;
  readonly status: AlertStatus;
  readonly raisedAt: string;
  readonly acknowledgedBy: string | null;
  readonly resolutionReason: AlertResolutionReason | null;
}

export interface UpdateRoutineParametersRequest {
  readonly intervalKm: number | null;
  readonly intervalMonths: number | null;
  readonly dueSoonThresholdPercent: number | null;
}

const DEFAULT_ICON_BY_TYPE: Readonly<Record<string, string>> = {
  OIL_CHANGE: 'oil_barrel',
  BRAKES: 'album',
  TIRES: 'tire_repair',
};

export function mapPlanSummaryDtoToVm(dto: MaintenancePlanSummaryDto): MaintenancePlanSummaryVm {
  return {
    vehicleId: dto.vehicleId,
    plate: dto.plate,
    customerId: dto.customerId,
    status: dto.status,
    odometerKm: dto.odometerKm,
    worstDueStatus: dto.worstDueStatus,
    worstConsumedPercent: dto.worstConsumedPercent,
    worstTriggeredBy: dto.worstTriggeredBy ?? null,
    nextRoutineName: dto.nextRoutineName ?? null,
    nextRemainingKind: dto.nextRemainingKind ?? null,
    nextRemainingValue: dto.nextRemainingValue ?? null,
  };
}

export function mapRoutineDtoToVm(dto: RoutineDto): RoutineVm {
  return {
    routineId: dto.routineId,
    vehicleId: dto.vehicleId,
    plate: dto.plate,
    name: dto.name,
    typeCode: dto.typeCode,
    icon: dto.icon ?? DEFAULT_ICON_BY_TYPE[dto.typeCode] ?? 'build',
    state: dto.state,
    dueStatus: dto.dueStatus,
    triggeredBy: dto.triggeredBy,
    tracks: dto.tracks.map((t: RoutineTrackVm) => ({ ...t })),
    nextDueKm: dto.nextDueKm,
    nextDueDate: dto.nextDueDate,
    cycleStartKm: dto.cycleStartKm ?? 0,
    cycleStartDate: dto.cycleStartDate ?? '2026-11-12',
    intervalKm: dto.intervalKm ?? null,
    intervalMonths: dto.intervalMonths ?? null,
    defaultIntervalKm: dto.defaultIntervalKm ?? 10_000,
    defaultIntervalMonths: dto.defaultIntervalMonths ?? 6,
    defaultDueSoonPercent: dto.defaultDueSoonPercent ?? 10,
    intervalKmOverride: dto.intervalKmOverride ?? null,
    intervalMonthsOverride: dto.intervalMonthsOverride ?? null,
    dueSoonPercentOverride: dto.dueSoonPercentOverride ?? null,
    effectiveDueSoonPercent: dto.effectiveDueSoonPercent,
    thresholdSource: dto.thresholdSource,
    openServiceOrderId: dto.openServiceOrderId,
    openServiceOrderNumber: dto.openServiceOrderNumber ?? dto.openServiceOrderId,
    openServiceOrderStatus: dto.openServiceOrderStatus ?? null,
    canCreateOrder: dto.canCreateOrder,
  };
}

export function mapPlanDetailDtoToVm(dto: MaintenancePlanDetailDto): MaintenancePlanDetailVm {
  return {
    vehicleId: dto.vehicleId,
    plate: dto.plate,
    customerId: dto.customerId,
    status: dto.status,
    odometerKm: dto.odometerKm,
    routines: dto.routines.map(mapRoutineDtoToVm),
  };
}

export function mapOdometerReadingDtoToVm(dto: OdometerReadingDto): OdometerReadingVm {
  return {
    kilometers: dto.kilometers,
    readAt: dto.readAt,
    outcome: dto.outcome,
    ignoredReason: dto.ignoredReason,
  };
}

export function mapAlertDtoToVm(dto: AlertDto): AlertVm {
  return {
    alertId: dto.alertId,
    vehicleId: dto.vehicleId,
    plate: dto.plate,
    customerId: dto.customerId,
    routineId: dto.routineId,
    routineName: dto.routineName,
    routineTypeCode: dto.routineTypeCode ?? 'OIL_CHANGE',
    type: dto.type,
    triggeredBy: dto.triggeredBy,
    status: dto.status,
    raisedAt: dto.raisedAt,
    acknowledgedBy: dto.acknowledgedBy,
    resolutionReason: dto.resolutionReason,
  };
}

@Injectable({
  providedIn: 'root',
})
export class MaintenanceApi {
  private readonly apiClient: ApiClient = inject(ApiClient);

  getPlanSummaries(customerId?: string): Observable<readonly MaintenancePlanSummaryVm[]> {
    return this.apiClient
      .get<readonly MaintenancePlanSummaryDto[]>(ApiRoutes.maintenance.plans(customerId))
      .pipe(map((dtos: readonly MaintenancePlanSummaryDto[]) => dtos.map(mapPlanSummaryDtoToVm)));
  }

  getPlanByVehicle(vehicleId: string): Observable<MaintenancePlanDetailVm | null> {
    return this.apiClient
      .get<MaintenancePlanDetailDto | null>(ApiRoutes.maintenance.planByVehicle(vehicleId))
      .pipe(
        map((dto: MaintenancePlanDetailDto | null) => (dto ? mapPlanDetailDtoToVm(dto) : null))
      );
  }

  getOdometerReadings(vehicleId: string): Observable<readonly OdometerReadingVm[]> {
    return this.apiClient
      .get<readonly OdometerReadingDto[]>(ApiRoutes.maintenance.vehicleOdometerReadings(vehicleId))
      .pipe(map((dtos: readonly OdometerReadingDto[]) => dtos.map(mapOdometerReadingDtoToVm)));
  }

  updateRoutineParameters(
    vehicleId: string,
    routineId: string,
    body: UpdateRoutineParametersRequest
  ): Observable<RoutineVm> {
    return this.apiClient
      .put<UpdateRoutineParametersRequest, RoutineDto>(
        ApiRoutes.maintenance.routineById(vehicleId, routineId),
        body
      )
      .pipe(map(mapRoutineDtoToVm));
  }

  updateRoutineState(
    vehicleId: string,
    routineId: string,
    state: RoutineState
  ): Observable<RoutineVm> {
    return this.apiClient
      .patch<{ readonly state: RoutineState }, RoutineDto>(
        ApiRoutes.maintenance.routineState(vehicleId, routineId),
        { state }
      )
      .pipe(map(mapRoutineDtoToVm));
  }

  reevaluate(vehicleId?: string): Observable<ReevaluationSummaryVm> {
    const payload: { readonly vehicleId?: string } = vehicleId ? { vehicleId } : {};
    return this.apiClient.post<{ readonly vehicleId?: string }, ReevaluationSummaryVm>(
      ApiRoutes.maintenance.reevaluations(),
      payload
    );
  }

  getAlerts(query?: AlertsQuery): Observable<readonly AlertVm[]> {
    return this.apiClient
      .get<readonly AlertDto[]>(ApiRoutes.maintenance.alerts(query))
      .pipe(map((dtos: readonly AlertDto[]) => dtos.map(mapAlertDtoToVm)));
  }

  acknowledgeAlert(alertId: string, acknowledgedBy: string): Observable<AlertVm> {
    return this.apiClient
      .post<{ readonly acknowledgedBy: string }, AlertDto>(
        ApiRoutes.maintenance.acknowledgeAlert(alertId),
        { acknowledgedBy }
      )
      .pipe(map(mapAlertDtoToVm));
  }
}

