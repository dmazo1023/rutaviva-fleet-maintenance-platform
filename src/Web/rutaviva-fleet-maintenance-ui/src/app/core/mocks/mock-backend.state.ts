import { Injectable, inject } from '@angular/core';
import {
  AlertResolutionReason,
  AlertStatus,
  AlertType,
  ApprovalPriority,
  ApprovalReason,
  DueStatus,
  MaintenancePlanStatus,
  OdometerReadingIgnoredReason,
  OdometerReadingOutcome,
  PricingStatus,
  Profile,
  RoutineState,
  RoutineTypeStatus,
  ServiceLineApprovalStatus,
  ServiceLineRequester,
  ServiceLineType,
  ServiceOrderAction,
  ServiceOrderBlockedReason,
  ServiceOrderChangeType,
  ServiceOrderStatus,
  TriggeredBy,
  VehicleStatus,
} from '../models/enums';
import { NotificationsService } from '../realtime/notifications.service';

export interface MockApproverDto {
  readonly id: string;
  readonly customerId: string;
  readonly fullName: string;
  readonly email: string;
  readonly status: 'Active' | 'Inactive';
}

export interface MockCustomerDto {
  readonly id: string;
  readonly taxId: string;
  readonly name: string;
  readonly email: string;
  readonly phone: string;
  readonly status: 'Active' | 'Inactive';
  readonly approvers: readonly MockApproverDto[];
}

export interface MockWorkshopDto {
  readonly id: string;
  readonly taxId: string;
  readonly name: string;
  readonly email: string;
  readonly phone: string;
  readonly address: string;
  readonly city: string;
  readonly status: 'Active' | 'Inactive';
}

export interface MockVehicleDto {
  readonly id: string;
  readonly plate: string;
  readonly vin: string | null;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly customerId: string;
  readonly customerName: string;
  readonly workshopId: string;
  readonly workshopName: string;
  readonly responsibleApproverId: string | null;
  readonly responsibleApproverName: string | null;
  status: VehicleStatus;
  odometerKm: number;
  lastReadingText: string;
}

export interface MockRoutineTypeDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly icon: string;
  defaultIntervalKm: number | null;
  defaultIntervalMonths: number | null;
  dueSoonThresholdPercent: number;
  readonly status: RoutineTypeStatus;
}

export interface MockRoutineRecord {
  readonly routineId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly typeCode: string;
  state: RoutineState;
  cycleNumber: number;
  cycleStartKm: number;
  cycleStartDate: string;
  intervalKmOverride: number | null;
  intervalMonthsOverride: number | null;
  dueSoonPercentOverride: number | null;
  stickyOverdue: boolean;
  cycleOrderCreated: boolean;
}

export interface MockTrackDto {
  readonly kind: 'km' | 'time';
  readonly consumedPercent: number;
  readonly dueSoonPercent: number;
  readonly remaining: number;
  readonly status: DueStatus;
}

export interface MockEvaluatedRoutineDto {
  readonly routineId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly name: string;
  readonly typeCode: string;
  readonly icon: string;
  readonly state: RoutineState;
  readonly dueStatus: DueStatus;
  readonly triggeredBy: TriggeredBy | null;
  readonly tracks: readonly MockTrackDto[];
  readonly nextDueKm: number | null;
  readonly nextDueDate: string | null;
  readonly cycleStartKm: number;
  readonly cycleStartDate: string;
  readonly intervalKm: number | null;
  readonly intervalMonths: number | null;
  readonly defaultIntervalKm: number;
  readonly defaultIntervalMonths: number;
  readonly defaultDueSoonPercent: number;
  readonly intervalKmOverride: number | null;
  readonly intervalMonthsOverride: number | null;
  readonly dueSoonPercentOverride: number | null;
  readonly effectiveDueSoonPercent: number;
  readonly thresholdSource: 'Type' | 'Routine';
  readonly openServiceOrderId: string | null;
  readonly openServiceOrderNumber: string | null;
  readonly openServiceOrderStatus: ServiceOrderStatus | null;
  readonly canCreateOrder: boolean;
}

export interface MockPlanSummaryDto {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly status: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly worstDueStatus: DueStatus | null;
  readonly worstConsumedPercent: number;
  readonly worstTriggeredBy: TriggeredBy | null;
  readonly nextRoutineName: string | null;
  readonly nextRemainingKind: 'km' | 'time' | null;
  readonly nextRemainingValue: number | null;
}

export interface MockPlanDetailDto {
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly status: MaintenancePlanStatus;
  readonly odometerKm: number;
  readonly routines: readonly MockEvaluatedRoutineDto[];
}

export interface MockOdometerReadingDto {
  readonly kilometers: number;
  readonly readAt: string;
  readonly outcome: OdometerReadingOutcome;
  readonly ignoredReason: OdometerReadingIgnoredReason | null;
}

export interface MockAlertDto {
  readonly alertId: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly customerId: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode: string;
  readonly cycleNumber: number;
  readonly type: AlertType;
  readonly triggeredBy: TriggeredBy;
  status: AlertStatus;
  readonly raisedAt: string;
  acknowledgedBy: string | null;
  resolutionReason: AlertResolutionReason | null;
}

export interface MockServiceLineDto {
  readonly id: string;
  readonly lineType: ServiceLineType;
  readonly description: string;
  amount: number | null;
  pricingStatus: PricingStatus;
  readonly requestedBy: ServiceLineRequester;
  approvalStatus: ServiceLineApprovalStatus;
}

export interface MockOrderHistoryDto {
  readonly id: string;
  readonly changeType: ServiceOrderChangeType;
  readonly detail: string;
  readonly changedBy: string;
  readonly changedAt: string;
  readonly icon: string;
}

export interface MockServiceOrderRecord {
  readonly id: string;
  readonly number: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly routineId: string;
  readonly routineTypeCode: string;
  readonly routineName: string;
  readonly customerId: string;
  readonly workshopId: string;
  status: ServiceOrderStatus;
  statusBeforeHold: ServiceOrderStatus | null;
  reason: ApprovalReason | null;
  priority: ApprovalPriority | null;
  autoApproved: boolean;
  autoApprovalMaxAmountApplied: number;
  appointmentAt: string | null;
  lines: MockServiceLineDto[];
  history: MockOrderHistoryDto[];
  updatedAt: string;
}

export interface MockServiceOrderActionDto {
  readonly action: ServiceOrderAction;
  readonly enabled: boolean;
  readonly blockedReason: ServiceOrderBlockedReason | null;
}

export interface MockServiceOrderListItemDto {
  readonly id: string;
  readonly number: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode: string;
  readonly customerId: string;
  readonly status: ServiceOrderStatus;
  readonly statusBeforeHold: ServiceOrderStatus | null;
  readonly reason: ApprovalReason | null;
  readonly priority: ApprovalPriority | null;
  readonly total: number | null;
  readonly appointmentAt: string | null;
  readonly updatedAt: string;
}

export interface MockServiceOrderDetailDto {
  readonly id: string;
  readonly number: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode: string;
  readonly routineIcon: string;
  readonly customerId: string;
  readonly workshopId: string;
  readonly workshopName: string;
  readonly status: ServiceOrderStatus;
  readonly statusBeforeHold: ServiceOrderStatus | null;
  readonly reason: ApprovalReason | null;
  readonly priority: ApprovalPriority | null;
  readonly availableActions: readonly MockServiceOrderActionDto[];
  readonly lines: readonly MockServiceLineDto[];
  readonly total: number;
  readonly autoApproved: boolean;
  readonly autoApprovalMaxAmountApplied: number;
  readonly appointmentAt: string | null;
  readonly history: readonly MockOrderHistoryDto[];
  readonly updatedAt: string;
}

export interface MockApprovalPolicyEntryDto {
  readonly id: string;
  readonly currency: string;
  readonly autoApprovalMaxAmount: number;
  readonly effectiveFrom: string;
  readonly createdBy: string;
}

export interface MockApprovalPolicyResponseDto {
  readonly currentAmount: number;
  readonly currency: string;
  readonly history: readonly MockApprovalPolicyEntryDto[];
}

const MS_PER_DAY = 86_400_000;
const FULL_PERCENT = 100;
const INITIAL_POLICY_AMOUNT = 1_500_000;
const OPEN_ORDER_STATUSES: readonly ServiceOrderStatus[] = [
  'Created',
  'Quoted',
  'PendingApproval',
  'Approved',
  'OnSite',
  'InProgress',
  'OnHold',
];
const PRE_PROGRESS_STATUSES: readonly ServiceOrderStatus[] = [
  'Created',
  'PendingApproval',
  'Approved',
  'OnSite',
];

