import {
  AlertStatus,
  ImportEntityType,
  Profile,
  ReportCode,
  ReportFormat,
  VehicleStatus,
  WorkshopStatus,
} from '../models/enums';

export type ServiceOrderStatusGroup = 'PendingApproval' | 'Open' | 'Closed';

export interface ServiceOrdersQuery {
  readonly customerId?: string;
  readonly vehicleId?: string;
  readonly routineId?: string;
  readonly statusGroup?: ServiceOrderStatusGroup;
}

export interface ServiceOrderDetailQuery {
  readonly actor?: Profile;
  readonly actorId?: string;
}

export interface AlertsQuery {
  readonly customerId?: string;
  readonly status?: AlertStatus;
}

export interface VehiclesQuery {
  readonly customerId?: string;
  readonly status?: VehicleStatus;
}

export interface ReportFilterQuery {
  readonly customerId?: string;
  readonly status?: string;
  readonly page?: number;
  readonly pageSize?: number;
}

function buildQuery(params: Readonly<Record<string, string | number | boolean | undefined>>): string {
  const entries: [string, string][] = Object.entries(params)
    .filter((entry): entry is [string, string | number | boolean] => {
      const value: string | number | boolean | undefined = entry[1];
      return value !== undefined && value !== '';
    })
    .map(([key, value]) => [key, String(value)]);

  if (entries.length === 0) {
    return '';
  }

  const search: string = new URLSearchParams(entries).toString();
  return `?${search}`;
}

const API_V1 = '/api/v1';

