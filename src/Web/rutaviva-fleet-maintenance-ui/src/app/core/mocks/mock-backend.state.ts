import { Injectable, inject } from '@angular/core';
import {
  OdometerReadingIgnoredReason,
  OdometerReadingOutcome,
  RoutineState,
  VehicleStatus,
} from '../models/enums';
import { NotificationsService } from '../realtime/notifications.service';
import { AlertSearch, MockAlertBook } from './mock-alert-book';
import { dateOnly } from './mock-backend.dates';
import {
  AlertDto,
  CustomerDetailDto,
  CustomerSummaryDto,
  OdometerReadingDto,
  PagedResultDto,
  PlanDetailDto,
  PlanSummaryDto,
  ReevaluationSummaryDto,
  RoutineDetailDto,
  RoutineTypeDto,
  TelemetryResponseDto,
  VehicleDetailDto,
  VehicleSummaryDto,
  WorkshopDetailDto,
  WorkshopSummaryDto,
} from './mock-backend.dtos';
import { ERRORS } from './mock-backend.errors';
import { evaluateRoutine, summarizePlan } from './mock-backend.evaluator';
import {
  toAlertEvent,
  toCustomerDetail,
  toCustomerSummary,
  toRoutineTypeDto,
  toVehicleDetail,
  toVehicleSummary,
  toWorkshopDetail,
  toWorkshopSummary,
  VehicleRelations,
} from './mock-backend.mappers';
import {
  ACKNOWLEDGED_SEED_PLATE,
  ACKNOWLEDGER_NAME,
  buildCustomers,
  buildRoutines,
  buildRoutineTypes,
  buildVehicles,
  buildWorkshops,
  MockApproverRecord,
  MockCustomerRecord,
  MockRoutineRecord,
  MockRoutineTypeRecord,
  MockVehicleRecord,
  MockWorkshopRecord,
  SEED_TODAY,
} from './mock-backend.seed';

const FIRST_PAGE = 1;
const MAX_VEHICLE_PAGE_SIZE = 200;
const DEFAULT_READINGS_LIMIT = 50;
const MAX_DUE_SOON_PERCENT = 100;
const ORDERS_NOT_REQUESTED = 0;
const SEED_READING_OFFSETS_KM: readonly number[] = [0, 0, 50, 420];
const SEED_READING_TIMES: readonly string[] = [
  '2026-11-12T08:42:00-05:00',
  '2026-11-12T08:41:00-05:00',
  '2026-11-12T08:40:00-05:00',
  '2026-11-11T17:05:00-05:00',
];

export interface VehicleListQuery {
  readonly customerId: string | null;
  readonly status: VehicleStatus | null;
  readonly search: string | null;
  readonly page: number;
  readonly pageSize: number;
}

export interface RoutineParametersInput {
  readonly intervalKm: number | null;
  readonly intervalMonths: number | null;
  readonly dueSoonPercent: number | null;
}

export interface RoutineTypeParametersInput {
  readonly intervalKm: number | null;
  readonly intervalMonths: number | null;
  readonly dueSoonPercent: number;
}

@Injectable({
  providedIn: 'root',
})
export class MockBackendState {
  private readonly notifications: NotificationsService = inject(NotificationsService);
  private currentDateIso: string = SEED_TODAY;

  private readonly customers: MockCustomerRecord[] = buildCustomers();
  private readonly workshops: MockWorkshopRecord[] = buildWorkshops();
  private readonly routineTypes: MockRoutineTypeRecord[] = buildRoutineTypes();
  private readonly vehicles: MockVehicleRecord[] = buildVehicles();
  private readonly routines: MockRoutineRecord[] = buildRoutines(this.vehicles);
  private readonly readings: Map<string, OdometerReadingDto[]> = new Map();
  private readonly alerts: MockAlertBook = new MockAlertBook(
    (alert: AlertDto) => this.notifications.emitAlertChanged(toAlertEvent(alert)),
    () => this.currentDateIso
  );