function addMonthsIso(startIso: string, months: number): Date {
  const parts: string[] = startIso.split('-');
  const year: number = Number(parts[0]);
  const month: number = Number(parts[1]) - 1;
  const day: number = Number(parts[2]);
  const date: Date = new Date(year, month, day);
  date.setMonth(date.getMonth() + months);
  if (date.getDate() < day) {
    date.setDate(0);
  }
  return date;
}

function parseIsoDate(iso: string): Date {
  const parts: string[] = iso.slice(0, 10).split('-');
  return new Date(Number(parts[0]), Number(parts[1]) - 1, Number(parts[2]));
}

function formatDateIso(date: Date): string {
  const year: number = date.getFullYear();
  const month: string = String(date.getMonth() + 1).padStart(2, '0');
  const day: string = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function severityRank(status: DueStatus): number {
  if (status === 'Overdue') {
    return 2;
  }
  if (status === 'DueSoon') {
    return 1;
  }
  return 0;
}

function statusFromSeverity(sev: number): DueStatus {
  if (sev >= 2) {
    return 'Overdue';
  }
  if (sev === 1) {
    return 'DueSoon';
  }
  return 'UpToDate';
}

@Injectable({
  providedIn: 'root',
})
export class MockBackendState {
  private readonly notifications: NotificationsService = inject(NotificationsService);

  private currentDateIso = '2026-11-12';
  private orderSequence = 5;
  private lineSequence = 20;
  private historySequence = 40;
  private policySequence = 2;

  readonly customers: readonly MockCustomerDto[] = [
    {
      id: '900000001',
      taxId: '900000001',
      name: 'RutaViva S.A.S.',
      email: 'flota@rutaviva.example',
      phone: '6045550001',
      status: 'Active',
      approvers: [
        {
          id: 'Laura_Gomez',
          customerId: '900000001',
          fullName: 'Laura Gómez',
          email: 'Laura_Gomez@rutaviva.com',
          status: 'Active',
        },
        {
          id: 'Andres_Restrepo',
          customerId: '900000001',
          fullName: 'Andrés Restrepo',
          email: 'Andres_Restrepo@rutaviva.com',
          status: 'Active',
        },
      ],
    },
    {
      id: '800200300',
      taxId: '800200300',
      name: 'Transportes Andinos S.A.',
      email: 'flota@transandinos.example',
      phone: '6045550002',
      status: 'Active',
      approvers: [
        {
          id: 'Camila_Torres',
          customerId: '800200300',
          fullName: 'Camila Torres',
          email: 'Camila_Torres@rutaviva.com',
          status: 'Active',
        },
        {
          id: 'Mateo_Rios',
          customerId: '800200300',
          fullName: 'Mateo Ríos',
          email: 'Mateo_Rios@rutaviva.com',
          status: 'Active',
        },
      ],
    },
    {
      id: '811400500',
      taxId: '811400500',
      name: 'Distribuidora El Valle Ltda.',
      email: 'logistica@elvalle.example',
      phone: '6025550003',
      status: 'Active',
      approvers: [
        {
          id: 'Julian_Ospina',
          customerId: '811400500',
          fullName: 'Julián Ospina',
          email: 'Julian_Ospina@rutaviva.com',
          status: 'Active',
        },
      ],
    },
  ];

  readonly workshops: readonly MockWorkshopDto[] = [
    {
      id: '900100001',
      taxId: '900100001',
      name: 'Autofrenos Medellín',
      email: 'contacto@autofrenos.example',
      phone: '6044440001',
      address: 'Cra 50 # 10-20',
      city: 'Medellín',
      status: 'Active',
    },
    {
      id: '900100002',
      taxId: '900100002',
      name: 'Servicentro La 80',
      email: 'servicio@servicentro80.example',
      phone: '6044440002',
      address: 'Calle 80 # 65-10',
      city: 'Medellín',
      status: 'Active',
    },
    {
      id: '900100003',
      taxId: '900100003',
      name: 'Lubricantes y Llantas Bogotá',
      email: 'info@lubrillantas.example',
      phone: '6014440003',
      address: 'Av 68 # 22-15',
      city: 'Bogotá',
      status: 'Active',
    },
  ];

  readonly routineTypes: MockRoutineTypeDto[] = [
    {
      id: 'OIL_CHANGE',
      code: 'OIL_CHANGE',
      name: 'Cambio de aceite',
      icon: 'oil_barrel',
      defaultIntervalKm: 10_000,
      defaultIntervalMonths: 6,
      dueSoonThresholdPercent: 10,
      status: 'Active',
    },
    {
      id: 'BRAKES',
      code: 'BRAKES',
      name: 'Frenos',
      icon: 'album',
      defaultIntervalKm: 20_000,
      defaultIntervalMonths: 12,
      dueSoonThresholdPercent: 10,
      status: 'Active',
    },
    {
      id: 'TIRES',
      code: 'TIRES',
      name: 'Llantas',
      icon: 'tire_repair',
      defaultIntervalKm: 40_000,
      defaultIntervalMonths: 24,
      dueSoonThresholdPercent: 10,
      status: 'Active',
    },
  ];

  readonly vehicles: MockVehicleDto[] = [
    this.buildVehicle(
      'RVA101',
      '9FB4SRC9AKM100101',
      'Renault',
      'Kangoo',
      2021,
      '900000001',
      '900100001',
      'Laura_Gomez',
      52_300
    ),
    this.buildVehicle(
      'RVA102',
      '9GACE1340NB100102',
      'Chevrolet',
      'NHR',
      2022,
      '900000001',
      '900100001',
      null,
      81_450
    ),
    this.buildVehicle(
      'RVA103',
      null,
      'Toyota',
      'Hilux',
      2023,
      '900000001',
      '900100002',
      'Andres_Restrepo',
      30_200
    ),
    this.buildVehicle(
      'RVA104',
      'JHHAFJ4H2PK100104',
      'Hino',
      '300',
      2023,
      '900000001',
      '900100003',
      'Laura_Gomez',
      98_000
    ),
    this.buildVehicle(
      'TAN201',
      'LVBV3JBB1NE100201',
      'Foton',
      'Aumark',
      2022,
      '800200300',
      '900100002',
      'Camila_Torres',
      120_800
    ),
    this.buildVehicle(
      'TAN202',
      null,
      'Nissan',
      'Frontier',
      2024,
      '800200300',
      '900100003',
      null,
      19_000
    ),
    this.buildVehicle(
      'TAN203',
      'KNCSHX71AP7100203',
      'Kia',
      'K2700',
      2023,
      '800200300',
      '900100003',
      'Mateo_Rios',
      70_100
    ),
    this.buildVehicle(
      'EVA301',
      null,
      'JAC',
      'X200',
      2026,
      '811400500',
      '900100001',
      'Julian_Ospina',
      0
    ),
    this.buildVehicle(
      'EVA302',
      '9GAJM52348B100302',
      'Chevrolet',
      'NPR',
      2020,
      '811400500',
      '900100002',
      null,
      45_500
    ),
    this.buildVehicle(
      'EVA30D',
      null,
      'Yamaha',
      'FZ 2.0',
      2024,
      '811400500',
      '900100001',
      'Julian_Ospina',
      12_500
    ),
  ];

  readonly routines: MockRoutineRecord[] = [
    this.buildRoutine('RVA101', 'OIL_CHANGE', 45_000, '2026-08-01'),
    this.buildRoutine('RVA101', 'BRAKES', 40_000, '2026-03-15'),
    this.buildRoutine('RVA102', 'OIL_CHANGE', 72_000, '2026-07-15'),
    this.buildRoutine('RVA103', 'OIL_CHANGE', 28_000, '2026-05-25'),
    this.buildRoutine('RVA104', 'OIL_CHANGE', 92_000, '2026-09-20'),
    this.buildRoutine('RVA104', 'BRAKES', 77_000, '2025-12-01'),
    this.buildRoutine('TAN201', 'OIL_CHANGE', 110_000, '2026-09-01'),
    this.buildRoutine('TAN201', 'BRAKES', 105_000, '2026-06-10'),
    this.buildRoutine('TAN202', 'OIL_CHANGE', 15_000, '2026-04-20'),
    this.buildRoutine('TAN203', 'OIL_CHANGE', 60_000, '2026-05-01'),
    this.buildRoutine('EVA301', 'OIL_CHANGE', 0, '2026-11-12'),
    this.buildRoutine('EVA301', 'BRAKES', 0, '2026-11-12'),
    this.buildRoutine('EVA302', 'BRAKES', 27_000, '2026-02-10'),
    this.buildRoutine('EVA302', 'TIRES', 10_000, '2025-08-01'),
    this.buildRoutine('EVA30D', 'OIL_CHANGE', 10_000, '2026-10-01', 3_000, 3, 20),
  ];

  private readonly readingsByVehicle = new Map<string, MockOdometerReadingDto[]>();
  readonly alerts: MockAlertDto[] = [];
  readonly orders: MockServiceOrderRecord[] = [
    {
      id: 'OS-0001',
      number: 'OS-0001',
      vehicleId: 'RVA104',
      plate: 'RVA104',
      routineId: 'RVA104-BRAKES',
      routineTypeCode: 'BRAKES',
      routineName: 'Frenos',
      customerId: '900000001',
      workshopId: '900100003',
      status: 'PendingApproval',
      statusBeforeHold: null,
      reason: 'Quote',
      priority: 'Normal',
      autoApproved: false,
      autoApprovalMaxAmountApplied: INITIAL_POLICY_AMOUNT,
      appointmentAt: null,
      lines: [
        {
          id: 'ln-1',
          lineType: 'Routine',
          description: 'Pastillas y discos delanteros',
          amount: 2_350_000,
          pricingStatus: 'Priced',
          requestedBy: 'Workshop',
          approvalStatus: 'PendingApproval',
        },
      ],
      history: [
        {
          id: 'h-1',
          changeType: 'StatusChanged',
          detail: 'Creada',
          changedBy: 'Sistema',
          changedAt: '12/11/2026 06:10',
          icon: 'note_add',
        },
        {
          id: 'h-2',
          changeType: 'StatusChanged',
          detail: 'Cotizada',
          changedBy: 'Lubricantes y Llantas Bogotá',
          changedAt: '12/11/2026 07:02',
          icon: 'request_quote',
        },
        {
          id: 'h-3',
          changeType: 'StatusChanged',
          detail: 'Enviada a aprobación',
          changedBy: 'Sistema',
          changedAt: '12/11/2026 07:02',
          icon: 'gavel',
        },
      ],
      updatedAt: '2026-11-12T07:02:00Z',
    },
    {
      id: 'OS-0002',
      number: 'OS-0002',
      vehicleId: 'TAN201',
      plate: 'TAN201',
      routineId: 'TAN201-OIL_CHANGE',
      routineTypeCode: 'OIL_CHANGE',
      routineName: 'Cambio de aceite',
      customerId: '800200300',
      workshopId: '900100002',
      status: 'Approved',
      statusBeforeHold: null,
      reason: null,
      priority: null,
      autoApproved: true,
      autoApprovalMaxAmountApplied: INITIAL_POLICY_AMOUNT,
      appointmentAt: '14/11/2026 08:00',
      lines: [
        {
          id: 'ln-2',
          lineType: 'Routine',
          description: 'Aceite 15W-40 y filtro',
          amount: 480_000,
          pricingStatus: 'Priced',
          requestedBy: 'Workshop',
          approvalStatus: 'Approved',
        },
      ],
      history: [
        {
          id: 'h-4',
          changeType: 'StatusChanged',
          detail: 'Creada',
          changedBy: 'Sistema',
          changedAt: '12/11/2026 06:10',
          icon: 'note_add',
        },
        {
          id: 'h-5',
          changeType: 'StatusChanged',
          detail: 'Cotizada',
          changedBy: 'Servicentro La 80',
          changedAt: '12/11/2026 06:55',
          icon: 'request_quote',
        },
        {
          id: 'h-6',
          changeType: 'StatusChanged',
          detail: 'Aprobada automáticamente',
          changedBy: 'Sistema',
          changedAt: '12/11/2026 06:55',
          icon: 'bolt',
        },
        {
          id: 'h-7',
          changeType: 'AppointmentScheduled',
          detail: 'Cita 14/11/2026 08:00',
          changedBy: 'Servicentro La 80',
          changedAt: '12/11/2026 07:20',
          icon: 'event_available',
        },
      ],
      updatedAt: '2026-11-12T07:20:00Z',
    },
    {
      id: 'OS-0003',
      number: 'OS-0003',
      vehicleId: 'TAN202',
      plate: 'TAN202',
      routineId: 'TAN202-OIL_CHANGE',
      routineTypeCode: 'OIL_CHANGE',
      routineName: 'Cambio de aceite',
      customerId: '800200300',
      workshopId: '900100003',
      status: 'Cancelled',
      statusBeforeHold: null,
      reason: null,
      priority: null,
      autoApproved: false,
      autoApprovalMaxAmountApplied: INITIAL_POLICY_AMOUNT,
      appointmentAt: null,
      lines: [
        {
          id: 'ln-3',
          lineType: 'Routine',
          description: 'Aceite sintético y filtros',
          amount: 1_950_000,
          pricingStatus: 'Priced',
          requestedBy: 'Workshop',
          approvalStatus: 'Rejected',
        },
      ],
      history: [
        {
          id: 'h-8',
          changeType: 'StatusChanged',
          detail: 'Creada',
          changedBy: 'Sistema',
          changedAt: '12/11/2026 06:10',
          icon: 'note_add',
        },
        {
          id: 'h-9',
          changeType: 'StatusChanged',
          detail: 'Cotizada',
          changedBy: 'Lubricantes y Llantas Bogotá',
          changedAt: '12/11/2026 06:40',
          icon: 'request_quote',
        },
        {
          id: 'h-10',
          changeType: 'StatusChanged',
          detail: 'Rechazada',
          changedBy: 'Camila Torres',
          changedAt: '12/11/2026 08:05',
          icon: 'thumb_down',
        },
      ],
      updatedAt: '2026-11-12T08:05:00Z',
    },
    {
      id: 'OS-0004',
      number: 'OS-0004',
      vehicleId: 'TAN203',
      plate: 'TAN203',
      routineId: 'TAN203-OIL_CHANGE',
      routineTypeCode: 'OIL_CHANGE',
      routineName: 'Cambio de aceite',
      customerId: '800200300',
      workshopId: '900100003',
      status: 'PendingApproval',
      statusBeforeHold: null,
      reason: 'AdditionalServices',
      priority: 'High',
      autoApproved: true,
      autoApprovalMaxAmountApplied: INITIAL_POLICY_AMOUNT,
      appointmentAt: '12/11/2026 07:30',
      lines: [
        {
          id: 'ln-4',
          lineType: 'Routine',
          description: 'Aceite y filtro',
          amount: 520_000,
          pricingStatus: 'Priced',
          requestedBy: 'Workshop',
          approvalStatus: 'Approved',
        },
        {
          id: 'ln-5',
          lineType: 'Additional',
          description: 'Filtro de aire obstruido',
          amount: 180_000,
          pricingStatus: 'Priced',
          requestedBy: 'Workshop',
          approvalStatus: 'PendingApproval',
        },
      ],
      history: [
        {
          id: 'h-11',
          changeType: 'StatusChanged',
          detail: 'Creada',
          changedBy: 'Sistema',
          changedAt: '12/11/2026 06:10',
          icon: 'note_add',
        },
        {
          id: 'h-12',
          changeType: 'StatusChanged',
          detail: 'Aprobada automáticamente',
          changedBy: 'Sistema',
          changedAt: '12/11/2026 06:30',
          icon: 'bolt',
        },
        {
          id: 'h-13',
          changeType: 'AppointmentScheduled',
          detail: 'Cita 12/11/2026 07:30',
          changedBy: 'Lubricantes y Llantas Bogotá',
          changedAt: '12/11/2026 06:35',
          icon: 'event_available',
        },
        {
          id: 'h-14',
          changeType: 'StatusChanged',
          detail: 'En sitio',
          changedBy: 'Lubricantes y Llantas Bogotá',
          changedAt: '12/11/2026 07:34',
          icon: 'garage',
        },
        {
          id: 'h-15',
          changeType: 'AdditionalServiceAdded',
          detail: 'Servicio adicional por aprobar',
          changedBy: 'Lubricantes y Llantas Bogotá',
          changedAt: '12/11/2026 08:12',
          icon: 'priority_high',
        },
      ],
      updatedAt: '2026-11-12T08:12:00Z',
    },
  ];

  readonly approvalPolicies: MockApprovalPolicyEntryDto[] = [
    {
      id: 'pol-1',
      currency: 'COP',
      autoApprovalMaxAmount: INITIAL_POLICY_AMOUNT,
      effectiveFrom: '10/11/2026 08:00',
      createdBy: 'Operador',
    },
  ];

  constructor() {
    for (const vehicle of this.vehicles) {
      const readings: MockOdometerReadingDto[] =
        vehicle.odometerKm > 0
          ? [
              {
                kilometers: vehicle.odometerKm,
                readAt: '12/11/2026 08:42',
                outcome: 'Accepted',
                ignoredReason: null,
              },
              {
                kilometers: vehicle.odometerKm,
                readAt: '12/11/2026 08:41',
                outcome: 'Ignored',
                ignoredReason: 'NotGreaterThanCurrent',
              },
              {
                kilometers: vehicle.odometerKm - 50,
                readAt: '12/11/2026 08:40',
                outcome: 'Ignored',
                ignoredReason: 'NotGreaterThanCurrent',
              },
              {
                kilometers: Math.max(0, vehicle.odometerKm - 420),
                readAt: '11/11/2026 17:05',
                outcome: 'Accepted',
                ignoredReason: null,
              },
            ]
          : [];
      this.readingsByVehicle.set(vehicle.id, readings);
    }

    for (const rt of this.routines) {
      if (
        ['TAN202-OIL_CHANGE', 'RVA104-BRAKES', 'TAN201-OIL_CHANGE', 'TAN203-OIL_CHANGE'].includes(
          rt.routineId
        )
      ) {
        rt.cycleOrderCreated = true;
      }
    }

    this.reconcileAlertsAndOrders(true);
    for (const alert of this.alerts) {
      if (alert.plate === 'TAN202') {
        alert.status = 'Acknowledged';
        alert.acknowledgedBy = 'Camila Torres';
      }
    }
  }

  getVehicles(customerId?: string | null, status?: VehicleStatus | null): readonly MockVehicleDto[] {
    return this.vehicles.filter((v: MockVehicleDto) => {
      if (customerId && v.customerId !== customerId) {
        return false;
      }
      if (status && v.status !== status) {
        return false;
      }
      return true;
    });
  }

  getVehicleByIdOrPlate(idOrPlate: string): MockVehicleDto | null {
    return (
      this.vehicles.find(
        (v: MockVehicleDto) =>
          v.id.toLowerCase() === idOrPlate.toLowerCase() ||
          v.plate.toLowerCase() === idOrPlate.toLowerCase()
      ) ?? null
    );
  }

  getPlanSummaries(customerId?: string | null): readonly MockPlanSummaryDto[] {
    return this.vehicles
      .filter((v: MockVehicleDto) => !customerId || v.customerId === customerId)
      .map((v: MockVehicleDto) => this.buildPlanSummary(v));
  }

  getPlanByVehicle(vehicleIdOrPlate: string): MockPlanDetailDto | null {
    const vehicle: MockVehicleDto | null = this.getVehicleByIdOrPlate(vehicleIdOrPlate);
    if (!vehicle) {
      return null;
    }
    const evaluated: readonly MockEvaluatedRoutineDto[] = this.routines
      .filter((r: MockRoutineRecord) => r.vehicleId === vehicle.id)
      .map((r: MockRoutineRecord) => this.evaluateRoutine(r, vehicle));

    return {
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      customerId: vehicle.customerId,
      status: vehicle.status === 'Active' ? 'Active' : 'Suspended',
      odometerKm: vehicle.odometerKm,
      routines: evaluated,
    };
  }

  getReadings(vehicleIdOrPlate: string): readonly MockOdometerReadingDto[] {
    const vehicle: MockVehicleDto | null = this.getVehicleByIdOrPlate(vehicleIdOrPlate);
    if (!vehicle) {
      return [];
    }
    return this.readingsByVehicle.get(vehicle.id) ?? [];
  }

  getAlerts(customerId?: string | null, status?: AlertStatus | null): readonly MockAlertDto[] {
    return this.alerts
      .filter((a: MockAlertDto) => {
        if (customerId && a.customerId !== customerId) {
          return false;
        }
        if (status && a.status !== status) {
          return false;
        }
        return true;
      })
      .sort((a: MockAlertDto, b: MockAlertDto) => {
        const typeDiff: number = (b.type === 'Overdue' ? 1 : 0) - (a.type === 'Overdue' ? 1 : 0);
        return typeDiff !== 0 ? typeDiff : b.alertId.localeCompare(a.alertId);
      });
  }

  acknowledgeAlert(alertId: string, acknowledgedBy: string): MockAlertDto | null {
    const alert: MockAlertDto | undefined = this.alerts.find(
      (a: MockAlertDto) => a.alertId === alertId
    );
    if (!alert) {
      return null;
    }
    alert.status = 'Acknowledged';
    alert.acknowledgedBy = acknowledgedBy;
    this.notifications.emitAlertChanged({
      alertId: alert.alertId,
      vehicleId: alert.vehicleId,
      plate: alert.plate,
      customerId: alert.customerId,
      routineId: alert.routineId,
      routineName: alert.routineName,
      type: alert.type,
      triggeredBy: alert.triggeredBy,
      status: alert.status,
      raisedAt: alert.raisedAt,
      acknowledgedBy: alert.acknowledgedBy,
      resolutionReason: alert.resolutionReason,
      occurredAt: new Date().toISOString(),
    });
    return { ...alert };
  }

  updateVehicleStatus(vehicleIdOrPlate: string, status: VehicleStatus): MockVehicleDto | null {
    const vehicle: MockVehicleDto | null = this.getVehicleByIdOrPlate(vehicleIdOrPlate);
    if (!vehicle) {
      return null;
    }
    vehicle.status = status;
    for (const order of this.orders) {
      if (order.vehicleId === vehicle.id) {
        if (status === 'Inactive' && OPEN_ORDER_STATUSES.includes(order.status)) {
          order.statusBeforeHold = order.status;
          order.status = 'OnHold';
        } else if (status === 'Active' && order.status === 'OnHold' && order.statusBeforeHold) {
          order.status = order.statusBeforeHold;
          order.statusBeforeHold = null;
        }
      }
    }
    if (status === 'Active') {
      this.reconcileAlertsAndOrders(false);
    }
    this.notifications.emitVehicleChanged({
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      customerId: vehicle.customerId,
      status: vehicle.status,
      planStatus: vehicle.status === 'Active' ? 'Active' : 'Suspended',
      occurredAt: new Date().toISOString(),
    });
    return { ...vehicle };
  }

  updateRoutineParameters(
    vehicleIdOrPlate: string,
    routineId: string,
    body: {
      readonly intervalKm?: number | null;
      readonly intervalMonths?: number | null;
      readonly dueSoonThresholdPercent?: number | null;
    }
  ): MockEvaluatedRoutineDto | null {
    const vehicle: MockVehicleDto | null = this.getVehicleByIdOrPlate(vehicleIdOrPlate);
    const routine: MockRoutineRecord | undefined = this.routines.find(
      (r: MockRoutineRecord) => r.routineId === routineId
    );
    if (!vehicle || !routine) {
      return null;
    }
    routine.intervalKmOverride = body.intervalKm ?? null;
    routine.intervalMonthsOverride = body.intervalMonths ?? null;
    routine.dueSoonPercentOverride = body.dueSoonThresholdPercent ?? null;
    this.reconcileAlertsAndOrders(false);
    const evaluated: MockEvaluatedRoutineDto = this.evaluateRoutine(routine, vehicle);
    this.emitRoutineUpdate(evaluated, vehicle);
    return evaluated;
  }

  updateRoutineState(
    vehicleIdOrPlate: string,
    routineId: string,
    state: RoutineState
  ): MockEvaluatedRoutineDto | null {
    const vehicle: MockVehicleDto | null = this.getVehicleByIdOrPlate(vehicleIdOrPlate);
    const routine: MockRoutineRecord | undefined = this.routines.find(
      (r: MockRoutineRecord) => r.routineId === routineId
    );
    if (!vehicle || !routine) {
      return null;
    }
    routine.state = state;
    if (state === 'Inactive') {
      for (const alert of this.alerts) {
        if (alert.routineId === routine.routineId && alert.status !== 'Resolved') {
          alert.status = 'Resolved';
          alert.resolutionReason = 'RoutineDeactivated';
        }
      }
    } else {
      this.reconcileAlertsAndOrders(false);
    }
    const evaluated: MockEvaluatedRoutineDto = this.evaluateRoutine(routine, vehicle);
    this.emitRoutineUpdate(evaluated, vehicle);
    return evaluated;
  }

  recordOdometerReading(
    plate: string,
    kilometers: number,
    readAt: string
  ): {
    readonly plate: string;
    readonly kilometers: number;
    readonly readAt: string;
    readonly outcome: OdometerReadingOutcome;
    readonly ignoredReason: OdometerReadingIgnoredReason | null;
  } {
    const vehicle: MockVehicleDto | null = this.getVehicleByIdOrPlate(plate);
    if (!vehicle) {
      return {
        plate,
        kilometers,
        readAt,
        outcome: 'Ignored',
        ignoredReason: 'UnknownVehicle',
      };
    }

    const isAccepted: boolean = kilometers > vehicle.odometerKm;
    const outcome: OdometerReadingOutcome = isAccepted ? 'Accepted' : 'Ignored';
    const ignoredReason: OdometerReadingIgnoredReason | null = isAccepted
      ? null
      : 'NotGreaterThanCurrent';

    const list: MockOdometerReadingDto[] = this.readingsByVehicle.get(vehicle.id) ?? [];
    list.unshift({
      kilometers,
      readAt,
      outcome,
      ignoredReason,
    });
    this.readingsByVehicle.set(vehicle.id, list);

    if (isAccepted) {
      vehicle.odometerKm = kilometers;
      vehicle.lastReadingText = 'ahora';
      this.reconcileAlertsAndOrders(false);
    }

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

    if (isAccepted) {
      for (const rt of this.routines.filter((r: MockRoutineRecord) => r.vehicleId === vehicle.id)) {
        const evaluated: MockEvaluatedRoutineDto = this.evaluateRoutine(rt, vehicle);
        this.emitRoutineUpdate(evaluated, vehicle);
      }
    }

    return {
      plate: vehicle.plate,
      kilometers,
      readAt,
      outcome,
      ignoredReason,
    };
  }

  runTimeEvaluation(asOfDate?: string): {
    readonly asOfDate: string;
    readonly evaluatedPlans: number;
    readonly dueSoonRoutines: number;
    readonly overdueRoutines: number;
    readonly requestedOrders: number;
  } {
    if (asOfDate && asOfDate > this.currentDateIso) {
      this.currentDateIso = asOfDate.slice(0, 10);
    }
    const ordersBefore: number = this.orders.length;
    this.reconcileAlertsAndOrders(false);
    const evaluatedList: MockEvaluatedRoutineDto[] = this.routines.map((r: MockRoutineRecord) => {
      const v: MockVehicleDto = this.getVehicleByIdOrPlate(r.vehicleId)!;
      return this.evaluateRoutine(r, v);
    });
    return {
      asOfDate: this.currentDateIso,
      evaluatedPlans: this.vehicles.filter((v: MockVehicleDto) => v.status === 'Active').length,
      dueSoonRoutines: evaluatedList.filter((e: MockEvaluatedRoutineDto) => e.dueStatus === 'DueSoon')
        .length,
      overdueRoutines: evaluatedList.filter((e: MockEvaluatedRoutineDto) => e.dueStatus === 'Overdue')
        .length,
      requestedOrders: Math.max(0, this.orders.length - ordersBefore),
    };
  }

  getServiceOrders(query: {
    readonly customerId?: string | null;
    readonly vehicleId?: string | null;
    readonly routineId?: string | null;
    readonly statusGroup?: string | null;
  }): readonly MockServiceOrderListItemDto[] {
    return this.orders
      .filter((o: MockServiceOrderRecord) => {
        if (query.customerId && o.customerId !== query.customerId) {
          return false;
        }
        if (
          query.vehicleId &&
          o.vehicleId.toLowerCase() !== query.vehicleId.toLowerCase() &&
          o.plate.toLowerCase() !== query.vehicleId.toLowerCase()
        ) {
          return false;
        }
        if (query.routineId && o.routineId !== query.routineId) {
          return false;
        }
        if (query.statusGroup === 'PendingApproval' && o.status !== 'PendingApproval') {
          return false;
        }
        if (query.statusGroup === 'Open' && !OPEN_ORDER_STATUSES.includes(o.status)) {
          return false;
        }
        if (query.statusGroup === 'Closed' && OPEN_ORDER_STATUSES.includes(o.status)) {
          return false;
        }
        return true;
      })
      .map((o: MockServiceOrderRecord) => this.toOrderListItem(o));
  }

  getServiceOrderDetail(
    id: string,
    actor: Profile = 'Operator'
  ): MockServiceOrderDetailDto | null {
    const order: MockServiceOrderRecord | undefined = this.orders.find(
      (o: MockServiceOrderRecord) => o.id === id || o.number === id
    );
    if (!order) {
      return null;
    }
    const workshop: MockWorkshopDto | undefined = this.workshops.find(
      (w: MockWorkshopDto) => w.id === order.workshopId
    );
    const type: MockRoutineTypeDto | undefined = this.routineTypes.find(
      (t: MockRoutineTypeDto) => t.code === order.routineTypeCode
    );
    return {
      id: order.id,
      number: order.number,
      vehicleId: order.vehicleId,
      plate: order.plate,
      routineId: order.routineId,
      routineName: order.routineName,
      routineTypeCode: order.routineTypeCode,
      routineIcon: type?.icon ?? 'build',
      customerId: order.customerId,
      workshopId: order.workshopId,
      workshopName: workshop?.name ?? 'Taller',
      status: order.status,
      statusBeforeHold: order.statusBeforeHold,
      reason: order.reason,
      priority: order.priority,
      availableActions: this.computeAvailableActions(order, actor),
      lines: order.lines.map((l: MockServiceLineDto) => ({ ...l })),
      total: this.calculateOrderTotal(order),
      autoApproved: order.autoApproved,
      autoApprovalMaxAmountApplied: order.autoApprovalMaxAmountApplied,
      appointmentAt: order.appointmentAt,
      history: order.history.map((h: MockOrderHistoryDto) => ({ ...h })),
      updatedAt: order.updatedAt,
    };
  }

  createManualOrder(vehicleIdOrPlate: string, routineId: string): MockServiceOrderDetailDto | null {
    const vehicle: MockVehicleDto | null = this.getVehicleByIdOrPlate(vehicleIdOrPlate);
    const routine: MockRoutineRecord | undefined = this.routines.find(
      (r: MockRoutineRecord) => r.routineId === routineId
    );
    if (!vehicle || !routine) {
      return null;
    }
    const order: MockServiceOrderRecord = this.createOrderInternal(routine, vehicle, 'Operador');
    return this.getServiceOrderDetail(order.id, 'Operator');
  }

  performOrderAction(
    id: string,
    action:
      | 'approve'
      | 'reject'
      | 'cancel'
      | 'quote'
      | 'requestAdditional'
      | 'priceAdditional'
      | 'addAdditional'
      | 'appointment'
      | 'onSite'
      | 'start'
      | 'complete',
    payload: Record<string, unknown>
  ): MockServiceOrderDetailDto | null {
    const order: MockServiceOrderRecord | undefined = this.orders.find(
      (o: MockServiceOrderRecord) => o.id === id
    );
    if (!order) {
      return null;
    }

    const nowText = '12/11/2026 09:15';
    const actorName: string =
      typeof payload['actorName'] === 'string' ? payload['actorName'] : 'Usuario';

    if (action === 'approve') {
      for (const line of order.lines) {
        if (line.approvalStatus === 'PendingApproval') {
          line.approvalStatus = 'Approved';
        }
      }
      const isAdditional: boolean = order.reason === 'AdditionalServices';
      order.status = isAdditional ? 'OnSite' : 'Approved';
      order.reason = null;
      order.priority = null;
      this.appendHistory(
        order,
        'StatusChanged',
        isAdditional ? 'Servicio adicional aprobado' : 'Aprobada',
        actorName,
        nowText,
        'thumb_up'
      );
    } else if (action === 'reject') {
      const isAdditional: boolean = order.reason === 'AdditionalServices';
      for (const line of order.lines) {
        if (line.approvalStatus === 'PendingApproval') {
          line.approvalStatus = 'Rejected';
        }
      }
      order.status = isAdditional ? 'OnSite' : 'Cancelled';
      order.reason = null;
      order.priority = null;
      this.appendHistory(
        order,
        'StatusChanged',
        isAdditional ? 'Servicio adicional rechazado' : 'Rechazada',
        actorName,
        nowText,
        'thumb_down'
      );
    } else if (action === 'cancel') {
      order.status = 'Cancelled';
      order.reason = null;
      order.priority = null;
      this.appendHistory(order, 'StatusChanged', 'Cancelada', actorName, nowText, 'block');
    } else if (action === 'quote') {
      const description: string = String(payload['description'] ?? order.routineName);
      const amount: number = Number(payload['amount'] ?? 0);
      const policyAmount: number = this.currentPolicyAmount();
      const auto: boolean = amount <= policyAmount;
      order.autoApprovalMaxAmountApplied = policyAmount;
      this.lineSequence += 1;
      order.lines = [
        {
          id: `ln-${this.lineSequence}`,
          lineType: 'Routine',
          description,
          amount,
          pricingStatus: 'Priced',
          requestedBy: 'Workshop',
          approvalStatus: auto ? 'Approved' : 'PendingApproval',
        },
      ];
      this.appendHistory(order, 'StatusChanged', 'Cotizada', actorName, nowText, 'request_quote');
      if (auto) {
        order.status = 'Approved';
        order.autoApproved = true;
        this.appendHistory(
          order,
          'StatusChanged',
          'Aprobada automáticamente',
          'Sistema',
          nowText,
          'bolt'
        );
      } else {
        order.status = 'PendingApproval';
        order.reason = 'Quote';
        order.priority = 'Normal';
        this.appendHistory(
          order,
          'StatusChanged',
          'Enviada a aprobación',
          'Sistema',
          nowText,
          'gavel'
        );
      }
    } else if (action === 'requestAdditional') {
      const description: string = String(payload['description'] ?? '');
      this.lineSequence += 1;
      order.lines.push({
        id: `ln-${this.lineSequence}`,
        lineType: 'Additional',
        description,
        amount: null,
        pricingStatus: 'PendingPricing',
        requestedBy: 'Approver',
        approvalStatus: 'PendingApproval',
      });
      this.appendHistory(
        order,
        'AdditionalServiceAdded',
        `Pedido: ${description}`,
        actorName,
        nowText,
        'add_circle'
      );
    } else if (action === 'priceAdditional') {
      const lineId: string = String(payload['lineId'] ?? '');
      const amount: number = Number(payload['amount'] ?? 0);
      const targetLine: MockServiceLineDto | undefined = order.lines.find(
        (l: MockServiceLineDto) => l.id === lineId
      );
      if (targetLine) {
        targetLine.amount = amount;
        targetLine.pricingStatus = 'Priced';
        this.appendHistory(
          order,
          'AdditionalServiceAdded',
          `Precio: ${targetLine.description}`,
          actorName,
          nowText,
          'sell'
        );
      }
    } else if (action === 'addAdditional') {
      const description: string = String(payload['description'] ?? '');
      const amount: number = Number(payload['amount'] ?? 0);
      this.lineSequence += 1;
      order.lines.push({
        id: `ln-${this.lineSequence}`,
        lineType: 'Additional',
        description,
        amount,
        pricingStatus: 'Priced',
        requestedBy: 'Workshop',
        approvalStatus: 'PendingApproval',
      });
      order.status = 'PendingApproval';
      order.reason = 'AdditionalServices';
      order.priority = 'High';
      this.appendHistory(
        order,
        'AdditionalServiceAdded',
        'Servicio adicional por aprobar',
        actorName,
        nowText,
        'priority_high'
      );
      this.notifications.emitApprovalRequested({
        serviceOrderId: order.id,
        number: order.number,
        plate: order.plate,
        routineName: order.routineName,
        customerId: order.customerId,
        reason: 'AdditionalServices',
        priority: 'High',
        totalAmount: this.calculateOrderTotal(order),
        occurredAt: new Date().toISOString(),
      });
    } else if (action === 'appointment') {
      const scheduledAt: string = String(payload['scheduledAt'] ?? '');
      const isReschedule: boolean = order.appointmentAt !== null;
      order.appointmentAt = scheduledAt;
      this.appendHistory(
        order,
        isReschedule ? 'AppointmentRescheduled' : 'AppointmentScheduled',
        `${isReschedule ? 'Reprogramada' : 'Cita'} ${scheduledAt}`,
        actorName,
        nowText,
        'event_available'
      );
    } else if (action === 'onSite') {
      order.status = 'OnSite';
      this.appendHistory(order, 'StatusChanged', 'En sitio', actorName, nowText, 'garage');
    } else if (action === 'start') {
      order.status = 'InProgress';
      this.appendHistory(order, 'StatusChanged', 'En proceso', actorName, nowText, 'build');
    } else if (action === 'complete') {
      const servicedKm: number = Number(payload['servicedKm'] ?? 0);
      const servicedAt: string = String(payload['servicedAt'] ?? this.currentDateIso);
      order.status = 'Completed';
      this.appendHistory(
        order,
        'StatusChanged',
        `Finalizada · ${servicedKm} km`,
        actorName,
        nowText,
        'check_circle'
      );

      const routine: MockRoutineRecord | undefined = this.routines.find(
        (r: MockRoutineRecord) => r.routineId === order.routineId
      );
      const vehicle: MockVehicleDto | null = this.getVehicleByIdOrPlate(order.vehicleId);
      if (routine && vehicle) {
        routine.cycleStartKm = servicedKm;
        routine.cycleStartDate = servicedAt.slice(0, 10);
        routine.stickyOverdue = false;
        routine.cycleOrderCreated = false;
        routine.cycleNumber += 1;
        if (servicedKm > vehicle.odometerKm) {
          vehicle.odometerKm = servicedKm;
        }
        for (const alert of this.alerts) {
          if (alert.routineId === routine.routineId && alert.status !== 'Resolved') {
            alert.status = 'Resolved';
            alert.resolutionReason = 'RoutineRestarted';
            this.notifications.emitAlertChanged({
              alertId: alert.alertId,
              vehicleId: alert.vehicleId,
              plate: alert.plate,
              customerId: alert.customerId,
              routineId: alert.routineId,
              routineName: alert.routineName,
              type: alert.type,
              triggeredBy: alert.triggeredBy,
              status: alert.status,
              raisedAt: alert.raisedAt,
              acknowledgedBy: alert.acknowledgedBy,
              resolutionReason: alert.resolutionReason,
              occurredAt: new Date().toISOString(),
            });
          }
        }
        const evaluated: MockEvaluatedRoutineDto = this.evaluateRoutine(routine, vehicle);
        this.emitRoutineUpdate(evaluated, vehicle);
      }
    }

    order.updatedAt = new Date().toISOString();
    this.notifications.emitServiceOrderChanged({
      serviceOrderId: order.id,
      number: order.number,
      vehicleId: order.vehicleId,
      plate: order.plate,
      routineId: order.routineId,
      routineName: order.routineName,
      customerId: order.customerId,
      workshopId: order.workshopId,
      status: order.status,
      priority: order.priority,
      reason: order.reason,
      totalAmount: this.calculateOrderTotal(order),
      autoApproved: order.autoApproved,
      appointmentAt: order.appointmentAt,
      updatedAt: order.updatedAt,
    });

    return this.getServiceOrderDetail(order.id, 'Operator');
  }

  updateRoutineType(
    id: string,
    body: {
      readonly defaultIntervalKm: number | null;
      readonly defaultIntervalMonths: number | null;
      readonly dueSoonThresholdPercent: number;
    }
  ): MockRoutineTypeDto | null {
    const target: MockRoutineTypeDto | undefined = this.routineTypes.find(
      (t: MockRoutineTypeDto) => t.id === id || t.code === id
    );
    if (!target) {
      return null;
    }
    target.defaultIntervalKm = body.defaultIntervalKm;
    target.defaultIntervalMonths = body.defaultIntervalMonths;
    target.dueSoonThresholdPercent = body.dueSoonThresholdPercent;
    return { ...target };
  }

  getApprovalPolicy(): MockApprovalPolicyResponseDto {
    return {
      currentAmount: this.currentPolicyAmount(),
      currency: 'COP',
      history: this.approvalPolicies.map((p: MockApprovalPolicyEntryDto) => ({ ...p })),
    };
  }

  addApprovalPolicy(amount: number, createdBy = 'Operador'): MockApprovalPolicyResponseDto {
    this.policySequence += 1;
    this.approvalPolicies.unshift({
      id: `pol-${this.policySequence}`,
      currency: 'COP',
      autoApprovalMaxAmount: amount,
      effectiveFrom: '12/11/2026 09:30',
      createdBy,
    });
    return this.getApprovalPolicy();
  }

  private currentPolicyAmount(): number {
    return this.approvalPolicies[0]?.autoApprovalMaxAmount ?? INITIAL_POLICY_AMOUNT;
  }

  private appendHistory(
    order: MockServiceOrderRecord,
    changeType: ServiceOrderChangeType,
    detail: string,
    changedBy: string,
    changedAt: string,
    icon: string
  ): void {
    this.historySequence += 1;
    order.history.push({
      id: `h-${this.historySequence}`,
      changeType,
      detail,
      changedBy,
      changedAt,
      icon,
    });
  }

  private computeAvailableActions(
    order: MockServiceOrderRecord,
    actor: Profile
  ): readonly MockServiceOrderActionDto[] {
    const actions: MockServiceOrderActionDto[] = [];
    const hasPendingPrice: boolean = order.lines.some(
      (l: MockServiceLineDto) => l.pricingStatus === 'PendingPricing'
    );

    if (actor === 'Approver') {
      if (order.status === 'PendingApproval') {
        actions.push({
          action: 'Approve',
          enabled: !hasPendingPrice,
          blockedReason: hasPendingPrice ? 'WaitingForPrice' : null,
        });
        actions.push({
          action: 'Reject',
          enabled: true,
          blockedReason: null,
        });
        if (order.reason === 'Quote') {
          actions.push({
            action: 'RequestAdditionalService',
            enabled: true,
            blockedReason: null,
          });
        }
      }
      const canCancel: boolean =
        PRE_PROGRESS_STATUSES.includes(order.status) ||
        (order.status === 'OnHold' &&
          order.statusBeforeHold !== null &&
          PRE_PROGRESS_STATUSES.includes(order.statusBeforeHold));
      if (canCancel) {
        actions.push({
          action: 'Cancel',
          enabled: true,
          blockedReason: null,
        });
      }
    }

    if (actor === 'Workshop') {
      if (order.status === 'Created') {
        actions.push({
          action: 'Quote',
          enabled: true,
          blockedReason: null,
        });
      }
      if (order.status === 'Approved') {
        actions.push({
          action: 'ScheduleAppointment',
          enabled: true,
          blockedReason: null,
        });
        actions.push({
          action: 'MarkOnSite',
          enabled: order.appointmentAt !== null,
          blockedReason: order.appointmentAt !== null ? null : 'AppointmentRequired',
        });
      }
      if (order.status === 'OnSite') {
        actions.push({
          action: 'AddAdditionalService',
          enabled: true,
          blockedReason: null,
        });
        actions.push({
          action: 'Start',
          enabled: true,
          blockedReason: null,
        });
      }
      if (order.status === 'InProgress') {
        actions.push({
          action: 'Complete',
          enabled: true,
          blockedReason: null,
        });
      }
    }

    return actions;
  }

  private calculateOrderTotal(order: MockServiceOrderRecord): number {
    return order.lines
      .filter((l: MockServiceLineDto) => l.amount !== null && l.approvalStatus !== 'Rejected')
      .reduce((sum: number, l: MockServiceLineDto) => sum + (l.amount ?? 0), 0);
  }

  private toOrderListItem(order: MockServiceOrderRecord): MockServiceOrderListItemDto {
    const total: number = this.calculateOrderTotal(order);
    return {
      id: order.id,
      number: order.number,
      vehicleId: order.vehicleId,
      plate: order.plate,
      routineId: order.routineId,
      routineName: order.routineName,
      routineTypeCode: order.routineTypeCode,
      customerId: order.customerId,
      status: order.status,
      statusBeforeHold: order.statusBeforeHold,
      reason: order.reason,
      priority: order.priority,
      total: total > 0 ? total : null,
      appointmentAt: order.appointmentAt,
      updatedAt: order.updatedAt,
    };
  }

  private buildPlanSummary(vehicle: MockVehicleDto): MockPlanSummaryDto {
    if (vehicle.status === 'Inactive') {
      return {
        vehicleId: vehicle.id,
        plate: vehicle.plate,
        customerId: vehicle.customerId,
        status: 'Suspended',
        odometerKm: vehicle.odometerKm,
        worstDueStatus: null,
        worstConsumedPercent: 0,
        worstTriggeredBy: null,
        nextRoutineName: null,
        nextRemainingKind: null,
        nextRemainingValue: null,
      };
    }

    const activeRoutines: MockEvaluatedRoutineDto[] = this.routines
      .filter((r: MockRoutineRecord) => r.vehicleId === vehicle.id && r.state === 'Active')
      .map((r: MockRoutineRecord) => this.evaluateRoutine(r, vehicle));

    if (activeRoutines.length === 0) {
      return {
        vehicleId: vehicle.id,
        plate: vehicle.plate,
        customerId: vehicle.customerId,
        status: 'Active',
        odometerKm: vehicle.odometerKm,
        worstDueStatus: 'UpToDate',
        worstConsumedPercent: 0,
        worstTriggeredBy: null,
        nextRoutineName: null,
        nextRemainingKind: null,
        nextRemainingValue: null,
      };
    }

    const sorted: MockEvaluatedRoutineDto[] = [...activeRoutines].sort(
      (a: MockEvaluatedRoutineDto, b: MockEvaluatedRoutineDto) => {
        const rankDiff: number = severityRank(b.dueStatus) - severityRank(a.dueStatus);
        if (rankDiff !== 0) {
          return rankDiff;
        }
        return this.maxConsumed(b) - this.maxConsumed(a);
      }
    );

    const worst: MockEvaluatedRoutineDto = sorted[0]!;
    const kmTrack: MockTrackDto | undefined = worst.tracks.find(
      (t: MockTrackDto) => t.kind === 'km'
    );
    const timeTrack: MockTrackDto | undefined = worst.tracks.find(
      (t: MockTrackDto) => t.kind === 'time'
    );
    const useKm: boolean =
      kmTrack !== undefined &&
      (timeTrack === undefined || kmTrack.consumedPercent >= timeTrack.consumedPercent);
    const primaryTrack: MockTrackDto | undefined = useKm ? kmTrack : timeTrack;

    return {
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      customerId: vehicle.customerId,
      status: 'Active',
      odometerKm: vehicle.odometerKm,
      worstDueStatus: worst.dueStatus,
      worstConsumedPercent: primaryTrack?.consumedPercent ?? 0,
      worstTriggeredBy: worst.dueStatus === 'UpToDate' ? null : worst.triggeredBy,
      nextRoutineName: worst.name,
      nextRemainingKind: primaryTrack?.kind ?? null,
      nextRemainingValue: primaryTrack?.remaining ?? null,
    };
  }

  private maxConsumed(routine: MockEvaluatedRoutineDto): number {
    return routine.tracks.reduce(
      (max: number, t: MockTrackDto) => Math.max(max, t.consumedPercent),
      0
    );
  }

  private evaluateRoutine(
    routine: MockRoutineRecord,
    vehicle: MockVehicleDto
  ): MockEvaluatedRoutineDto {
    const rType: MockRoutineTypeDto = this.routineTypes.find(
      (t: MockRoutineTypeDto) => t.code === routine.typeCode
    )!;
    const intervalKm: number | null = routine.intervalKmOverride ?? rType.defaultIntervalKm;
    const intervalMonths: number | null =
      routine.intervalMonthsOverride ?? rType.defaultIntervalMonths;
    const effectiveDueSoonPercent: number =
      routine.dueSoonPercentOverride ?? rType.dueSoonThresholdPercent;

    const tracks: MockTrackDto[] = [];
    let nextDueKm: number | null = null;
    let nextDueDate: string | null = null;
    let kmSev = 0;
    let timeSev = 0;

    if (intervalKm !== null && intervalKm > 0) {
      nextDueKm = routine.cycleStartKm + intervalKm;
      const consumedKm: number = Math.max(
        0,
        ((vehicle.odometerKm - routine.cycleStartKm) / intervalKm) * FULL_PERCENT
      );
      const remainingKm: number = nextDueKm - vehicle.odometerKm;
      kmSev =
        consumedKm >= FULL_PERCENT
          ? 2
          : consumedKm >= FULL_PERCENT - effectiveDueSoonPercent
            ? 1
            : 0;
      tracks.push({
        kind: 'km',
        consumedPercent: Math.round(consumedKm * 100) / 100,
        dueSoonPercent: effectiveDueSoonPercent,
        remaining: remainingKm,
        status: statusFromSeverity(kmSev),
      });
    }

    if (intervalMonths !== null && intervalMonths > 0) {
      const startDate: Date = parseIsoDate(routine.cycleStartDate);
      const dueDate: Date = addMonthsIso(routine.cycleStartDate, intervalMonths);
      const todayDate: Date = parseIsoDate(this.currentDateIso);
      nextDueDate = formatDateIso(dueDate);
      const totalDays: number = Math.max(1, (dueDate.getTime() - startDate.getTime()) / MS_PER_DAY);
      const elapsedDays: number = (todayDate.getTime() - startDate.getTime()) / MS_PER_DAY;
      const consumedTime: number = Math.max(0, (elapsedDays / totalDays) * FULL_PERCENT);
      const remainingDays: number = Math.round(
        (dueDate.getTime() - todayDate.getTime()) / MS_PER_DAY
      );
      timeSev =
        consumedTime >= FULL_PERCENT
          ? 2
          : consumedTime >= FULL_PERCENT - effectiveDueSoonPercent
            ? 1
            : 0;
      tracks.push({
        kind: 'time',
        consumedPercent: Math.round(consumedTime * 100) / 100,
        dueSoonPercent: effectiveDueSoonPercent,
        remaining: remainingDays,
        status: statusFromSeverity(timeSev),
      });
    }

    let maxSev: number = Math.max(kmSev, timeSev);
    if (routine.stickyOverdue && maxSev < 2) {
      maxSev = 2;
    }

    const dueStatus: DueStatus = statusFromSeverity(maxSev);
    let triggeredBy: TriggeredBy | null = null;
    if (maxSev > 0) {
      if (kmSev === maxSev && timeSev === maxSev) {
        triggeredBy = 'DistanceAndTime';
      } else if (kmSev === maxSev) {
        triggeredBy = 'Distance';
      } else if (timeSev === maxSev) {
        triggeredBy = 'Time';
      } else {
        triggeredBy = 'Distance';
      }
    }

    const openOrder: MockServiceOrderRecord | undefined = this.orders.find(
      (o: MockServiceOrderRecord) =>
        o.routineId === routine.routineId && OPEN_ORDER_STATUSES.includes(o.status)
    );

    const canCreateOrder: boolean =
      routine.state === 'Active' &&
      vehicle.status === 'Active' &&
      dueStatus === 'Overdue' &&
      !openOrder;

    return {
      routineId: routine.routineId,
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      name: rType.name,
      typeCode: routine.typeCode,
      icon: rType.icon,
      state: routine.state,
      dueStatus,
      triggeredBy,
      tracks,
      nextDueKm,
      nextDueDate,
      cycleStartKm: routine.cycleStartKm,
      cycleStartDate: routine.cycleStartDate,
      intervalKm,
      intervalMonths,
      defaultIntervalKm: rType.defaultIntervalKm ?? 0,
      defaultIntervalMonths: rType.defaultIntervalMonths ?? 0,
      defaultDueSoonPercent: rType.dueSoonThresholdPercent,
      intervalKmOverride: routine.intervalKmOverride,
      intervalMonthsOverride: routine.intervalMonthsOverride,
      dueSoonPercentOverride: routine.dueSoonPercentOverride,
      effectiveDueSoonPercent,
      thresholdSource: routine.dueSoonPercentOverride !== null ? 'Routine' : 'Type',
      openServiceOrderId: openOrder?.id ?? null,
      openServiceOrderNumber: openOrder?.number ?? null,
      openServiceOrderStatus: openOrder?.status ?? null,
      canCreateOrder,
    };
  }

  private reconcileAlertsAndOrders(silent: boolean): void {
    for (const routine of this.routines) {
      const vehicle: MockVehicleDto | null = this.getVehicleByIdOrPlate(routine.vehicleId);
      if (!vehicle || routine.state !== 'Active' || vehicle.status !== 'Active') {
        continue;
      }
      const evaluated: MockEvaluatedRoutineDto = this.evaluateRoutine(routine, vehicle);

      if (evaluated.dueStatus === 'DueSoon' && evaluated.triggeredBy) {
        this.raiseAlertIfNew(routine, vehicle, evaluated.name, 'DueSoon', evaluated.triggeredBy, silent);
      }

      if (evaluated.dueStatus === 'Overdue' && evaluated.triggeredBy) {
        routine.stickyOverdue = true;
        for (const existing of this.alerts) {
          if (
            existing.routineId === routine.routineId &&
            existing.type === 'DueSoon' &&
            existing.status !== 'Resolved'
          ) {
            existing.status = 'Resolved';
            existing.resolutionReason = 'EscalatedToOverdue';
          }
        }
        this.raiseAlertIfNew(routine, vehicle, evaluated.name, 'Overdue', evaluated.triggeredBy, silent);

        const hasOpenOrder: boolean = this.orders.some(
          (o: MockServiceOrderRecord) =>
            o.routineId === routine.routineId && OPEN_ORDER_STATUSES.includes(o.status)
        );
        if (!hasOpenOrder && !routine.cycleOrderCreated) {
          routine.cycleOrderCreated = true;
          this.createOrderInternal(routine, vehicle, 'Sistema');
        }
      }

      if (evaluated.dueStatus === 'UpToDate') {
        for (const existing of this.alerts) {
          if (existing.routineId === routine.routineId && existing.status !== 'Resolved') {
            existing.status = 'Resolved';
            existing.resolutionReason = 'ParametersChanged';
          }
        }
      }
    }
  }

  private raiseAlertIfNew(
    routine: MockRoutineRecord,
    vehicle: MockVehicleDto,
    routineName: string,
    type: AlertType,
    triggeredBy: TriggeredBy,
    silent: boolean
  ): void {
    const exists: boolean = this.alerts.some(
      (a: MockAlertDto) =>
        a.routineId === routine.routineId && a.cycleNumber === routine.cycleNumber && a.type === type
    );
    if (exists) {
      return;
    }
    const alert: MockAlertDto = {
      alertId: `AL-${this.alerts.length + 1}`,
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      customerId: vehicle.customerId,
      routineId: routine.routineId,
      routineName,
      routineTypeCode: routine.typeCode,
      cycleNumber: routine.cycleNumber,
      type,
      triggeredBy,
      status: 'Active',
      raisedAt: '12/11/2026',
      acknowledgedBy: null,
      resolutionReason: null,
    };
    this.alerts.push(alert);

    if (!silent) {
      this.notifications.emitAlertChanged({
        alertId: alert.alertId,
        vehicleId: alert.vehicleId,
        plate: alert.plate,
        customerId: alert.customerId,
        routineId: alert.routineId,
        routineName: alert.routineName,
        type: alert.type,
        triggeredBy: alert.triggeredBy,
        status: alert.status,
        raisedAt: alert.raisedAt,
        acknowledgedBy: null,
        resolutionReason: null,
        occurredAt: new Date().toISOString(),
      });
    }
  }

  private createOrderInternal(
    routine: MockRoutineRecord,
    vehicle: MockVehicleDto,
    createdBy: string
  ): MockServiceOrderRecord {
    const rType: MockRoutineTypeDto = this.routineTypes.find(
      (t: MockRoutineTypeDto) => t.code === routine.typeCode
    )!;
    const orderNumber = `OS-${String(this.orderSequence).padStart(4, '0')}`;
    this.orderSequence += 1;
    this.historySequence += 1;
    const order: MockServiceOrderRecord = {
      id: orderNumber,
      number: orderNumber,
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      routineId: routine.routineId,
      routineTypeCode: routine.typeCode,
      routineName: rType.name,
      customerId: vehicle.customerId,
      workshopId: vehicle.workshopId,
      status: 'Created',
      statusBeforeHold: null,
      reason: null,
      priority: null,
      autoApproved: false,
      autoApprovalMaxAmountApplied: this.currentPolicyAmount(),
      appointmentAt: null,
      lines: [],
      history: [
        {
          id: `h-${this.historySequence}`,
          changeType: 'StatusChanged',
          detail: createdBy === 'Sistema' ? 'Creada' : 'Creada manualmente',
          changedBy: createdBy,
          changedAt: '12/11/2026 09:00',
          icon: 'note_add',
        },
      ],
      updatedAt: new Date().toISOString(),
    };
    this.orders.push(order);
    return order;
  }

  private emitRoutineUpdate(evaluated: MockEvaluatedRoutineDto, vehicle: MockVehicleDto): void {
    this.notifications.emitRoutineChanged({
      routineId: evaluated.routineId,
      vehicleId: vehicle.id,
      plate: vehicle.plate,
      customerId: vehicle.customerId,
      routineTypeCode: evaluated.typeCode,
      routineName: evaluated.name,
      state: evaluated.state,
      dueStatus: evaluated.dueStatus,
      triggeredBy: evaluated.triggeredBy,
      consumedPercent: this.maxConsumed(evaluated),
      effectiveDueSoonPercent: evaluated.effectiveDueSoonPercent,
      nextDueKm: evaluated.nextDueKm,
      nextDueDate: evaluated.nextDueDate,
      odometerKm: vehicle.odometerKm,
      occurredAt: new Date().toISOString(),
    });
  }

  private buildVehicle(
    plate: string,
    vin: string | null,
    brand: string,
    model: string,
    modelYear: number,
    customerId: string,
    workshopId: string,
    responsibleApproverId: string | null,
    odometerKm: number
  ): MockVehicleDto {
    const customer: MockCustomerDto = this.customers.find(
      (c: MockCustomerDto) => c.id === customerId
    )!;
    const workshop: MockWorkshopDto = this.workshops.find(
      (w: MockWorkshopDto) => w.id === workshopId
    )!;
    const approver: MockApproverDto | undefined = customer.approvers.find(
      (a: MockApproverDto) => a.id === responsibleApproverId
    );
    return {
      id: plate,
      plate,
      vin,
      brand,
      model,
      modelYear,
      customerId,
      customerName: customer.name,
      workshopId,
      workshopName: workshop.name,
      responsibleApproverId,
      responsibleApproverName: approver?.fullName ?? null,
      status: 'Active',
      odometerKm,
      lastReadingText: 'hace 3 min',
    };
  }

  private buildRoutine(
    plate: string,
    typeCode: string,
    cycleStartKm: number,
    cycleStartDate: string,
    intervalKmOverride: number | null = null,
    intervalMonthsOverride: number | null = null,
    dueSoonPercentOverride: number | null = null
  ): MockRoutineRecord {
    return {
      routineId: `${plate}-${typeCode}`,
      vehicleId: plate,
      plate,
      typeCode,
      state: 'Active',
      cycleNumber: 1,
      cycleStartKm,
      cycleStartDate,
      intervalKmOverride,
      intervalMonthsOverride,
      dueSoonPercentOverride,
      stickyOverdue: false,
      cycleOrderCreated: false,
    };
  }
}