export const ApiRoutes = {
  customers: {
    list: (): string => `${API_V1}/customers`,
    byId: (id: string): string => `${API_V1}/customers/${encodeURIComponent(id)}`,
    status: (id: string): string => `${API_V1}/customers/${encodeURIComponent(id)}/status`,
    approvers: (id: string): string => `${API_V1}/customers/${encodeURIComponent(id)}/approvers`,
    approverStatus: (id: string, approverId: string): string =>
      `${API_V1}/customers/${encodeURIComponent(id)}/approvers/${encodeURIComponent(approverId)}/status`,
    bulk: (dryRun?: boolean): string => `${API_V1}/customers/bulk${buildQuery({ dryRun })}`,
    approversBulk: (dryRun?: boolean): string =>
      `${API_V1}/customers/approvers/bulk${buildQuery({ dryRun })}`,
  },
  workshops: {
    list: (status?: WorkshopStatus): string => `${API_V1}/workshops${buildQuery({ status })}`,
    byId: (id: string): string => `${API_V1}/workshops/${encodeURIComponent(id)}`,
    status: (id: string): string => `${API_V1}/workshops/${encodeURIComponent(id)}/status`,
    bulk: (dryRun?: boolean): string => `${API_V1}/workshops/bulk${buildQuery({ dryRun })}`,
  },
  fleet: {
    vehicles: (query?: VehiclesQuery): string =>
      `${API_V1}/fleet/vehicles${buildQuery({
        customerId: query?.customerId,
        status: query?.status,
      })}`,
    vehicleById: (id: string): string => `${API_V1}/fleet/vehicles/${encodeURIComponent(id)}`,
    vehicleByPlate: (plate: string): string =>
      `${API_V1}/fleet/vehicles/by-plate/${encodeURIComponent(plate)}`,
    vehicleStatus: (id: string): string =>
      `${API_V1}/fleet/vehicles/${encodeURIComponent(id)}/status`,
    vehicleResponsible: (id: string): string =>
      `${API_V1}/fleet/vehicles/${encodeURIComponent(id)}/responsible`,
    bulk: (dryRun?: boolean): string => `${API_V1}/fleet/vehicles/bulk${buildQuery({ dryRun })}`,
  },
  maintenance: {
    odometerReadings: (): string => `${API_V1}/maintenance/odometer-readings`,
    timeEvaluations: (): string => `${API_V1}/maintenance/time-evaluations`,
    reevaluations: (): string => `${API_V1}/maintenance/reevaluations`,
    plans: (customerId?: string): string =>
      `${API_V1}/maintenance/plans${buildQuery({ customerId })}`,
    planByVehicle: (vehicleId: string): string =>
      `${API_V1}/maintenance/plans/${encodeURIComponent(vehicleId)}`,
    vehicleOdometerReadings: (vehicleId: string): string =>
      `${API_V1}/maintenance/plans/${encodeURIComponent(vehicleId)}/odometer-readings`,
    routineById: (vehicleId: string, routineId: string): string =>
      `${API_V1}/maintenance/plans/${encodeURIComponent(vehicleId)}/routines/${encodeURIComponent(routineId)}`,
    routineState: (vehicleId: string, routineId: string): string =>
      `${API_V1}/maintenance/plans/${encodeURIComponent(vehicleId)}/routines/${encodeURIComponent(routineId)}/state`,
    routinesBulk: (dryRun?: boolean): string =>
      `${API_V1}/maintenance/routines/bulk${buildQuery({ dryRun })}`,
    routineTypes: (): string => `${API_V1}/maintenance/routine-types`,
    routineTypeById: (id: string): string =>
      `${API_V1}/maintenance/routine-types/${encodeURIComponent(id)}`,
    alerts: (query?: AlertsQuery): string =>
      `${API_V1}/maintenance/alerts${buildQuery({
        customerId: query?.customerId,
        status: query?.status,
      })}`,
    acknowledgeAlert: (id: string): string =>
      `${API_V1}/maintenance/alerts/${encodeURIComponent(id)}/acknowledge`,
  },
  serviceOrders: {
    list: (query?: ServiceOrdersQuery): string =>
      `${API_V1}/service-orders${buildQuery({
        customerId: query?.customerId,
        vehicleId: query?.vehicleId,
        routineId: query?.routineId,
        statusGroup: query?.statusGroup,
      })}`,
    create: (): string => `${API_V1}/service-orders`,
    byId: (id: string, query?: ServiceOrderDetailQuery): string =>
      `${API_V1}/service-orders/${encodeURIComponent(id)}${buildQuery({
        actor: query?.actor,
        actorId: query?.actorId,
      })}`,
    approvals: (customerId?: string, approverId?: string): string =>
      `${API_V1}/service-orders/approvals${buildQuery({ customerId, approverId })}`,
    quote: (id: string): string => `${API_V1}/service-orders/${encodeURIComponent(id)}/quote`,
    requestAdditionalService: (id: string): string =>
      `${API_V1}/service-orders/${encodeURIComponent(id)}/additional-services/requests`,
    priceAdditionalService: (id: string, lineId: string): string =>
      `${API_V1}/service-orders/${encodeURIComponent(id)}/additional-services/${encodeURIComponent(lineId)}/price`,
    addAdditionalService: (id: string): string =>
      `${API_V1}/service-orders/${encodeURIComponent(id)}/additional-services`,
    appointment: (id: string): string =>
      `${API_V1}/service-orders/${encodeURIComponent(id)}/appointment`,
    onSite: (id: string): string => `${API_V1}/service-orders/${encodeURIComponent(id)}/on-site`,
    start: (id: string): string => `${API_V1}/service-orders/${encodeURIComponent(id)}/start`,
    complete: (id: string): string => `${API_V1}/service-orders/${encodeURIComponent(id)}/complete`,
    approve: (id: string): string => `${API_V1}/service-orders/${encodeURIComponent(id)}/approve`,
    reject: (id: string): string => `${API_V1}/service-orders/${encodeURIComponent(id)}/reject`,
    cancel: (id: string): string => `${API_V1}/service-orders/${encodeURIComponent(id)}/cancel`,
    approvalPolicies: (): string => `${API_V1}/service-orders/approval-policies`,
  },
  imports: {
    create: (): string => `${API_V1}/imports`,
    byId: (id: string): string => `${API_V1}/imports/${encodeURIComponent(id)}`,
    confirm: (id: string): string => `${API_V1}/imports/${encodeURIComponent(id)}/confirm`,
    errorsCsv: (id: string, entity?: ImportEntityType): string =>
      `${API_V1}/imports/${encodeURIComponent(id)}/errors.csv${buildQuery({ entity })}`,
    templateCsv: (entity: ImportEntityType): string =>
      `${API_V1}/imports/templates/${encodeURIComponent(entity)}.csv`,
  },
  reports: {
    catalog: (): string => `${API_V1}/reports`,
    preview: (code: ReportCode, query?: ReportFilterQuery): string =>
      `${API_V1}/reports/${encodeURIComponent(code)}${buildQuery({
        customerId: query?.customerId,
        status: query?.status,
        page: query?.page,
        pageSize: query?.pageSize,
      })}`,
    download: (code: ReportCode, format: ReportFormat, query?: ReportFilterQuery): string =>
      `${API_V1}/reports/${encodeURIComponent(code)}/download${buildQuery({
        format: format.toLowerCase(),
        customerId: query?.customerId,
        status: query?.status,
      })}`,
  },
} as const;