  constructor() {
    this.seedReadings();
    this.reconcileAll(true);
    this.acknowledgeSeededAlerts();
  }

  listCustomers(): readonly CustomerSummaryDto[] {
    return this.customers.map(toCustomerSummary);
  }

  getCustomer(id: string): CustomerDetailDto {
    return toCustomerDetail(this.requireCustomer(id));
  }

  listWorkshops(status: string | null, city: string | null): readonly WorkshopSummaryDto[] {
    return this.workshops
      .filter((w: MockWorkshopRecord) => !status || w.status === status)
      .filter((w: MockWorkshopRecord) => !city || w.city.toLowerCase() === city.toLowerCase())
      .map(toWorkshopSummary);
  }

  getWorkshop(id: string): WorkshopDetailDto {
    const workshop: MockWorkshopRecord | undefined = this.workshops.find((w: MockWorkshopRecord) => w.id === id);
    if (!workshop) {
      throw ERRORS.workshopNotFound;
    }
    return toWorkshopDetail(workshop);
  }

  listVehicles(query: VehicleListQuery): PagedResultDto<VehicleSummaryDto> {
    this.validateVehiclePage(query);
    const matching: MockVehicleRecord[] = this.vehicles.filter((v: MockVehicleRecord) => this.vehicleMatches(v, query));
    const start: number = (query.page - FIRST_PAGE) * query.pageSize;
    return {
      items: matching.slice(start, start + query.pageSize).map((v: MockVehicleRecord) => toVehicleSummary(v, this.relationsOf(v))),
      page: query.page,
      pageSize: query.pageSize,
      totalCount: matching.length,
    };
  }

  getVehicle(id: string): VehicleDetailDto {
    return this.detailOf(this.requireVehicle(id));
  }

  getVehicleByPlate(plate: string): VehicleDetailDto {
    return this.detailOf(this.requireVehicleByPlate(plate));
  }

  changeVehicleStatus(id: string, status: unknown): void {
    if (status !== 'Active' && status !== 'Inactive') {
      throw ERRORS.vehicleStatusInvalid;
    }
    const vehicle: MockVehicleRecord = this.requireVehicle(id);
    vehicle.status = status;
    vehicle.statusChangedAt = new Date().toISOString();
    if (status === 'Active') {
      this.reconcileAll(false);
    }
    this.emitVehicle(vehicle);
  }

  listPlans(customerId: string | null): readonly PlanSummaryDto[] {
    return this.vehicles
      .filter((v: MockVehicleRecord) => !customerId || v.customerId === customerId)
      .map((v: MockVehicleRecord) => summarizePlan(this.planHeader(v), this.evaluateAll(v)));
  }

  getPlan(vehicleId: string): PlanDetailDto {
    const vehicle: MockVehicleRecord | undefined = this.findVehicle(vehicleId);
    if (!vehicle) {
      throw ERRORS.planNotFound;
    }
    const routines: RoutineDetailDto[] = this.evaluateAll(vehicle);
    return { ...summarizePlan(this.planHeader(vehicle), routines), routines };
  }

  listReadings(vehicleId: string, limit: number = DEFAULT_READINGS_LIMIT): readonly OdometerReadingDto[] {
    return (this.readings.get(vehicleId) ?? []).slice(0, limit);
  }

  ingestReading(plate: string, odometerKm: number, readAt: string): TelemetryResponseDto {
    if (plate.trim() === '') {
      throw ERRORS.plateRequired;
    }
    const vehicle: MockVehicleRecord | undefined = this.findVehicleByPlate(plate);
    if (!vehicle) {
      return { outcome: 'Ignored', reason: 'UnknownVehicle' };
    }
    const accepted: boolean = odometerKm > vehicle.odometerKm;
    const outcome: OdometerReadingOutcome = accepted ? 'Accepted' : 'Ignored';
    const reason: OdometerReadingIgnoredReason | null = accepted ? null : 'NotGreaterThanCurrent';
    this.storeReading(vehicle, { kilometers: odometerKm, readAt, outcome, ignoredReason: reason });
    if (accepted) {
      this.applyOdometer(vehicle, odometerKm);
    }
    this.emitReading(vehicle, odometerKm, readAt, outcome, reason);
    return { outcome, reason };
  }

