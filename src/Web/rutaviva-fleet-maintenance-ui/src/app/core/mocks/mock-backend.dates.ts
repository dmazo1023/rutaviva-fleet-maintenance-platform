const MS_PER_DAY = 86_400_000;
const ISO_DATE_LENGTH = 10;
const MONTH_OFFSET = 1;
const PAD_WIDTH = 2;

export function parseIsoDate(iso: string): Date {
  const parts: string[] = iso.slice(0, ISO_DATE_LENGTH).split('-');
  return new Date(Number(parts[0]), Number(parts[1]) - MONTH_OFFSET, Number(parts[2]));
}

export function formatDateIso(date: Date): string {
  const year: number = date.getFullYear();
  const month: string = String(date.getMonth() + MONTH_OFFSET).padStart(PAD_WIDTH, '0');
  const day: string = String(date.getDate()).padStart(PAD_WIDTH, '0');
  return `${year}-${month}-${day}`;
}

export function addMonths(startIso: string, months: number): Date {
  const start: Date = parseIsoDate(startIso);
  const day: number = start.getDate();
  const result: Date = new Date(start.getFullYear(), start.getMonth(), day);
  result.setMonth(result.getMonth() + months);
  if (result.getDate() < day) {
    result.setDate(0);
  }
  return result;
}

export function daysBetween(from: Date, to: Date): number {
  return (to.getTime() - from.getTime()) / MS_PER_DAY;
}

export function dateOnly(isoDateTime: string): string {
  return isoDateTime.slice(0, ISO_DATE_LENGTH);
}
