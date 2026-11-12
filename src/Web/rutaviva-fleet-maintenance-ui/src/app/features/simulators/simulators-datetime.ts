const BOGOTA_UTC_OFFSET = '-05:00';
const DATETIME_LOCAL_LENGTH = 16;
const MINUTES_ONLY_SUFFIX = ':00';
const ISO_DATE_LENGTH = 10;
const MS_PER_MINUTE = 60_000;

export function toBogotaIso(dateTimeLocal: string): string {
  const trimmed: string = dateTimeLocal.trim();
  const withSeconds: string =
    trimmed.length === DATETIME_LOCAL_LENGTH ? `${trimmed}${MINUTES_ONLY_SUFFIX}` : trimmed;
  return `${withSeconds}${BOGOTA_UTC_OFFSET}`;
}

function toLocalIso(now: Date): string {
  return new Date(now.getTime() - now.getTimezoneOffset() * MS_PER_MINUTE).toISOString();
}

export function currentDateTimeLocal(now: Date = new Date()): string {
  return toLocalIso(now).slice(0, DATETIME_LOCAL_LENGTH);
}

export function currentDateInput(now: Date = new Date()): string {
  return toLocalIso(now).slice(0, ISO_DATE_LENGTH);
}
