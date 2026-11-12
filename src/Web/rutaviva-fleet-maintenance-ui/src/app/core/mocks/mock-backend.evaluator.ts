import { AlertTrigger, DueStatus } from '../models/enums';
import { addMonths, daysBetween, formatDateIso, parseIsoDate } from './mock-backend.dates';
import {
  ParameterSourceDto,
  PlanSummaryDto,
  RoutineDetailDto,
  RoutineTrackDto,
} from './mock-backend.dtos';
import { MockRoutineRecord, MockRoutineTypeRecord } from './mock-backend.seed';

const FULL_PERCENT = 100;
const ROUNDING_FACTOR = 100;
const SEVERITY_OVERDUE = 2;
const SEVERITY_DUE_SOON = 1;
const SEVERITY_UP_TO_DATE = 0;

interface TrackOutcome<TNext> {
  readonly track: RoutineTrackDto;
  readonly next: TNext;
}

export interface EvaluationContext {
  readonly odometerKm: number;
  readonly todayIso: string;
}

export function severityOf(status: DueStatus): number {
  if (status === 'Overdue') {
    return SEVERITY_OVERDUE;
  }
  return status === 'DueSoon' ? SEVERITY_DUE_SOON : SEVERITY_UP_TO_DATE;
}

function statusOfSeverity(severity: number): DueStatus {
  if (severity >= SEVERITY_OVERDUE) {
    return 'Overdue';
  }
  return severity === SEVERITY_DUE_SOON ? 'DueSoon' : 'UpToDate';
}

function statusOfConsumption(consumedPercent: number, dueSoonPercent: number): DueStatus {
  if (consumedPercent >= FULL_PERCENT) {
    return 'Overdue';
  }
  return consumedPercent >= FULL_PERCENT - dueSoonPercent ? 'DueSoon' : 'UpToDate';
}

function roundPercent(value: number): number {
  return Math.round(value * ROUNDING_FACTOR) / ROUNDING_FACTOR;
}

function sourceOf(override: number | null): ParameterSourceDto {
  return override === null ? 'RoutineType' : 'Override';
}

function buildDistance(
  routine: MockRoutineRecord,
  intervalKm: number | null,
  dueSoon: number,
  odometerKm: number
): TrackOutcome<number> | null {
  if (intervalKm === null || intervalKm <= 0) {
    return null;
  }
  const nextDueKm: number = routine.cycleStartKm + intervalKm;
  const consumed: number = Math.max(0, ((odometerKm - routine.cycleStartKm) / intervalKm) * FULL_PERCENT);
  const track: RoutineTrackDto = {
    kind: 'km',
    consumedPercent: roundPercent(consumed),
    dueSoonPercent: dueSoon,
    remaining: nextDueKm - odometerKm,
    status: statusOfConsumption(consumed, dueSoon),
  };
  return { track, next: nextDueKm };
}

function buildTime(
  routine: MockRoutineRecord,
  intervalMonths: number | null,
  dueSoon: number,
  todayIso: string
): TrackOutcome<string> | null {
  if (intervalMonths === null || intervalMonths <= 0) {
    return null;
  }
  const start: Date = parseIsoDate(routine.cycleStartDate);
  const due: Date = addMonths(routine.cycleStartDate, intervalMonths);
  const today: Date = parseIsoDate(todayIso);
  const totalDays: number = Math.max(1, daysBetween(start, due));
  const consumed: number = Math.max(0, (daysBetween(start, today) / totalDays) * FULL_PERCENT);
  const track: RoutineTrackDto = {
    kind: 'time',
    consumedPercent: roundPercent(consumed),
    dueSoonPercent: dueSoon,
    remaining: Math.round(daysBetween(today, due)),
    status: statusOfConsumption(consumed, dueSoon),
  };
  return { track, next: formatDateIso(due) };
}

