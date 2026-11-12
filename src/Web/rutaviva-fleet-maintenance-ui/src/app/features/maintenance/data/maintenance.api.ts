import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of, switchMap, throwError } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { AlertsQuery, ApiRoutes } from '../../../core/api/api-routes';
import { SessionStore } from '../../../core/context/session.store';
import {
  AlertResolutionReason,
  AlertStatus,
  AlertType,
  DueStatus,
  MaintenancePlanStatus,
  OdometerReadingIgnoredReason,
  OdometerReadingOutcome,
  RoutineState,
  TriggeredBy,
} from '../../../core/models/enums';
import { RoutineTrackVm } from '../../../shared/models/routine-track.vm';
import {
  AlertsPageVm,
  AlertVm,
  MaintenancePlanDetailVm,
  MaintenancePlanSummaryVm,
  NextMaintenanceKind,
  NextMaintenanceVm,
  OdometerReadingVm,
  ReevaluationSummaryVm,
  RegisterServiceRequest,
  RoutineVm,
} from '../models/maintenance.vm';

export type ParameterSourceDto = 'RoutineType' | 'Override';

export interface PagedResultDto<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

export interface NextMaintenanceDto {
  readonly routineName: string;
  readonly kind: NextMaintenanceKind;
  readonly remaining: number;
  readonly dueStatus: DueStatus;
  readonly triggeredBy: TriggeredBy | null;
}

export interface MaintenancePlanSummaryDto {
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
  readonly nextMaintenance?: NextMaintenanceDto | null;
}

export interface RoutineDto {
  readonly routineId: string;
  readonly routineTypeCode: string;
  readonly name: string;
  readonly state: RoutineState;
  readonly dueStatus: DueStatus;
  readonly triggeredBy: TriggeredBy | null;
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
  readonly distanceTrack: RoutineTrackVm | null;
  readonly timeTrack: RoutineTrackVm | null;
}

