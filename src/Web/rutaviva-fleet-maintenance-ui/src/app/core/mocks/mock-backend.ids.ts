const HEX_RADIX = 16;
const GROUP_DIGITS = 8;
const INDEX_DIGITS = 12;
const GUID_VERSION_SEGMENT = '0000-4000-8000';

const CUSTOMER_GROUP = 0xc0;
const APPROVER_GROUP = 0xa0;
const WORKSHOP_GROUP = 0xb0;
const VEHICLE_GROUP = 0xd0;
const ROUTINE_GROUP = 0xe0;
const ALERT_GROUP = 0xf0;

function seedGuid(group: number, index: number): string {
  const head: string = group.toString(HEX_RADIX).padStart(GROUP_DIGITS, '0');
  const tail: string = index.toString(HEX_RADIX).padStart(INDEX_DIGITS, '0');
  return `${head}-${GUID_VERSION_SEGMENT}-${tail}`;
}

export const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function customerGuid(index: number): string {
  return seedGuid(CUSTOMER_GROUP, index);
}

export function approverGuid(index: number): string {
  return seedGuid(APPROVER_GROUP, index);
}

export function workshopGuid(index: number): string {
  return seedGuid(WORKSHOP_GROUP, index);
}

export function vehicleGuid(index: number): string {
  return seedGuid(VEHICLE_GROUP, index);
}

export function routineGuid(index: number): string {
  return seedGuid(ROUTINE_GROUP, index);
}

export function alertGuid(index: number): string {
  return seedGuid(ALERT_GROUP, index);
}

export const ROUTINE_TYPE_IDS = {
  oilChange: '7f3c1a10-0001-4000-8000-000000000001',
  brakes: '7f3c1a10-0001-4000-8000-000000000002',
  tires: '7f3c1a10-0001-4000-8000-000000000003',
} as const;
