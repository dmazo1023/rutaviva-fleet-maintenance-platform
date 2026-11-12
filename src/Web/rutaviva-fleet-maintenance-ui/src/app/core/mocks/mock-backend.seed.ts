import {
  ApproverStatus,
  CustomerStatus,
  RoutineState,
  RoutineTypeStatus,
  VehicleStatus,
  WorkshopStatus,
} from '../models/enums';
import {
  approverGuid,
  customerGuid,
  ROUTINE_TYPE_IDS,
  routineGuid,
  vehicleGuid,
  workshopGuid,
} from './mock-backend.ids';

export interface MockApproverRecord {
  readonly id: string;
  readonly customerId: string;
  readonly fullName: string;
  readonly email: string;
  readonly status: ApproverStatus;
}

export interface MockCustomerRecord {
  readonly id: string;
  readonly taxId: string;
  readonly name: string;
  readonly email: string;
  readonly phone: string | null;
  readonly status: CustomerStatus;
  readonly approvers: readonly MockApproverRecord[];
}

export interface MockWorkshopRecord {
  readonly id: string;
  readonly taxId: string;
  readonly name: string;
  readonly email: string;
  readonly phone: string | null;
  readonly address: string;
  readonly city: string;
  readonly status: WorkshopStatus;
  readonly createdAt: string;
}

export interface MockVehicleRecord {
  readonly id: string;
  readonly plate: string;
  readonly vin: string | null;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly customerId: string;
  readonly workshopId: string;
  readonly responsibleApproverId: string | null;
  readonly initialOdometerKm: number;
  readonly registeredAt: string;
  odometerKm: number;
  status: VehicleStatus;
  statusChangedAt: string;
}

export interface MockRoutineTypeRecord {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  defaultIntervalKm: number | null;
  defaultIntervalMonths: number | null;
  dueSoonPercent: number;
  readonly status: RoutineTypeStatus;
  updatedBy: string;
  updatedAt: string;
}

export interface MockRoutineRecord {
  readonly routineId: string;
  readonly vehicleId: string;
  readonly typeCode: string;
  state: RoutineState;
  cycleNumber: number;
  cycleStartKm: number;
  cycleStartDate: string;
  intervalKmOverride: number | null;
  intervalMonthsOverride: number | null;
  dueSoonPercentOverride: number | null;
  stickyOverdue: boolean;
}

interface VehicleSeed {
  readonly plate: string;
  readonly vin: string | null;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly customerId: string;
  readonly workshopId: string;
  readonly responsibleApproverId: string | null;
  readonly odometerKm: number;
}

interface RoutineSeed {
  readonly plate: string;
  readonly typeCode: string;
  readonly cycleStartKm: number;
  readonly cycleStartDate: string;
  readonly intervalKmOverride?: number;
  readonly intervalMonthsOverride?: number;
  readonly dueSoonPercentOverride?: number;
}

export const SEED_TIMESTAMP = '2026-11-01T00:00:00.000Z';
export const SEED_AUTHOR = 'system';
export const SEED_TODAY = '2026-11-12';

const CUSTOMER = {
  rutaViva: customerGuid(1),
  andinos: customerGuid(2),
  elValle: customerGuid(3),
} as const;

const APPROVER = {
  laura: approverGuid(1),
  andres: approverGuid(2),
  camila: approverGuid(3),
  mateo: approverGuid(4),
  julian: approverGuid(5),
} as const;

const WORKSHOP = {
  autofrenos: workshopGuid(1),
  servicentro: workshopGuid(2),
  lubricantes: workshopGuid(3),
} as const;

export const ACKNOWLEDGER_NAME = 'Camila Torres';
export const ACKNOWLEDGED_SEED_PLATE = 'TAN202';