  changeRoutineState(vehicleId: string, routineId: string, state: unknown, updatedBy: string): void {
    this.requireUpdatedBy(updatedBy);
    if (state !== 'Active' && state !== 'Inactive') {
      throw ERRORS.stateRequired;
    }
    const { vehicle, routine } = this.requireRoutine(vehicleId, routineId);
    routine.state = state;
    this.afterStateChange(routine, state);
    this.emitRoutine(routine, vehicle);
  }

  updateRoutineParameters(
    vehicleId: string,
    routineId: string,
    input: RoutineParametersInput,
    updatedBy: string
  ): void {
    this.requireUpdatedBy(updatedBy);
    this.validateIntervals(input.intervalKm, input.intervalMonths);
    this.validateDueSoon(input.dueSoonPercent);
    const { vehicle, routine } = this.requireRoutine(vehicleId, routineId);
    routine.intervalKmOverride = input.intervalKm;
    routine.intervalMonthsOverride = input.intervalMonths;
    routine.dueSoonPercentOverride = input.dueSoonPercent;
    this.reconcileAll(false);
    this.emitRoutine(routine, vehicle);
  }

  registerService(
    vehicleId: string,
    routineId: string,
    servicedKm: number | null,
    servicedAt: string | null
  ): void {
    if (servicedKm === null || servicedAt === null) {
      throw ERRORS.serviceDataRequired;
    }
    const { vehicle, routine } = this.requireRoutine(vehicleId, routineId);
    this.restartCycle(routine, servicedKm, dateOnly(servicedAt));
    this.alerts.resolveOpen(routine.routineId, 'RoutineRestarted');
    if (servicedKm > vehicle.odometerKm) {
      vehicle.odometerKm = servicedKm;
    }
    this.reconcileAll(false);
    this.emitRoutine(routine, vehicle);
  }

  reevaluate(asOfDate: string | null): ReevaluationSummaryDto {
    if (asOfDate && dateOnly(asOfDate) > this.currentDateIso) {
      this.currentDateIso = dateOnly(asOfDate);
    }
    this.reconcileAll(false);
    const statuses: RoutineDetailDto[] = this.activePlanRoutines();
    return {
      evaluatedPlans: this.vehicles.filter((v: MockVehicleRecord) => v.status === 'Active').length,
      dueSoon: statuses.filter((r: RoutineDetailDto) => r.dueStatus === 'DueSoon').length,
      overdue: statuses.filter((r: RoutineDetailDto) => r.dueStatus === 'Overdue').length,
      ordersRequested: ORDERS_NOT_REQUESTED,
    };
  }

  listRoutineTypes(): readonly RoutineTypeDto[] {
    return this.routineTypes.map(toRoutineTypeDto);
  }

  updateRoutineType(id: string, input: RoutineTypeParametersInput, updatedBy: string): void {
    this.requireUpdatedBy(updatedBy);
    this.validateTypeIntervals(input);
    this.validateDueSoon(input.dueSoonPercent);
    const type: MockRoutineTypeRecord | undefined = this.routineTypes.find((t: MockRoutineTypeRecord) => t.id === id);
    if (!type) {
      throw ERRORS.routineTypeNotFound;
    }
    type.defaultIntervalKm = input.intervalKm;
    type.defaultIntervalMonths = input.intervalMonths;
    type.dueSoonPercent = input.dueSoonPercent;
    type.updatedBy = updatedBy;
    type.updatedAt = new Date().toISOString();
    this.reconcileAll(false);
  }