function resolveTrigger(
  distance: DueStatus | null,
  time: DueStatus | null,
  worstSeverity: number
): AlertTrigger | null {
  if (worstSeverity === SEVERITY_UP_TO_DATE) {
    return null;
  }
  const byDistance: boolean = distance !== null && severityOf(distance) === worstSeverity;
  const byTime: boolean = time !== null && severityOf(time) === worstSeverity;
  if (byDistance && byTime) {
    return 'DistanceAndTime';
  }
  return byTime ? 'Time' : 'Distance';
}

function worstSeverityOf(
  routine: MockRoutineRecord,
  distance: RoutineTrackDto | null,
  time: RoutineTrackDto | null
): number {
  const tracked: number = Math.max(
    distance ? severityOf(distance.status) : SEVERITY_UP_TO_DATE,
    time ? severityOf(time.status) : SEVERITY_UP_TO_DATE
  );
  return routine.stickyOverdue ? Math.max(tracked, SEVERITY_OVERDUE) : tracked;
}

export function evaluateRoutine(
  routine: MockRoutineRecord,
  type: MockRoutineTypeRecord,
  context: EvaluationContext
): RoutineDetailDto {
  const intervalKm: number | null = routine.intervalKmOverride ?? type.defaultIntervalKm;
  const intervalMonths: number | null = routine.intervalMonthsOverride ?? type.defaultIntervalMonths;
  const dueSoon: number = routine.dueSoonPercentOverride ?? type.dueSoonPercent;
  const distance: TrackOutcome<number> | null = buildDistance(routine, intervalKm, dueSoon, context.odometerKm);
  const time: TrackOutcome<string> | null = buildTime(routine, intervalMonths, dueSoon, context.todayIso);
  const worst: number = worstSeverityOf(routine, distance?.track ?? null, time?.track ?? null);

  return {
    routineId: routine.routineId,
    routineTypeCode: type.code,
    name: type.name,
    state: routine.state,
    dueStatus: statusOfSeverity(worst),
    triggeredBy: resolveTrigger(distance?.track.status ?? null, time?.track.status ?? null, worst),
    cycleNumber: routine.cycleNumber,
    cycleStartKm: routine.cycleStartKm,
    cycleStartDate: routine.cycleStartDate,
    intervalKm,
    intervalKmSource: sourceOf(routine.intervalKmOverride),
    intervalMonths,
    intervalMonthsSource: sourceOf(routine.intervalMonthsOverride),
    dueSoonPercent: dueSoon,
    dueSoonPercentSource: sourceOf(routine.dueSoonPercentOverride),
    nextDueKm: distance?.next ?? null,
    nextDueDate: time?.next ?? null,
    consumedPercent: Math.max(distance?.track.consumedPercent ?? 0, time?.track.consumedPercent ?? 0),
    distanceTrack: distance?.track ?? null,
    timeTrack: time?.track ?? null,
  };
}

function countByStatus(routines: readonly RoutineDetailDto[], status: DueStatus): number {
  return routines.filter((r: RoutineDetailDto) => r.state === 'Active' && r.dueStatus === status).length;
}

function worstStatusOf(active: readonly RoutineDetailDto[]): DueStatus | null {
  if (active.length === 0) {
    return null;
  }
  const worst: number = Math.max(...active.map((r: RoutineDetailDto) => severityOf(r.dueStatus)));
  return statusOfSeverity(worst);
}

export function summarizePlan(
  header: Pick<PlanSummaryDto, 'vehicleId' | 'plate' | 'planStatus' | 'odometerKm'>,
  routines: readonly RoutineDetailDto[]
): PlanSummaryDto {
  const active: RoutineDetailDto[] = routines.filter((r: RoutineDetailDto) => r.state === 'Active');
  return {
    ...header,
    worstDueStatus: worstStatusOf(active),
    maxConsumedPercent: Math.max(0, ...active.map((r: RoutineDetailDto) => r.consumedPercent)),
    upToDateCount: countByStatus(routines, 'UpToDate'),
    dueSoonCount: countByStatus(routines, 'DueSoon'),
    overdueCount: countByStatus(routines, 'Overdue'),
    inactiveCount: routines.length - active.length,
  };
}