const VEHICLE_SEEDS: readonly VehicleSeed[] = [
  { plate: 'RVA101', vin: '9FB4SRC9AKM100101', brand: 'Renault', model: 'Kangoo', modelYear: 2021, customerId: CUSTOMER.rutaViva, workshopId: WORKSHOP.autofrenos, responsibleApproverId: APPROVER.laura, odometerKm: 52_300 },
  { plate: 'RVA102', vin: '9GACE1340NB100102', brand: 'Chevrolet', model: 'NHR', modelYear: 2022, customerId: CUSTOMER.rutaViva, workshopId: WORKSHOP.autofrenos, responsibleApproverId: null, odometerKm: 81_450 },
  { plate: 'RVA103', vin: null, brand: 'Toyota', model: 'Hilux', modelYear: 2023, customerId: CUSTOMER.rutaViva, workshopId: WORKSHOP.servicentro, responsibleApproverId: APPROVER.andres, odometerKm: 30_200 },
  { plate: 'RVA104', vin: 'JHHAFJ4H2PK100104', brand: 'Hino', model: '300', modelYear: 2023, customerId: CUSTOMER.rutaViva, workshopId: WORKSHOP.lubricantes, responsibleApproverId: APPROVER.laura, odometerKm: 98_000 },
  { plate: 'TAN201', vin: 'LVBV3JBB1NE100201', brand: 'Foton', model: 'Aumark', modelYear: 2022, customerId: CUSTOMER.andinos, workshopId: WORKSHOP.servicentro, responsibleApproverId: APPROVER.camila, odometerKm: 120_800 },
  { plate: 'TAN202', vin: null, brand: 'Nissan', model: 'Frontier', modelYear: 2024, customerId: CUSTOMER.andinos, workshopId: WORKSHOP.lubricantes, responsibleApproverId: null, odometerKm: 19_000 },
  { plate: 'TAN203', vin: 'KNCSHX71AP7100203', brand: 'Kia', model: 'K2700', modelYear: 2023, customerId: CUSTOMER.andinos, workshopId: WORKSHOP.lubricantes, responsibleApproverId: APPROVER.mateo, odometerKm: 70_100 },
  { plate: 'EVA301', vin: null, brand: 'JAC', model: 'X200', modelYear: 2026, customerId: CUSTOMER.elValle, workshopId: WORKSHOP.autofrenos, responsibleApproverId: APPROVER.julian, odometerKm: 0 },
  { plate: 'EVA302', vin: '9GAJM52348B100302', brand: 'Chevrolet', model: 'NPR', modelYear: 2020, customerId: CUSTOMER.elValle, workshopId: WORKSHOP.servicentro, responsibleApproverId: null, odometerKm: 45_500 },
  { plate: 'EVA30D', vin: null, brand: 'Yamaha', model: 'FZ 2.0', modelYear: 2024, customerId: CUSTOMER.elValle, workshopId: WORKSHOP.autofrenos, responsibleApproverId: APPROVER.julian, odometerKm: 12_500 },
];

const OIL = 'OIL_CHANGE';
const BRAKES = 'BRAKES';
const TIRES = 'TIRES';

const ROUTINE_SEEDS: readonly RoutineSeed[] = [
  { plate: 'RVA101', typeCode: OIL, cycleStartKm: 45_000, cycleStartDate: '2026-08-01' },
  { plate: 'RVA101', typeCode: BRAKES, cycleStartKm: 40_000, cycleStartDate: '2026-03-15' },
  { plate: 'RVA102', typeCode: OIL, cycleStartKm: 72_000, cycleStartDate: '2026-07-15' },
  { plate: 'RVA103', typeCode: OIL, cycleStartKm: 28_000, cycleStartDate: '2026-05-25' },
  { plate: 'RVA104', typeCode: OIL, cycleStartKm: 92_000, cycleStartDate: '2026-09-20' },
  { plate: 'RVA104', typeCode: BRAKES, cycleStartKm: 77_000, cycleStartDate: '2025-12-01' },
  { plate: 'TAN201', typeCode: OIL, cycleStartKm: 110_000, cycleStartDate: '2026-09-01' },
  { plate: 'TAN201', typeCode: BRAKES, cycleStartKm: 105_000, cycleStartDate: '2026-06-10' },
  { plate: 'TAN202', typeCode: OIL, cycleStartKm: 15_000, cycleStartDate: '2026-04-20' },
  { plate: 'TAN203', typeCode: OIL, cycleStartKm: 60_000, cycleStartDate: '2026-05-01' },
  { plate: 'EVA301', typeCode: OIL, cycleStartKm: 0, cycleStartDate: '2026-11-12' },
  { plate: 'EVA301', typeCode: BRAKES, cycleStartKm: 0, cycleStartDate: '2026-11-12' },
  { plate: 'EVA302', typeCode: BRAKES, cycleStartKm: 27_000, cycleStartDate: '2026-02-10' },
  { plate: 'EVA302', typeCode: TIRES, cycleStartKm: 10_000, cycleStartDate: '2025-08-01' },
  { plate: 'EVA30D', typeCode: OIL, cycleStartKm: 10_000, cycleStartDate: '2026-10-01', intervalKmOverride: 3_000, intervalMonthsOverride: 3, dueSoonPercentOverride: 20 },
];

function buildApprover(
  id: string,
  customerId: string,
  fullName: string,
  email: string
): MockApproverRecord {
  return { id, customerId, fullName, email, status: 'Active' };
}