  searchAlerts(query: AlertSearch): PagedResultDto<AlertDto> {
    return this.alerts.search(query);
  }

  acknowledgeAlert(alertId: string, acknowledgedBy: string): void {
    if (acknowledgedBy.trim() === '') {
      throw ERRORS.acknowledgedByRequired;
    }
    this.alerts.acknowledge(alertId, acknowledgedBy);
  }

  private findVehicle(id: string): MockVehicleRecord | undefined {
    return this.vehicles.find((v: MockVehicleRecord) => v.id.toLowerCase() === id.toLowerCase());
  }

  private findVehicleByPlate(plate: string): MockVehicleRecord | undefined {
    return this.vehicles.find((v: MockVehicleRecord) => v.plate.toLowerCase() === plate.trim().toLowerCase());
  }

  private requireVehicle(id: string): MockVehicleRecord {
    const vehicle: MockVehicleRecord | undefined = this.findVehicle(id);
    if (!vehicle) {
      throw ERRORS.vehicleNotFound;
    }
    return vehicle;
  }

  private requireVehicleByPlate(plate: string): MockVehicleRecord {
    const vehicle: MockVehicleRecord | undefined = this.findVehicleByPlate(plate);
    if (!vehicle) {
      throw ERRORS.vehicleNotFound;
    }
    return vehicle;
  }

  private requireCustomer(id: string): MockCustomerRecord {
    const customer: MockCustomerRecord | undefined = this.customers.find((c: MockCustomerRecord) => c.id === id);
    if (!customer) {
      throw ERRORS.customerNotFound;
    }
    return customer;
  }

  private requireRoutine(
    vehicleId: string,
    routineId: string
  ): { readonly vehicle: MockVehicleRecord; readonly routine: MockRoutineRecord } {
    const vehicle: MockVehicleRecord | undefined = this.findVehicle(vehicleId);
    if (!vehicle) {
      throw ERRORS.planNotFound;
    }
    const routine: MockRoutineRecord | undefined = this.routines.find(
      (r: MockRoutineRecord) => r.vehicleId === vehicle.id && r.routineId === routineId
    );
    if (!routine) {
      throw ERRORS.routineNotFound;
    }
    return { vehicle, routine };
  }

  private requireUpdatedBy(updatedBy: string): void {
    if (updatedBy.trim() === '') {
      throw ERRORS.updatedByRequired;
    }
  }

  private validateVehiclePage(query: VehicleListQuery): void {
    if (query.page < FIRST_PAGE) {
      throw ERRORS.invalidVehiclePage;
    }
    if (query.pageSize < FIRST_PAGE || query.pageSize > MAX_VEHICLE_PAGE_SIZE) {
      throw ERRORS.invalidVehiclePageSize;
    }
  }

  private validateIntervals(intervalKm: number | null, intervalMonths: number | null): void {
    if (intervalKm !== null && intervalKm <= 0) {
      throw ERRORS.intervalKmNotPositive;
    }
    if (intervalMonths !== null && intervalMonths <= 0) {
      throw ERRORS.intervalMonthsNotPositive;
    }
  }

  private validateTypeIntervals(input: RoutineTypeParametersInput): void {
    if (input.intervalKm === null && input.intervalMonths === null) {
      throw ERRORS.intervalEmpty;
    }
    this.validateIntervals(input.intervalKm, input.intervalMonths);
  }

  private validateDueSoon(percent: number | null): void {
    if (percent !== null && (percent <= 0 || percent >= MAX_DUE_SOON_PERCENT)) {
      throw ERRORS.dueSoonOutOfRange;
    }
  }

  private vehicleMatches(vehicle: MockVehicleRecord, query: VehicleListQuery): boolean {
    if (query.customerId && vehicle.customerId !== query.customerId) {
      return false;
    }
    if (query.status && vehicle.status !== query.status) {
      return false;
    }
    return !query.search || this.searchText(vehicle).includes(query.search.trim().toLowerCase());
  }