export interface MaintenancePlanDetailDto {
  readonly vehicleId: string;
  readonly plate: string;
  readonly planStatus: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly worstDueStatus: DueStatus | null;
  readonly maxConsumedPercent: number;
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

interface UpdateRoutineParametersBody extends UpdateRoutineParametersRequest {
  readonly updatedBy: string;
}

interface ChangeRoutineStateBody {
  readonly state: RoutineState;
  readonly updatedBy: string;
}

export const ALERTS_PAGE_SIZE = 100;
export const ALERT_STATUSES_TO_COUNT: readonly AlertStatus[] = [
  'Active',
  'Acknowledged',
  'Resolved',
];

const HTTP_NOT_FOUND = 404;
const SERVICES_SEGMENT = '/services';
const OVERRIDE_SOURCE: ParameterSourceDto = 'Override';
const DEFAULT_ICON = 'build';

const ICON_BY_TYPE_CODE: Readonly<Record<string, string>> = {
  OIL_CHANGE: 'oil_barrel',
  BRAKES: 'album',
  TIRES: 'tire_repair',
};

function overrideOf(value: number | null, source: ParameterSourceDto): number | null {
  return source === OVERRIDE_SOURCE ? value : null;
}

function typeDefaultOf(value: number | null, source: ParameterSourceDto): number | null {
  return source === OVERRIDE_SOURCE ? null : value;
}

function tracksOf(dto: RoutineDto): readonly RoutineTrackVm[] {
  return [dto.distanceTrack, dto.timeTrack]
    .filter((track: RoutineTrackVm | null): track is RoutineTrackVm => track !== null)
    .map((track: RoutineTrackVm) => ({ ...track }));
}

function mapNextMaintenanceDto(
  dto: NextMaintenanceDto | null | undefined
): NextMaintenanceVm | null {
  if (!dto) {
    return null;
  }
  return {
    routineName: dto.routineName,
    kind: dto.kind,
    remaining: dto.remaining,
    dueStatus: dto.dueStatus,
    triggeredBy: dto.triggeredBy,
  };
}

export function mapPlanSummaryDtoToVm(dto: MaintenancePlanSummaryDto): MaintenancePlanSummaryVm {
  return {
    vehicleId: dto.vehicleId,
    plate: dto.plate,
    status: dto.planStatus,
    odometerKm: dto.odometerKm,
    worstDueStatus: dto.worstDueStatus,
    maxConsumedPercent: dto.maxConsumedPercent,
    upToDateCount: dto.upToDateCount,
    dueSoonCount: dto.dueSoonCount,
    overdueCount: dto.overdueCount,
    inactiveCount: dto.inactiveCount,
    nextMaintenance: mapNextMaintenanceDto(dto.nextMaintenance),
  };
}

export function mapRoutineDtoToVm(dto: RoutineDto, vehicleId: string, plate: string): RoutineVm {
  return {
    routineId: dto.routineId,
    vehicleId,
    plate,
    name: dto.name,
    typeCode: dto.routineTypeCode,
    icon: ICON_BY_TYPE_CODE[dto.routineTypeCode] ?? DEFAULT_ICON,
    state: dto.state,
    dueStatus: dto.dueStatus,
    triggeredBy: dto.triggeredBy,
    tracks: tracksOf(dto),
    nextDueKm: dto.nextDueKm,
    nextDueDate: dto.nextDueDate,
    cycleStartKm: dto.cycleStartKm,
    cycleStartDate: dto.cycleStartDate,
    intervalKm: dto.intervalKm,
    intervalMonths: dto.intervalMonths,
    defaultIntervalKm: typeDefaultOf(dto.intervalKm, dto.intervalKmSource),
    defaultIntervalMonths: typeDefaultOf(dto.intervalMonths, dto.intervalMonthsSource),
    defaultDueSoonPercent: typeDefaultOf(dto.dueSoonPercent, dto.dueSoonPercentSource),
    intervalKmOverride: overrideOf(dto.intervalKm, dto.intervalKmSource),
    intervalMonthsOverride: overrideOf(dto.intervalMonths, dto.intervalMonthsSource),
    dueSoonPercentOverride: overrideOf(dto.dueSoonPercent, dto.dueSoonPercentSource),
    effectiveDueSoonPercent: dto.dueSoonPercent,
    thresholdSource: dto.dueSoonPercentSource === OVERRIDE_SOURCE ? 'Routine' : 'Type',
    openServiceOrderId: null,
    openServiceOrderNumber: null,
    openServiceOrderStatus: null,
    canCreateOrder: false,
  };
}

export function mapPlanDetailDtoToVm(dto: MaintenancePlanDetailDto): MaintenancePlanDetailVm {
  return {
    vehicleId: dto.vehicleId,
    plate: dto.plate,
    status: dto.planStatus,
    odometerKm: dto.odometerKm,
    worstDueStatus: dto.worstDueStatus,
    maxConsumedPercent: dto.maxConsumedPercent,
    routines: dto.routines.map((routine: RoutineDto) =>
      mapRoutineDtoToVm(routine, dto.vehicleId, dto.plate)
    ),
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
    type: dto.type,
    triggeredBy: dto.triggeredBy,
    status: dto.status,
    raisedAt: dto.raisedAt,
    acknowledgedBy: dto.acknowledgedBy,
    resolutionReason: dto.resolutionReason,
  };
}

function notFoundToNull(error: unknown): Observable<null> {
  if (error instanceof HttpErrorResponse && error.status === HTTP_NOT_FOUND) {
    return of(null);
  }
  return throwError(() => error);
}

function requireRoutine(plan: MaintenancePlanDetailVm | null, routineId: string): RoutineVm {
  const routine: RoutineVm | undefined = plan?.routines.find(
    (r: RoutineVm) => r.routineId === routineId
  );
  if (!routine) {
    throw new Error(`Routine ${routineId} not found in plan`);
  }
  return routine;
}

function alertsUrl(query: AlertsQuery): string {
  const base: string = ApiRoutes.maintenance.alerts(query);
  const separator: string = base.includes('?') ? '&' : '?';
  return `${base}${separator}pageSize=${ALERTS_PAGE_SIZE}`;
}

@Injectable({
  providedIn: 'root',
})
export class MaintenanceApi {
  private readonly apiClient: ApiClient = inject(ApiClient);
  private readonly sessionStore: SessionStore = inject(SessionStore);

