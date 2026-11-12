import { HttpResponse } from '@angular/common/http';
import { AlertStatus, VehicleStatus } from '../models/enums';
import { AlertSearch } from './mock-alert-book';
import { NOT_IMPLEMENTED_ROUTE } from './mock-backend.errors';
import {
  acceptedResponse,
  noContentResponse,
  okResponse,
} from './mock-backend.http';
import { MockBackendState, VehicleListQuery } from './mock-backend.state';

const DEFAULT_PAGE = 1;
const DEFAULT_VEHICLE_PAGE_SIZE = 50;
const DEFAULT_ALERT_PAGE_SIZE = 20;
const DEFAULT_READINGS_LIMIT = 50;
const ID = '([^/]+)';

export interface RouteContext {
  readonly state: MockBackendState;
  readonly captures: readonly string[];
  readonly params: URLSearchParams;
  readonly body: Record<string, unknown>;
}

export interface MockRoute {
  readonly method: string;
  readonly pattern: RegExp;
  readonly handle: (context: RouteContext) => HttpResponse<unknown>;
}

function textOf(body: Record<string, unknown>, key: string): string {
  const value: unknown = body[key];
  return typeof value === 'string' ? value : '';
}

function textOrNull(body: Record<string, unknown>, key: string): string | null {
  const value: unknown = body[key];
  return typeof value === 'string' ? value : null;
}

function numberOrNull(body: Record<string, unknown>, key: string): number | null {
  const value: unknown = body[key];
  return typeof value === 'number' ? value : null;
}

function intParam(params: URLSearchParams, name: string, fallback: number): number {
  const raw: string | null = params.get(name);
  const parsed: number = raw === null ? fallback : Number(raw);
  return Number.isNaN(parsed) ? fallback : parsed;
}

function vehicleStatusOf(raw: string | null): VehicleStatus | null {
  return raw === 'Active' || raw === 'Inactive' ? raw : null;
}

function alertStatusOf(raw: string | null): AlertStatus | null {
  return raw === 'Active' || raw === 'Acknowledged' || raw === 'Resolved' ? raw : null;
}

function vehicleQuery(params: URLSearchParams): VehicleListQuery {
  return {
    customerId: params.get('customerId'),
    status: vehicleStatusOf(params.get('status')),
    search: params.get('search'),
    page: intParam(params, 'page', DEFAULT_PAGE),
    pageSize: intParam(params, 'pageSize', DEFAULT_VEHICLE_PAGE_SIZE),
  };
}

function alertQuery(params: URLSearchParams): AlertSearch {
  return {
    status: alertStatusOf(params.get('status')),
    vehicleId: params.get('vehicleId'),
    customerId: params.get('customerId'),
    page: intParam(params, 'page', DEFAULT_PAGE),
    pageSize: intParam(params, 'pageSize', DEFAULT_ALERT_PAGE_SIZE),
  };
}

function route(
  method: string,
  path: string,
  handle: (context: RouteContext) => HttpResponse<unknown>
): MockRoute {
  return { method, pattern: new RegExp(`^${path}$`), handle };
}

function notImplemented(): HttpResponse<unknown> {
  throw NOT_IMPLEMENTED_ROUTE;
}

const CUSTOMER_ROUTES: readonly MockRoute[] = [
  route('GET', '/api/v1/customers', (c) => okResponse(c.state.listCustomers())),
  route('GET', `/api/v1/customers/${ID}`, (c) => okResponse(c.state.getCustomer(c.captures[0]!))),
  route('GET', '/api/v1/workshops', (c) =>
    okResponse(c.state.listWorkshops(c.params.get('status'), c.params.get('city')))
  ),
  route('GET', `/api/v1/workshops/${ID}`, (c) => okResponse(c.state.getWorkshop(c.captures[0]!))),
];

const FLEET_ROUTES: readonly MockRoute[] = [
  route('GET', '/api/v1/fleet/vehicles', (c) => okResponse(c.state.listVehicles(vehicleQuery(c.params)))),
  route('GET', `/api/v1/fleet/vehicles/by-plate/${ID}`, (c) =>
    okResponse(c.state.getVehicleByPlate(c.captures[0]!))
  ),
  route('PATCH', `/api/v1/fleet/vehicles/${ID}/status`, (c) => {
    c.state.changeVehicleStatus(c.captures[0]!, c.body['status']);
    return noContentResponse();
  }),
  route('GET', `/api/v1/fleet/vehicles/${ID}`, (c) => okResponse(c.state.getVehicle(c.captures[0]!))),
];