  private searchText(vehicle: MockVehicleRecord): string {
    const owner: string = this.relationsOf(vehicle).customer.name;
    return [vehicle.plate, vehicle.vin ?? '', vehicle.brand, vehicle.model, owner].join(' ').toLowerCase();
  }

  private relationsOf(vehicle: MockVehicleRecord): VehicleRelations {
    const customer: MockCustomerRecord = this.requireCustomer(vehicle.customerId);
    const workshop: MockWorkshopRecord = this.workshops.find((w: MockWorkshopRecord) => w.id === vehicle.workshopId)!;
    const responsible: MockApproverRecord | null =
      customer.approvers.find((a: MockApproverRecord) => a.id === vehicle.responsibleApproverId) ?? null;
    return { customer, workshop, responsible };
  }

  private detailOf(vehicle: MockVehicleRecord): VehicleDetailDto {
    return toVehicleDetail(vehicle, this.relationsOf(vehicle));
  }

  private planHeader(
    vehicle: MockVehicleRecord
  ): Pick<PlanSummaryDto, 'vehicleId' | 'plate' | 'planStatus' | 'odometerKm'> {
    return {
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      planStatus: vehicle.status === 'Active' ? 'Active' : 'Suspended',
      odometerKm: vehicle.odometerKm,
    };
  }

  private evaluate(routine: MockRoutineRecord, vehicle: MockVehicleRecord): RoutineDetailDto {
    const type: MockRoutineTypeRecord = this.routineTypes.find((t: MockRoutineTypeRecord) => t.code === routine.typeCode)!;
    return evaluateRoutine(routine, type, { odometerKm: vehicle.odometerKm, todayIso: this.currentDateIso });
  }

  private evaluateAll(vehicle: MockVehicleRecord): RoutineDetailDto[] {
    return this.routines
      .filter((r: MockRoutineRecord) => r.vehicleId === vehicle.id)
      .map((r: MockRoutineRecord) => this.evaluate(r, vehicle));
  }

  private activePlanRoutines(): RoutineDetailDto[] {
    return this.vehicles
      .filter((v: MockVehicleRecord) => v.status === 'Active')
      .flatMap((v: MockVehicleRecord) => this.evaluateAll(v))
      .filter((r: RoutineDetailDto) => r.state === 'Active');
  }

  private seedReadings(): void {
    for (const vehicle of this.vehicles) {
      this.readings.set(vehicle.id, vehicle.odometerKm > 0 ? this.buildSeedReadings(vehicle) : []);
    }
  }

  private buildSeedReadings(vehicle: MockVehicleRecord): OdometerReadingDto[] {
    return SEED_READING_TIMES.map((readAt: string, index: number) => {
      const kilometers: number = Math.max(0, vehicle.odometerKm - SEED_READING_OFFSETS_KM[index]!);
      const accepted: boolean = index === 0 || index === SEED_READING_TIMES.length - 1;
      return {
        kilometers,
        readAt,
        receivedAt: readAt,
        outcome: accepted ? 'Accepted' : 'Ignored',
        ignoredReason: accepted ? null : 'NotGreaterThanCurrent',
      };
    });
  }

  private storeReading(
    vehicle: MockVehicleRecord,
    reading: Omit<OdometerReadingDto, 'receivedAt'>
  ): void {
    const list: OdometerReadingDto[] = this.readings.get(vehicle.id) ?? [];
    list.unshift({ ...reading, receivedAt: new Date().toISOString() });
    this.readings.set(vehicle.id, list);
  }

  private applyOdometer(vehicle: MockVehicleRecord, odometerKm: number): void {
    vehicle.odometerKm = odometerKm;
    this.reconcileAll(false);
    for (const routine of this.routines.filter((r: MockRoutineRecord) => r.vehicleId === vehicle.id)) {
      this.emitRoutine(routine, vehicle);
    }
  }