export function buildCustomers(): MockCustomerRecord[] {
  return [
    {
      id: CUSTOMER.rutaViva,
      taxId: '900000001',
      name: 'RutaViva S.A.S.',
      email: 'flota@rutaviva.example',
      phone: '6045550001',
      status: 'Active',
      approvers: [
        buildApprover(APPROVER.laura, CUSTOMER.rutaViva, 'Laura Gómez', 'Laura_Gomez@rutaviva.com'),
        buildApprover(APPROVER.andres, CUSTOMER.rutaViva, 'Andrés Restrepo', 'Andres_Restrepo@rutaviva.com'),
      ],
    },
    {
      id: CUSTOMER.andinos,
      taxId: '800200300',
      name: 'Transportes Andinos S.A.',
      email: 'flota@transandinos.example',
      phone: '6045550002',
      status: 'Active',
      approvers: [
        buildApprover(APPROVER.camila, CUSTOMER.andinos, 'Camila Torres', 'Camila_Torres@rutaviva.com'),
        buildApprover(APPROVER.mateo, CUSTOMER.andinos, 'Mateo Ríos', 'Mateo_Rios@rutaviva.com'),
      ],
    },
    {
      id: CUSTOMER.elValle,
      taxId: '811400500',
      name: 'Distribuidora El Valle Ltda.',
      email: 'logistica@elvalle.example',
      phone: '6025550003',
      status: 'Active',
      approvers: [
        buildApprover(APPROVER.julian, CUSTOMER.elValle, 'Julián Ospina', 'Julian_Ospina@rutaviva.com'),
      ],
    },
  ];
}

export function buildWorkshops(): MockWorkshopRecord[] {
  return [
    {
      id: WORKSHOP.autofrenos,
      taxId: '900100001',
      name: 'Autofrenos Medellín',
      email: 'contacto@autofrenos.example',
      phone: '6044440001',
      address: 'Cra 50 # 10-20',
      city: 'Medellín',
      status: 'Active',
      createdAt: SEED_TIMESTAMP,
    },
    {
      id: WORKSHOP.servicentro,
      taxId: '900100002',
      name: 'Servicentro La 80',
      email: 'servicio@servicentro80.example',
      phone: '6044440002',
      address: 'Calle 80 # 65-10',
      city: 'Medellín',
      status: 'Active',
      createdAt: SEED_TIMESTAMP,
    },
    {
      id: WORKSHOP.lubricantes,
      taxId: '900100003',
      name: 'Lubricantes y Llantas Bogotá',
      email: 'info@lubrillantas.example',
      phone: '6014440003',
      address: 'Av 68 # 22-15',
      city: 'Bogotá',
      status: 'Active',
      createdAt: SEED_TIMESTAMP,
    },
  ];
}

function buildRoutineType(
  id: string,
  code: string,
  name: string,
  intervalKm: number,
  intervalMonths: number
): MockRoutineTypeRecord {
  return {
    id,
    code,
    name,
    defaultIntervalKm: intervalKm,
    defaultIntervalMonths: intervalMonths,
    dueSoonPercent: 10,
    status: 'Active',
    updatedBy: SEED_AUTHOR,
    updatedAt: SEED_TIMESTAMP,
  };
}

export function buildRoutineTypes(): MockRoutineTypeRecord[] {
  return [
    buildRoutineType(ROUTINE_TYPE_IDS.oilChange, OIL, 'Cambio de aceite', 10_000, 6),
    buildRoutineType(ROUTINE_TYPE_IDS.brakes, BRAKES, 'Frenos', 20_000, 12),
    buildRoutineType(ROUTINE_TYPE_IDS.tires, TIRES, 'Llantas', 40_000, 24),
  ];
}

export function buildVehicles(): MockVehicleRecord[] {
  return VEHICLE_SEEDS.map((seed: VehicleSeed, index: number) => ({
    id: vehicleGuid(index + 1),
    plate: seed.plate,
    vin: seed.vin,
    brand: seed.brand,
    model: seed.model,
    modelYear: seed.modelYear,
    customerId: seed.customerId,
    workshopId: seed.workshopId,
    responsibleApproverId: seed.responsibleApproverId,
    initialOdometerKm: seed.odometerKm,
    registeredAt: SEED_TIMESTAMP,
    odometerKm: seed.odometerKm,
    status: 'Active',
    statusChangedAt: SEED_TIMESTAMP,
  }));
}

export function buildRoutines(vehicles: readonly MockVehicleRecord[]): MockRoutineRecord[] {
  return ROUTINE_SEEDS.map((seed: RoutineSeed, index: number) => ({
    routineId: routineGuid(index + 1),
    vehicleId: vehicles.find((v: MockVehicleRecord) => v.plate === seed.plate)!.id,
    typeCode: seed.typeCode,
    state: 'Active',
    cycleNumber: 1,
    cycleStartKm: seed.cycleStartKm,
    cycleStartDate: seed.cycleStartDate,
    intervalKmOverride: seed.intervalKmOverride ?? null,
    intervalMonthsOverride: seed.intervalMonthsOverride ?? null,
    dueSoonPercentOverride: seed.dueSoonPercentOverride ?? null,
    stickyOverdue: false,
  }));
}