const PLAN_ROUTES: readonly MockRoute[] = [
  route('GET', '/api/v1/maintenance/plans', (c) => okResponse(c.state.listPlans(c.params.get('customerId')))),
  route('GET', `/api/v1/maintenance/plans/${ID}/odometer-readings`, (c) =>
    okResponse(c.state.listReadings(c.captures[0]!, intParam(c.params, 'limit', DEFAULT_READINGS_LIMIT)))
  ),
  route('PATCH', `/api/v1/maintenance/plans/${ID}/routines/${ID}/state`, (c) => {
    c.state.changeRoutineState(c.captures[0]!, c.captures[1]!, c.body['state'], textOf(c.body, 'updatedBy'));
    return noContentResponse();
  }),
  route('POST', `/api/v1/maintenance/plans/${ID}/routines/${ID}/services`, (c) => {
    c.state.registerService(
      c.captures[0]!,
      c.captures[1]!,
      numberOrNull(c.body, 'servicedKm'),
      textOrNull(c.body, 'servicedAt')
    );
    return noContentResponse();
  }),
  route('PUT', `/api/v1/maintenance/plans/${ID}/routines/${ID}`, (c) => {
    c.state.updateRoutineParameters(
      c.captures[0]!,
      c.captures[1]!,
      {
        intervalKm: numberOrNull(c.body, 'intervalKm'),
        intervalMonths: numberOrNull(c.body, 'intervalMonths'),
        dueSoonPercent: numberOrNull(c.body, 'dueSoonThresholdPercent'),
      },
      textOf(c.body, 'updatedBy')
    );
    return noContentResponse();
  }),
  route('GET', `/api/v1/maintenance/plans/${ID}`, (c) => okResponse(c.state.getPlan(c.captures[0]!))),
];

const TELEMETRY_ROUTES: readonly MockRoute[] = [
  route('POST', '/api/v1/maintenance/odometer-readings', (c) =>
    acceptedResponse(
      c.state.ingestReading(
        textOf(c.body, 'plate'),
        numberOrNull(c.body, 'odometerKm') ?? 0,
        textOf(c.body, 'readAt')
      )
    )
  ),
  route('POST', '/api/v1/maintenance/time-evaluations', (c) =>
    okResponse(c.state.reevaluate(textOrNull(c.body, 'asOfDate')))
  ),
  route('POST', '/api/v1/maintenance/reevaluations', (c) =>
    okResponse(c.state.reevaluate(textOrNull(c.body, 'asOfDate')))
  ),
];

const CATALOG_ROUTES: readonly MockRoute[] = [
  route('GET', '/api/v1/maintenance/routine-types', (c) => okResponse(c.state.listRoutineTypes())),
  route('PUT', `/api/v1/maintenance/routine-types/${ID}`, (c) => {
    c.state.updateRoutineType(
      c.captures[0]!,
      {
        intervalKm: numberOrNull(c.body, 'intervalKm'),
        intervalMonths: numberOrNull(c.body, 'intervalMonths'),
        dueSoonPercent: numberOrNull(c.body, 'dueSoonThresholdPercent') ?? 0,
      },
      textOf(c.body, 'updatedBy')
    );
    return noContentResponse();
  }),
];

const ALERT_ROUTES: readonly MockRoute[] = [
  route('GET', '/api/v1/maintenance/alerts', (c) => okResponse(c.state.searchAlerts(alertQuery(c.params)))),
  route('POST', `/api/v1/maintenance/alerts/${ID}/acknowledge`, (c) => {
    c.state.acknowledgeAlert(c.captures[0]!, textOf(c.body, 'acknowledgedBy'));
    return noContentResponse();
  }),
];

const UNAVAILABLE_ROUTES: readonly MockRoute[] = [
  { method: '*', pattern: /^\/api\/v1\/service-orders(\/.*)?$/, handle: notImplemented },
];

export const MOCK_ROUTES: readonly MockRoute[] = [
  ...CUSTOMER_ROUTES,
  ...FLEET_ROUTES,
  ...PLAN_ROUTES,
  ...TELEMETRY_ROUTES,
  ...CATALOG_ROUTES,
  ...ALERT_ROUTES,
  ...UNAVAILABLE_ROUTES,
];