  private afterStateChange(routine: MockRoutineRecord, state: RoutineState): void {
    if (state === 'Inactive') {
      this.alerts.resolveOpen(routine.routineId, 'RoutineDeactivated');
      return;
    }
    this.reconcileAll(false);
  }

  private restartCycle(routine: MockRoutineRecord, servicedKm: number, servicedDate: string): void {
    routine.cycleStartKm = servicedKm;
    routine.cycleStartDate = servicedDate;
    routine.cycleNumber += 1;
    routine.stickyOverdue = false;
  }

  private reconcileAll(silent: boolean): void {
    for (const routine of this.routines) {
      this.reconcileRoutine(routine, silent);
    }
  }

  private reconcileRoutine(routine: MockRoutineRecord, silent: boolean): void {
    const vehicle: MockVehicleRecord = this.requireVehicle(routine.vehicleId);
    if (routine.state !== 'Active' || vehicle.status !== 'Active') {
      return;
    }
    const detail: RoutineDetailDto = this.evaluate(routine, vehicle);
    this.applyDueStatus(routine, vehicle, detail, silent);
  }

  private applyDueStatus(
    routine: MockRoutineRecord,
    vehicle: MockVehicleRecord,
    detail: RoutineDetailDto,
    silent: boolean
  ): void {
    const origin = {
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      customerId: vehicle.customerId,
      routineId: routine.routineId,
      routineName: detail.name,
      cycleNumber: routine.cycleNumber,
    };
    if (detail.dueStatus === 'UpToDate') {
      this.alerts.resolveOpen(routine.routineId, 'ParametersChanged');
      return;
    }
    if (detail.dueStatus === 'DueSoon') {
      this.alerts.raise(origin, 'DueSoon', detail.triggeredBy!, silent);
      return;
    }
    routine.stickyOverdue = true;
    this.alerts.resolveOpen(routine.routineId, 'EscalatedToOverdue', 'DueSoon');
    this.alerts.raise(origin, 'Overdue', detail.triggeredBy!, silent);
  }

  private acknowledgeSeededAlerts(): void {
    const seeded: MockVehicleRecord | undefined = this.findVehicleByPlate(ACKNOWLEDGED_SEED_PLATE);
    if (seeded) {
      this.alerts.acknowledgeForVehicle(seeded.id, ACKNOWLEDGER_NAME);
    }
  }

  private emitVehicle(vehicle: MockVehicleRecord): void {
    this.notifications.emitVehicleChanged({
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      customerId: vehicle.customerId,
      status: vehicle.status,
      planStatus: vehicle.status === 'Active' ? 'Active' : 'Suspended',
      occurredAt: new Date().toISOString(),
    });
  }

  private emitReading(
    vehicle: MockVehicleRecord,
    kilometers: number,
    readAt: string,
    outcome: OdometerReadingOutcome,
    ignoredReason: OdometerReadingIgnoredReason | null
  ): void {
    this.notifications.emitOdometerReadingRecorded({
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      customerId: vehicle.customerId,
      kilometers,
      readAt,
      outcome,
      ignoredReason,
      currentOdometerKm: vehicle.odometerKm,
      occurredAt: new Date().toISOString(),
    });
  }

  private emitRoutine(routine: MockRoutineRecord, vehicle: MockVehicleRecord): void {
    const detail: RoutineDetailDto = this.evaluate(routine, vehicle);
    this.notifications.emitRoutineChanged({
      routineId: detail.routineId,
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      customerId: vehicle.customerId,
      routineTypeCode: detail.routineTypeCode,
      routineName: detail.name,
      state: detail.state,
      dueStatus: detail.dueStatus,
      triggeredBy: detail.triggeredBy,
      consumedPercent: detail.consumedPercent,
      effectiveDueSoonPercent: detail.dueSoonPercent,
      nextDueKm: detail.nextDueKm,
      nextDueDate: detail.nextDueDate,
      odometerKm: vehicle.odometerKm,
      occurredAt: new Date().toISOString(),
    });
  }
}
