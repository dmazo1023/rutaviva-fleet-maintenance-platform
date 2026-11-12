import { HttpClient, HttpErrorResponse, HttpResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { lastValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { HUB_CONNECTION_FACTORY } from '../realtime/notifications.service';
import {
  AlertDto,
  CustomerDetailDto,
  CustomerSummaryDto,
  PagedResultDto,
  PlanDetailDto,
  PlanSummaryDto,
  RoutineDetailDto,
  RoutineTypeDto,
  TelemetryResponseDto,
  VehicleDetailDto,
  VehicleSummaryDto,
} from './mock-backend.dtos';
import { GUID_PATTERN } from './mock-backend.ids';
import { mockBackendInterceptor } from './mock-backend.interceptor';

const VEHICLES_URL = '/api/v1/fleet/vehicles';
const PLANS_URL = '/api/v1/maintenance/plans';
const ALERTS_URL = '/api/v1/maintenance/alerts';
const ROUTINE_TYPES_URL = '/api/v1/maintenance/routine-types';
const OVERDUE_PLATE = 'RVA104';
const SEEDED_VEHICLES = 10;
const SEEDED_ROUTINES = 15;
const SEEDED_CUSTOMERS = 3;
const SEEDED_WORKSHOPS = 3;
const HTTP_NO_CONTENT = 204;
const HTTP_ACCEPTED = 202;
const HTTP_BAD_REQUEST = 400;
const HTTP_NOT_FOUND = 404;
const HTTP_UNPROCESSABLE = 422;
const UNKNOWN_GUID = '00000000-0000-4000-8000-00000000ffff';
const ACTOR = 'Laura Gómez';

describe('mockBackendInterceptor', () => {
  let http: HttpClient;
  let originalFlag: boolean;

  beforeEach(() => {
    originalFlag = environment.useMockBackend;
    Object.assign(environment, { useMockBackend: true });
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([mockBackendInterceptor])),
        provideHttpClientTesting(),
        { provide: HUB_CONNECTION_FACTORY, useValue: () => null },
      ],
    });
    http = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    Object.assign(environment, { useMockBackend: originalFlag });
  });

  function get<T>(url: string): Promise<T> {
    return lastValueFrom(http.get<T>(url));
  }

  function send(method: string, url: string, body: unknown): Promise<HttpResponse<unknown>> {
    return lastValueFrom(http.request(method, url, { body, observe: 'response' }));
  }

  async function failure(call: Promise<unknown>): Promise<HttpErrorResponse> {
    try {
      await call;
    } catch (error: unknown) {
      return error as HttpErrorResponse;
    }
    throw new Error('Expected the request to fail');
  }

  async function vehicleIdOf(plate: string): Promise<string> {
    const page: PagedResultDto<VehicleSummaryDto> = await get(VEHICLES_URL);
    return page.items.find((v: VehicleSummaryDto) => v.plate === plate)!.id;
  }

  async function overdueRoutine(): Promise<{ vehicleId: string; routine: RoutineDetailDto }> {
    const vehicleId: string = await vehicleIdOf(OVERDUE_PLATE);
    const plan: PlanDetailDto = await get(`${PLANS_URL}/${vehicleId}`);
    return { vehicleId, routine: plan.routines.find((r: RoutineDetailDto) => r.dueStatus === 'Overdue')! };
  }

  it('should return vehicles as a paged result with guid ids and no odometer', async () => {
    const page: PagedResultDto<VehicleSummaryDto> = await get(VEHICLES_URL);

    expect(page.totalCount).toBe(SEEDED_VEHICLES);
    expect(page.page).toBe(1);
    expect(page.pageSize).toBe(50);
    expect(page.items.every((v: VehicleSummaryDto) => GUID_PATTERN.test(v.id))).toBe(true);
    expect(page.items[0]!.customerName).toBe('RutaViva S.A.S.');
    expect('odometerKm' in page.items[0]!).toBe(false);
  });

  it('should filter vehicles by customer and search text', async () => {
    const customers: CustomerSummaryDto[] = await get('/api/v1/customers');
    const andinos: CustomerSummaryDto = customers.find((c: CustomerSummaryDto) => c.name.startsWith('Transportes'))!;

    const page: PagedResultDto<VehicleSummaryDto> = await get(
      `${VEHICLES_URL}?customerId=${andinos.id}&search=aumark`
    );

    expect(page.items.map((v: VehicleSummaryDto) => v.plate)).toEqual(['TAN201']);
  });

  it('should reject a vehicle page size above the maximum with a problem details body', async () => {
    const error: HttpErrorResponse = await failure(get(`${VEHICLES_URL}?pageSize=201`));

    expect(error.status).toBe(HTTP_BAD_REQUEST);
    expect(error.error.code).toBe('Vehicle.InvalidPageSize');
  });

  it('should return a nested vehicle detail by id and by plate', async () => {
    const id: string = await vehicleIdOf('RVA101');

    const byId: VehicleDetailDto = await get(`${VEHICLES_URL}/${id}`);
    const byPlate: VehicleDetailDto = await get(`${VEHICLES_URL}/by-plate/rva101`);

    expect(byPlate).toEqual(byId);
    expect(byId.owner.taxId).toBe('900000001');
    expect(byId.workshop.name).toBe('Autofrenos Medellín');
    expect(byId.responsible?.name).toBe('Laura Gómez');
    expect(byId.initialOdometerKm).toBe(52_300);
  });

  it('should answer 404 for an unknown plate and an unknown vehicle id', async () => {
    const byPlate: HttpErrorResponse = await failure(get(`${VEHICLES_URL}/by-plate/ZZZ999`));
    const byId: HttpErrorResponse = await failure(get(`${VEHICLES_URL}/${UNKNOWN_GUID}`));

    expect(byPlate.status).toBe(HTTP_NOT_FOUND);
    expect(byId.status).toBe(HTTP_NOT_FOUND);
  });

  it('should answer 204 without body when changing the vehicle status', async () => {
    const id: string = await vehicleIdOf('RVA101');

    const response: HttpResponse<unknown> = await send('PATCH', `${VEHICLES_URL}/${id}/status`, { status: 'Inactive' });
    const detail: VehicleDetailDto = await get(`${VEHICLES_URL}/${id}`);

    expect(response.status).toBe(HTTP_NO_CONTENT);
    expect(response.body).toBeNull();
    expect(detail.status).toBe('Inactive');
  });

  it('should list customers without approvers and return approvers in the detail', async () => {
    const customers: CustomerSummaryDto[] = await get('/api/v1/customers');
    const detail: CustomerDetailDto = await get(`/api/v1/customers/${customers[0]!.id}`);

    expect(customers).toHaveLength(SEEDED_CUSTOMERS);
    expect('approvers' in customers[0]!).toBe(false);
    expect(detail.approvers).toHaveLength(2);
    expect(GUID_PATTERN.test(detail.approvers[0]!.id)).toBe(true);
  });

  it('should list the seeded workshops', async () => {
    const workshops: unknown[] = await get('/api/v1/workshops?status=Active');

    expect(workshops).toHaveLength(SEEDED_WORKSHOPS);
  });

  it('should return plan summaries with the real field names', async () => {
    const plans: PlanSummaryDto[] = await get(PLANS_URL);
    const overdue: PlanSummaryDto = plans.find((p: PlanSummaryDto) => p.plate === OVERDUE_PLATE)!;

    expect(plans).toHaveLength(SEEDED_VEHICLES);
    expect(overdue.planStatus).toBe('Active');
    expect(overdue.worstDueStatus).toBe('Overdue');
    expect(overdue.maxConsumedPercent).toBeGreaterThan(100);
    expect(overdue.overdueCount + overdue.dueSoonCount + overdue.upToDateCount).toBe(2);
    expect('status' in overdue).toBe(false);
  });

  it('should return the plan detail with tracks, type code and parameter sources', async () => {
    const { routine } = await overdueRoutine();

    expect(routine.routineTypeCode).toBe('BRAKES');
    expect(routine.distanceTrack?.kind).toBe('km');
    expect(routine.timeTrack?.kind).toBe('time');
    expect(routine.intervalKmSource).toBe('RoutineType');
    expect(routine.triggeredBy).not.toBeNull();
    expect('tracks' in routine).toBe(false);
  });

  it('should expose every seeded routine across the plans', async () => {
    const plans: PlanSummaryDto[] = await get(PLANS_URL);
    const details: PlanDetailDto[] = await Promise.all(
      plans.map((p: PlanSummaryDto) => get<PlanDetailDto>(`${PLANS_URL}/${p.vehicleId}`))
    );

    expect(details.reduce((sum: number, d: PlanDetailDto) => sum + d.routines.length, 0)).toBe(SEEDED_ROUTINES);
  });

  it('should answer 404 with problem details when the plan does not exist', async () => {
    const error: HttpErrorResponse = await failure(get(`${PLANS_URL}/${UNKNOWN_GUID}`));

    expect(error.status).toBe(HTTP_NOT_FOUND);
    expect(error.error.code).toBe('MaintenancePlan.NotFound');
    expect(error.error.title).toBe('NotFound');
  });

  it('should return alerts as a paged result and acknowledge with 204', async () => {
    const page: PagedResultDto<AlertDto> = await get(`${ALERTS_URL}?status=Active`);
    const target: AlertDto = page.items[0]!;

    const response: HttpResponse<unknown> = await send('POST', `${ALERTS_URL}/${target.alertId}/acknowledge`, {
      acknowledgedBy: ACTOR,
    });
    const acknowledged: PagedResultDto<AlertDto> = await get(`${ALERTS_URL}?status=Acknowledged`);

    expect(page.pageSize).toBe(20);
    expect(page.totalCount).toBeGreaterThan(0);
    expect(response.status).toBe(HTTP_NO_CONTENT);
    expect(acknowledged.items.some((a: AlertDto) => a.alertId === target.alertId)).toBe(true);
  });

  it('should reject a second acknowledgement and a missing actor', async () => {
    const page: PagedResultDto<AlertDto> = await get(`${ALERTS_URL}?status=Active`);
    const url = `${ALERTS_URL}/${page.items[0]!.alertId}/acknowledge`;
    const missing: HttpErrorResponse = await failure(send('POST', url, {}));
    await send('POST', url, { acknowledgedBy: ACTOR });

    const repeated: HttpErrorResponse = await failure(send('POST', url, { acknowledgedBy: ACTOR }));

    expect(missing.error.code).toBe('Alert.AcknowledgedByRequired');
    expect(repeated.status).toBe(HTTP_UNPROCESSABLE);
  });

  it('should reject alert page sizes above 100', async () => {
    const error: HttpErrorResponse = await failure(get(`${ALERTS_URL}?pageSize=101`));

    expect(error.status).toBe(HTTP_BAD_REQUEST);
  });

  it('should require updatedBy on routine parameters and answer 204 when present', async () => {
    const { vehicleId, routine } = await overdueRoutine();
    const url = `${PLANS_URL}/${vehicleId}/routines/${routine.routineId}`;

    const rejected: HttpErrorResponse = await failure(send('PUT', url, { intervalKm: 30_000 }));
    const accepted: HttpResponse<unknown> = await send('PUT', url, {
      intervalKm: 30_000,
      intervalMonths: null,
      dueSoonThresholdPercent: 15,
      updatedBy: ACTOR,
    });
    const plan: PlanDetailDto = await get(`${PLANS_URL}/${vehicleId}`);
    const updated: RoutineDetailDto = plan.routines.find((r: RoutineDetailDto) => r.routineId === routine.routineId)!;

    expect(rejected.status).toBe(HTTP_BAD_REQUEST);
    expect(rejected.error.code).toBe('Audit.UpdatedByRequired');
    expect(accepted.status).toBe(HTTP_NO_CONTENT);
    expect(updated.intervalKm).toBe(30_000);
    expect(updated.intervalKmSource).toBe('Override');
    expect(updated.dueSoonPercent).toBe(15);
  });

  it('should deactivate a routine with 204 and require updatedBy', async () => {
    const { vehicleId, routine } = await overdueRoutine();
    const url = `${PLANS_URL}/${vehicleId}/routines/${routine.routineId}/state`;

    const rejected: HttpErrorResponse = await failure(send('PATCH', url, { state: 'Inactive' }));
    const accepted: HttpResponse<unknown> = await send('PATCH', url, { state: 'Inactive', updatedBy: ACTOR });
    const plan: PlanDetailDto = await get(`${PLANS_URL}/${vehicleId}`);

    expect(rejected.error.code).toBe('Audit.UpdatedByRequired');
    expect(accepted.status).toBe(HTTP_NO_CONTENT);
    expect(plan.inactiveCount).toBe(1);
  });

  it('should restart a routine cycle when a service is registered and resolve its alerts', async () => {
    const { vehicleId, routine } = await overdueRoutine();
    const plan: PlanDetailDto = await get(`${PLANS_URL}/${vehicleId}`);

    const response: HttpResponse<unknown> = await send(
      'POST',
      `${PLANS_URL}/${vehicleId}/routines/${routine.routineId}/services`,
      { servicedKm: plan.odometerKm, servicedAt: '2026-11-12T00:00:00-05:00' }
    );
    const after: PlanDetailDto = await get(`${PLANS_URL}/${vehicleId}`);
    const restarted: RoutineDetailDto = after.routines.find((r: RoutineDetailDto) => r.routineId === routine.routineId)!;
    const open: PagedResultDto<AlertDto> = await get(`${ALERTS_URL}?vehicleId=${vehicleId}&status=Active&pageSize=100`);
    const resolved: PagedResultDto<AlertDto> = await get(`${ALERTS_URL}?vehicleId=${vehicleId}&status=Resolved`);

    expect(response.status).toBe(HTTP_NO_CONTENT);
    expect(restarted.dueStatus).toBe('UpToDate');
    expect(restarted.consumedPercent).toBe(0);
    expect(restarted.cycleNumber).toBe(routine.cycleNumber + 1);
    expect(open.items.some((a: AlertDto) => a.routineId === routine.routineId)).toBe(false);
    expect(resolved.items.some((a: AlertDto) => a.resolutionReason === 'RoutineRestarted')).toBe(true);
  });

  it('should validate the service body and unknown plans or routines', async () => {
    const { vehicleId, routine } = await overdueRoutine();

    const missing: HttpErrorResponse = await failure(
      send('POST', `${PLANS_URL}/${vehicleId}/routines/${routine.routineId}/services`, {})
    );
    const noPlan: HttpErrorResponse = await failure(
      send('POST', `${PLANS_URL}/${UNKNOWN_GUID}/routines/${routine.routineId}/services`, {
        servicedKm: 1,
        servicedAt: '2026-11-12T00:00:00-05:00',
      })
    );
    const noRoutine: HttpErrorResponse = await failure(
      send('POST', `${PLANS_URL}/${vehicleId}/routines/${UNKNOWN_GUID}/services`, {
        servicedKm: 1,
        servicedAt: '2026-11-12T00:00:00-05:00',
      })
    );

    expect(missing.error.code).toBe('Routine.ServiceDataRequired');
    expect(noPlan.error.code).toBe('MaintenancePlan.NotFound');
    expect(noRoutine.error.code).toBe('Routine.NotFound');
  });

  it('should answer telemetry with 202 and the outcome', async () => {
    const accepted: HttpResponse<unknown> = await send('POST', '/api/v1/maintenance/odometer-readings', {
      plate: 'RVA101',
      odometerKm: 60_000,
      readAt: '2026-11-12T09:00:00-05:00',
    });
    const ignored: HttpResponse<unknown> = await send('POST', '/api/v1/maintenance/odometer-readings', {
      plate: 'RVA101',
      odometerKm: 10,
      readAt: '2026-11-12T09:05:00-05:00',
    });
    const unknown: HttpResponse<unknown> = await send('POST', '/api/v1/maintenance/odometer-readings', {
      plate: 'ZZZ999',
      odometerKm: 10,
      readAt: '2026-11-12T09:05:00-05:00',
    });

    expect(accepted.status).toBe(HTTP_ACCEPTED);
    expect(accepted.body).toEqual<TelemetryResponseDto>({ outcome: 'Accepted', reason: null });
    expect(ignored.body).toEqual<TelemetryResponseDto>({ outcome: 'Ignored', reason: 'NotGreaterThanCurrent' });
    expect(unknown.body).toEqual<TelemetryResponseDto>({ outcome: 'Ignored', reason: 'UnknownVehicle' });
  });

  it('should return the time evaluation summary with the real field names', async () => {
    const summary: Record<string, unknown> = await lastValueFrom(
      http.post<Record<string, unknown>>('/api/v1/maintenance/time-evaluations', { asOfDate: '2026-11-12' })
    );

    expect(Object.keys(summary).sort()).toEqual(['dueSoon', 'evaluatedPlans', 'ordersRequested', 'overdue']);
    expect(summary['ordersRequested']).toBe(0);
  });

  it('should list routine types with dueSoonPercent and update them with 204', async () => {
    const types: RoutineTypeDto[] = await get(ROUTINE_TYPES_URL);
    const oil: RoutineTypeDto = types.find((t: RoutineTypeDto) => t.code === 'OIL_CHANGE')!;

    const response: HttpResponse<unknown> = await send('PUT', `${ROUTINE_TYPES_URL}/${oil.id}`, {
      intervalKm: 12_000,
      intervalMonths: 6,
      dueSoonThresholdPercent: 12,
      updatedBy: ACTOR,
    });
    const updated: RoutineTypeDto = (await get<RoutineTypeDto[]>(ROUTINE_TYPES_URL)).find(
      (t: RoutineTypeDto) => t.id === oil.id
    )!;

    expect(oil.dueSoonPercent).toBe(10);
    expect(response.status).toBe(HTTP_NO_CONTENT);
    expect(updated.defaultIntervalKm).toBe(12_000);
    expect(updated.dueSoonPercent).toBe(12);
    expect(updated.updatedBy).toBe(ACTOR);
  });

  it('should require updatedBy when updating a routine type', async () => {
    const types: RoutineTypeDto[] = await get(ROUTINE_TYPES_URL);

    const error: HttpErrorResponse = await failure(
      send('PUT', `${ROUTINE_TYPES_URL}/${types[0]!.id}`, {
        intervalKm: 12_000,
        intervalMonths: 6,
        dueSoonThresholdPercent: 12,
      })
    );

    expect(error.error.code).toBe('Audit.UpdatedByRequired');
  });

  it('should answer 404 for service orders and the approval policy', async () => {
    const orders: HttpErrorResponse = await failure(get('/api/v1/service-orders'));
    const policy: HttpErrorResponse = await failure(get('/api/v1/service-orders/approval-policies'));

    expect(orders.status).toBe(HTTP_NOT_FOUND);
    expect(policy.status).toBe(HTTP_NOT_FOUND);
  });
});
