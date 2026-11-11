import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AlertStatus, Profile, RoutineState, VehicleStatus } from '../models/enums';
import { MockBackendState } from './mock-backend.state';

const HTTP_OK = 200;
const HTTP_CREATED = 201;

function jsonResponse<T>(body: T, status: number = HTTP_OK): Observable<HttpResponse<T>> {
  return of(new HttpResponse<T>({ status, body }));
}

function asRecord(body: unknown): Record<string, unknown> {
  if (typeof body === 'object' && body !== null) {
    return body as Record<string, unknown>;
  }
  return {};
}

export const mockBackendInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.useMockBackend) {
    return next(req);
  }

  const state: MockBackendState = inject(MockBackendState);
  const parsedUrl: URL = new URL(req.url, 'http://localhost');
  const path: string = parsedUrl.pathname;
  const params: URLSearchParams = parsedUrl.searchParams;
  const method: string = req.method.toUpperCase();
  const body: Record<string, unknown> = asRecord(req.body);

  if (method === 'GET' && path === '/api/v1/customers') {
    return jsonResponse(state.customers);
  }

  const customerByIdMatch: RegExpExecArray | null = /^\/api\/v1\/customers\/([^/]+)$/.exec(path);
  if (method === 'GET' && customerByIdMatch?.[1]) {
    const customerId: string = decodeURIComponent(customerByIdMatch[1]);
    const customer = state.customers.find((c) => c.id === customerId) ?? null;
    return jsonResponse(customer);
  }

  if (method === 'GET' && path === '/api/v1/workshops') {
    const statusFilter: string | null = params.get('status');
    const workshops = statusFilter
      ? state.workshops.filter((w) => w.status === statusFilter)
      : state.workshops;
    return jsonResponse(workshops);
  }

  if (method === 'GET' && path === '/api/v1/fleet/vehicles') {
    const customerId: string | null = params.get('customerId');
    const status: VehicleStatus | null = (params.get('status') as VehicleStatus | null) ?? null;
    return jsonResponse(state.getVehicles(customerId, status));
  }

  const vehicleByPlateMatch: RegExpExecArray | null =
    /^\/api\/v1\/fleet\/vehicles\/by-plate\/([^/]+)$/.exec(path);
  if (method === 'GET' && vehicleByPlateMatch?.[1]) {
    const plate: string = decodeURIComponent(vehicleByPlateMatch[1]);
    return jsonResponse(state.getVehicleByIdOrPlate(plate));
  }

  const vehicleStatusMatch: RegExpExecArray | null =
    /^\/api\/v1\/fleet\/vehicles\/([^/]+)\/status$/.exec(path);
  if (method === 'PATCH' && vehicleStatusMatch?.[1]) {
    const id: string = decodeURIComponent(vehicleStatusMatch[1]);
    const nextStatus: VehicleStatus =
      body['status'] === 'Inactive' ? 'Inactive' : 'Active';
    return jsonResponse(state.updateVehicleStatus(id, nextStatus));
  }

  const vehicleByIdMatch: RegExpExecArray | null = /^\/api\/v1\/fleet\/vehicles\/([^/]+)$/.exec(
    path
  );
  if (method === 'GET' && vehicleByIdMatch?.[1]) {
    const id: string = decodeURIComponent(vehicleByIdMatch[1]);
    return jsonResponse(state.getVehicleByIdOrPlate(id));
  }

  if (method === 'GET' && path === '/api/v1/maintenance/plans') {
    const customerId: string | null = params.get('customerId');
    return jsonResponse(state.getPlanSummaries(customerId));
  }

  const planReadingsMatch: RegExpExecArray | null =
    /^\/api\/v1\/maintenance\/plans\/([^/]+)\/odometer-readings$/.exec(path);
  if (method === 'GET' && planReadingsMatch?.[1]) {
    const vehicleId: string = decodeURIComponent(planReadingsMatch[1]);
    return jsonResponse(state.getReadings(vehicleId));
  }

  const routineStateMatch: RegExpExecArray | null =
    /^\/api\/v1\/maintenance\/plans\/([^/]+)\/routines\/([^/]+)\/state$/.exec(path);
  if (method === 'PATCH' && routineStateMatch?.[1] && routineStateMatch[2]) {
    const vehicleId: string = decodeURIComponent(routineStateMatch[1]);
    const routineId: string = decodeURIComponent(routineStateMatch[2]);
    const nextState: RoutineState = body['state'] === 'Inactive' ? 'Inactive' : 'Active';
    return jsonResponse(state.updateRoutineState(vehicleId, routineId, nextState));
  }

  const routineUpdateMatch: RegExpExecArray | null =
    /^\/api\/v1\/maintenance\/plans\/([^/]+)\/routines\/([^/]+)$/.exec(path);
  if (method === 'PUT' && routineUpdateMatch?.[1] && routineUpdateMatch[2]) {
    const vehicleId: string = decodeURIComponent(routineUpdateMatch[1]);
    const routineId: string = decodeURIComponent(routineUpdateMatch[2]);
    return jsonResponse(
      state.updateRoutineParameters(vehicleId, routineId, {
        intervalKm: typeof body['intervalKm'] === 'number' ? body['intervalKm'] : null,
        intervalMonths:
          typeof body['intervalMonths'] === 'number' ? body['intervalMonths'] : null,
        dueSoonThresholdPercent:
          typeof body['dueSoonThresholdPercent'] === 'number'
            ? body['dueSoonThresholdPercent']
            : null,
      })
    );
  }

  const planByVehicleMatch: RegExpExecArray | null =
    /^\/api\/v1\/maintenance\/plans\/([^/]+)$/.exec(path);
  if (method === 'GET' && planByVehicleMatch?.[1]) {
    const vehicleId: string = decodeURIComponent(planByVehicleMatch[1]);
    return jsonResponse(state.getPlanByVehicle(vehicleId));
  }

  if (method === 'POST' && path === '/api/v1/maintenance/odometer-readings') {
    const plate: string = String(body['plate'] ?? '');
    const kilometers: number = Number(body['kilometers'] ?? 0);
    const readAt: string = String(body['readAt'] ?? '12/11/2026 09:00');
    return jsonResponse(state.recordOdometerReading(plate, kilometers, readAt));
  }

  if (
    method === 'POST' &&
    (path === '/api/v1/maintenance/time-evaluations' ||
      path === '/api/v1/maintenance/reevaluations')
  ) {
    const asOfDate: string | undefined =
      typeof body['asOfDate'] === 'string' ? body['asOfDate'] : undefined;
    return jsonResponse(state.runTimeEvaluation(asOfDate));
  }

  if (method === 'GET' && path === '/api/v1/maintenance/routine-types') {
    return jsonResponse(state.routineTypes);
  }

  const routineTypeByIdMatch: RegExpExecArray | null =
    /^\/api\/v1\/maintenance\/routine-types\/([^/]+)$/.exec(path);
  if (method === 'PUT' && routineTypeByIdMatch?.[1]) {
    const id: string = decodeURIComponent(routineTypeByIdMatch[1]);
    return jsonResponse(
      state.updateRoutineType(id, {
        defaultIntervalKm:
          typeof body['defaultIntervalKm'] === 'number' ? body['defaultIntervalKm'] : null,
        defaultIntervalMonths:
          typeof body['defaultIntervalMonths'] === 'number'
            ? body['defaultIntervalMonths']
            : null,
        dueSoonThresholdPercent: Number(body['dueSoonThresholdPercent'] ?? 10),
      })
    );
  }

  if (method === 'GET' && path === '/api/v1/maintenance/alerts') {
    const customerId: string | null = params.get('customerId');
    const status: AlertStatus | null = (params.get('status') as AlertStatus | null) ?? null;
    return jsonResponse(state.getAlerts(customerId, status));
  }

  const ackAlertMatch: RegExpExecArray | null =
    /^\/api\/v1\/maintenance\/alerts\/([^/]+)\/acknowledge$/.exec(path);
  if (method === 'POST' && ackAlertMatch?.[1]) {
    const alertId: string = decodeURIComponent(ackAlertMatch[1]);
    const acknowledgedBy: string = String(body['acknowledgedBy'] ?? 'Operador');
    return jsonResponse(state.acknowledgeAlert(alertId, acknowledgedBy));
  }

  if (method === 'GET' && path === '/api/v1/service-orders/approval-policies') {
    return jsonResponse(state.getApprovalPolicy());
  }

  if (method === 'POST' && path === '/api/v1/service-orders/approval-policies') {
    const amount: number = Number(body['autoApprovalMaxAmount'] ?? 0);
    const createdBy: string = String(body['createdBy'] ?? 'Operador');
    return jsonResponse(state.addApprovalPolicy(amount, createdBy), HTTP_CREATED);
  }

  if (method === 'GET' && path === '/api/v1/service-orders') {
    return jsonResponse(
      state.getServiceOrders({
        customerId: params.get('customerId'),
        vehicleId: params.get('vehicleId'),
        routineId: params.get('routineId'),
        statusGroup: params.get('statusGroup'),
      })
    );
  }

  if (method === 'POST' && path === '/api/v1/service-orders') {
    const vehicleId: string = String(body['vehicleId'] ?? body['plate'] ?? '');
    const routineId: string = String(body['routineId'] ?? '');
    return jsonResponse(state.createManualOrder(vehicleId, routineId), HTTP_CREATED);
  }

  const priceLineMatch: RegExpExecArray | null =
    /^\/api\/v1\/service-orders\/([^/]+)\/additional-services\/([^/]+)\/price$/.exec(path);
  if (method === 'POST' && priceLineMatch?.[1] && priceLineMatch[2]) {
    const id: string = decodeURIComponent(priceLineMatch[1]);
    const lineId: string = decodeURIComponent(priceLineMatch[2]);
    return jsonResponse(
      state.performOrderAction(id, 'priceAdditional', { ...body, lineId })
    );
  }

  const requestAdditionalMatch: RegExpExecArray | null =
    /^\/api\/v1\/service-orders\/([^/]+)\/additional-services\/requests$/.exec(path);
  if (method === 'POST' && requestAdditionalMatch?.[1]) {
    const id: string = decodeURIComponent(requestAdditionalMatch[1]);
    return jsonResponse(state.performOrderAction(id, 'requestAdditional', body));
  }

  const addAdditionalMatch: RegExpExecArray | null =
    /^\/api\/v1\/service-orders\/([^/]+)\/additional-services$/.exec(path);
  if (method === 'POST' && addAdditionalMatch?.[1]) {
    const id: string = decodeURIComponent(addAdditionalMatch[1]);
    return jsonResponse(state.performOrderAction(id, 'addAdditional', body));
  }

  const appointmentMatch: RegExpExecArray | null =
    /^\/api\/v1\/service-orders\/([^/]+)\/appointment$/.exec(path);
  if (method === 'PUT' && appointmentMatch?.[1]) {
    const id: string = decodeURIComponent(appointmentMatch[1]);
    return jsonResponse(state.performOrderAction(id, 'appointment', body));
  }

  const orderActionMatch: RegExpExecArray | null =
    /^\/api\/v1\/service-orders\/([^/]+)\/(quote|on-site|start|complete|approve|reject|cancel)$/.exec(
      path
    );
  if (method === 'POST' && orderActionMatch?.[1] && orderActionMatch[2]) {
    const id: string = decodeURIComponent(orderActionMatch[1]);
    const rawAct: string = orderActionMatch[2];
    const mappedAction =
      rawAct === 'on-site'
        ? 'onSite'
        : (rawAct as 'quote' | 'start' | 'complete' | 'approve' | 'reject' | 'cancel');
    return jsonResponse(state.performOrderAction(id, mappedAction, body));
  }

  const orderByIdMatch: RegExpExecArray | null = /^\/api\/v1\/service-orders\/([^/]+)$/.exec(
    path
  );
  if (method === 'GET' && orderByIdMatch?.[1]) {
    const id: string = decodeURIComponent(orderByIdMatch[1]);
    const actor: Profile = (params.get('actor') as Profile | null) ?? 'Operator';
    return jsonResponse(state.getServiceOrderDetail(id, actor));
  }

  return next(req);
};