  getPlanSummaries(customerId?: string): Observable<readonly MaintenancePlanSummaryVm[]> {
    return this.apiClient
      .get<readonly MaintenancePlanSummaryDto[]>(ApiRoutes.maintenance.plans(customerId))
      .pipe(map((dtos: readonly MaintenancePlanSummaryDto[]) => dtos.map(mapPlanSummaryDtoToVm)));
  }

  getPlanByVehicle(vehicleId: string): Observable<MaintenancePlanDetailVm | null> {
    return this.apiClient
      .get<MaintenancePlanDetailDto>(ApiRoutes.maintenance.planByVehicle(vehicleId))
      .pipe(
        map(mapPlanDetailDtoToVm),
        catchError(notFoundToNull)
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
    request: UpdateRoutineParametersRequest
  ): Observable<RoutineVm> {
    const body: UpdateRoutineParametersBody = {
      ...request,
      updatedBy: this.sessionStore.currentActorName(),
    };
    return this.apiClient
      .put<UpdateRoutineParametersBody, void>(
        ApiRoutes.maintenance.routineById(vehicleId, routineId),
        body
      )
      .pipe(switchMap(() => this.reloadRoutine(vehicleId, routineId)));
  }

  updateRoutineState(
    vehicleId: string,
    routineId: string,
    state: RoutineState
  ): Observable<RoutineVm> {
    const body: ChangeRoutineStateBody = {
      state,
      updatedBy: this.sessionStore.currentActorName(),
    };
    return this.apiClient
      .patch<ChangeRoutineStateBody, void>(
        ApiRoutes.maintenance.routineState(vehicleId, routineId),
        body
      )
      .pipe(switchMap(() => this.reloadRoutine(vehicleId, routineId)));
  }

  registerService(
    vehicleId: string,
    routineId: string,
    request: RegisterServiceRequest
  ): Observable<MaintenancePlanDetailVm | null> {
    const url = `${ApiRoutes.maintenance.routineById(vehicleId, routineId)}${SERVICES_SEGMENT}`;
    return this.apiClient
      .post<RegisterServiceRequest, void>(url, request)
      .pipe(switchMap(() => this.getPlanByVehicle(vehicleId)));
  }

  reevaluate(vehicleId?: string): Observable<ReevaluationSummaryVm> {
    const payload: { readonly vehicleId?: string } = vehicleId ? { vehicleId } : {};
    return this.apiClient.post<{ readonly vehicleId?: string }, ReevaluationSummaryVm>(
      ApiRoutes.maintenance.reevaluations(),
      payload
    );
  }

  getAlerts(query: AlertsQuery): Observable<AlertsPageVm> {
    return this.apiClient.get<PagedResultDto<AlertDto>>(alertsUrl(query)).pipe(
      map((page: PagedResultDto<AlertDto>) => ({
        items: page.items.map(mapAlertDtoToVm),
        totalCount: page.totalCount,
      }))
    );
  }

  acknowledgeAlert(alertId: string): Observable<void> {
    const body: { readonly acknowledgedBy: string } = {
      acknowledgedBy: this.sessionStore.currentActorName(),
    };
    return this.apiClient
      .post<{ readonly acknowledgedBy: string }, void>(
        ApiRoutes.maintenance.acknowledgeAlert(alertId),
        body
      )
      .pipe(map(() => undefined));
  }

  private reloadRoutine(vehicleId: string, routineId: string): Observable<RoutineVm> {
    return this.getPlanByVehicle(vehicleId).pipe(
      map((plan: MaintenancePlanDetailVm | null) => requireRoutine(plan, routineId))
    );
  }
}
