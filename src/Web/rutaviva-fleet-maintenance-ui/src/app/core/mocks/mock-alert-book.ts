import {
  AlertResolutionReason,
  AlertStatus,
  AlertTrigger,
  AlertType,
} from '../models/enums';
import { AlertDto, PagedResultDto } from './mock-backend.dtos';
import { ERRORS } from './mock-backend.errors';
import { alertGuid } from './mock-backend.ids';

const MS_PER_SECOND = 1000;
const RAISED_TIME_SUFFIX = 'T08:00:00-05:00';
const FIRST_PAGE = 1;
const MAX_PAGE_SIZE = 100;
const DEFAULT_PAGE_SIZE = 20;

type MockAlertRecord = { -readonly [K in keyof AlertDto]: AlertDto[K] };

export interface AlertOrigin {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly cycleNumber: number;
}

export interface AlertSearch {
  readonly status: AlertStatus | null;
  readonly vehicleId: string | null;
  readonly customerId: string | null;
  readonly page: number;
  readonly pageSize: number;
}

export const DEFAULT_ALERT_SEARCH_PAGE: Pick<AlertSearch, 'page' | 'pageSize'> = {
  page: FIRST_PAGE,
  pageSize: DEFAULT_PAGE_SIZE,
};

export class MockAlertBook {
  private readonly alerts: MockAlertRecord[] = [];

  constructor(
    private readonly publish: (alert: AlertDto) => void,
    private readonly todayIso: () => string
  ) {}

  search(query: AlertSearch): PagedResultDto<AlertDto> {
    this.validate(query);
    const matching: MockAlertRecord[] = this.alerts.filter((a: MockAlertRecord) => this.matches(a, query));
    const ordered: MockAlertRecord[] = this.byNewestFirst(matching);
    const start: number = (query.page - FIRST_PAGE) * query.pageSize;
    return {
      items: ordered.slice(start, start + query.pageSize).map((a: MockAlertRecord) => ({ ...a })),
      page: query.page,
      pageSize: query.pageSize,
      totalCount: ordered.length,
    };
  }

  acknowledge(alertId: string, acknowledgedBy: string): void {
    const alert: MockAlertRecord | undefined = this.alerts.find((a: MockAlertRecord) => a.alertId === alertId);
    if (!alert) {
      throw ERRORS.alertNotFound;
    }
    if (alert.status !== 'Active') {
      throw ERRORS.alertInvalidTransition;
    }
    alert.status = 'Acknowledged';
    alert.acknowledgedBy = acknowledgedBy;
    alert.acknowledgedAt = new Date().toISOString();
    this.publish({ ...alert });
  }

  acknowledgeForVehicle(vehicleId: string, acknowledgedBy: string): void {
    for (const alert of this.alerts.filter(
      (a: MockAlertRecord) => a.vehicleId === vehicleId && a.status === 'Active'
    )) {
      alert.status = 'Acknowledged';
      alert.acknowledgedBy = acknowledgedBy;
      alert.acknowledgedAt = new Date().toISOString();
    }
  }

  raise(origin: AlertOrigin, type: AlertType, triggeredBy: AlertTrigger, silent: boolean): void {
    if (this.existsForCycle(origin, type)) {
      return;
    }
    const alert: MockAlertRecord = this.build(origin, type, triggeredBy);
    this.alerts.push(alert);
    if (!silent) {
      this.publish({ ...alert });
    }
  }

  resolveOpen(
    routineId: string,
    reason: AlertResolutionReason,
    onlyType: AlertType | null = null
  ): void {
    const open: MockAlertRecord[] = this.alerts.filter(
      (a: MockAlertRecord) =>
        a.routineId === routineId &&
        a.status !== 'Resolved' &&
        (onlyType === null || a.type === onlyType)
    );
    for (const alert of open) {
      alert.status = 'Resolved';
      alert.resolutionReason = reason;
      alert.resolvedAt = new Date().toISOString();
      this.publish({ ...alert });
    }
  }

  private validate(query: AlertSearch): void {
    if (query.page < FIRST_PAGE) {
      throw ERRORS.invalidAlertPage;
    }
    if (query.pageSize < FIRST_PAGE || query.pageSize > MAX_PAGE_SIZE) {
      throw ERRORS.invalidAlertPageSize;
    }
  }

  private matches(alert: MockAlertRecord, query: AlertSearch): boolean {
    return (
      (query.status === null || alert.status === query.status) &&
      (query.vehicleId === null || alert.vehicleId === query.vehicleId) &&
      (query.customerId === null || alert.customerId === query.customerId)
    );
  }

  private byNewestFirst(list: MockAlertRecord[]): MockAlertRecord[] {
    return [...list].sort(
      (a: MockAlertRecord, b: MockAlertRecord) => Date.parse(b.raisedAt) - Date.parse(a.raisedAt)
    );
  }

  private existsForCycle(origin: AlertOrigin, type: AlertType): boolean {
    return this.alerts.some(
      (a: MockAlertRecord) =>
        a.routineId === origin.routineId && a.cycleNumber === origin.cycleNumber && a.type === type
    );
  }

  private build(origin: AlertOrigin, type: AlertType, triggeredBy: AlertTrigger): MockAlertRecord {
    const sequence: number = this.alerts.length + 1;
    const baseMs: number = Date.parse(`${this.todayIso()}${RAISED_TIME_SUFFIX}`);
    return {
      alertId: alertGuid(sequence),
      vehicleId: origin.vehicleId,
      plate: origin.plate,
      customerId: origin.customerId,
      routineId: origin.routineId,
      routineName: origin.routineName,
      cycleNumber: origin.cycleNumber,
      type,
      triggeredBy,
      status: 'Active',
      raisedAt: new Date(baseMs + sequence * MS_PER_SECOND).toISOString(),
      acknowledgedAt: null,
      acknowledgedBy: null,
      resolvedAt: null,
      resolutionReason: null,
    };
  }
}
