import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
  Signal,
  signal,
  WritableSignal,
} from '@angular/core';
import { DueStatus } from '../../../../core/models/enums';
import { RoutineTrackVm } from '../../../../shared/models/routine-track.vm';
import { RoutineTrackComponent } from '../../../../shared/routine-track/routine-track.component';
import { UpdateRoutineParametersRequest } from '../../data/maintenance.api';
import { MAINTENANCE_TEXTS } from '../../maintenance.texts';
import { RoutineVm } from '../../models/maintenance.vm';

const DAYS_PER_MONTH = 30;
const PERCENT_MAX = 100;
const PERCENT_MIN = 0;
const MONTHS_MIN = 1;
const MONTHS_MAX = 120;

interface ParsedField {
  readonly valid: boolean;
  readonly value: number | null;
}

function textOf(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value);
}

function parseField(raw: string, isValid: (n: number) => boolean): ParsedField {
  if (raw === '') {
    return { valid: true, value: null };
  }
  const parsed: number = Number(raw);
  return isValid(parsed) ? { valid: true, value: parsed } : { valid: false, value: null };
}

function isPositiveInteger(n: number): boolean {
  return Number.isInteger(n) && n > 0;
}

function isMonths(n: number): boolean {
  return Number.isInteger(n) && n >= MONTHS_MIN && n <= MONTHS_MAX;
}

function isPercent(n: number): boolean {
  return Number.isFinite(n) && n >= PERCENT_MIN && n <= PERCENT_MAX;
}

function statusOf(remaining: number, consumedPercent: number, warnStartPercent: number): DueStatus {
  if (remaining <= 0) {
    return 'Overdue';
  }
  return consumedPercent >= warnStartPercent ? 'DueSoon' : 'UpToDate';
}

function positiveNumberOf(raw: string): number | null {
  const parsed: number = Number(raw.trim());
  return raw.trim() !== '' && Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

@Component({
  selector: 'app-routine-params-dialog',
  imports: [RoutineTrackComponent],
  templateUrl: './routine-params.dialog.html',
  styleUrl: './routine-params.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoutineParamsDialogComponent {
  readonly open: InputSignal<boolean> = input.required<boolean>();
  readonly routine: InputSignal<RoutineVm | null> = input<RoutineVm | null>(null);
  readonly currentOdometerKm: InputSignal<number> = input<number>(0);
  readonly busy: InputSignal<boolean> = input<boolean>(false);

  readonly saved: OutputEmitterRef<UpdateRoutineParametersRequest> =
    output<UpdateRoutineParametersRequest>();
  readonly closed: OutputEmitterRef<void> = output<void>();

  readonly texts: typeof MAINTENANCE_TEXTS = MAINTENANCE_TEXTS;

  readonly kmInput: WritableSignal<string> = signal<string>('');
  readonly monthsInput: WritableSignal<string> = signal<string>('');
  readonly percentInput: WritableSignal<string> = signal<string>('');
  readonly validationError: WritableSignal<string | null> = signal<string | null>(null);

  constructor() {
    effect(() => {
      const r: RoutineVm | null = this.routine();
      if (!r) {
        return;
      }
      this.kmInput.set(textOf(r.intervalKm));
      this.monthsInput.set(textOf(r.intervalMonths));
      this.percentInput.set(textOf(r.dueSoonPercentOverride));
      this.validationError.set(null);
    });
  }

  readonly effectiveDueSoonPercent: Signal<number> = computed(() => {
    const r: RoutineVm | null = this.routine();
    const parsed: ParsedField = parseField(this.percentInput().trim(), isPercent);
    if (parsed.valid && parsed.value !== null) {
      return parsed.value;
    }
    return r?.defaultDueSoonPercent ?? r?.effectiveDueSoonPercent ?? PERCENT_MIN;
  });

  readonly thresholdSourceText: Signal<string> = computed(() =>
    this.percentInput().trim() !== ''
      ? this.texts.thresholdFromRoutine
      : this.texts.thresholdFromType
  );

  readonly previewTracks: Signal<readonly RoutineTrackVm[]> = computed(() => {
    const r: RoutineVm | null = this.routine();
    if (!r) {
      return [];
    }
    const warnStartPercent: number = PERCENT_MAX - this.effectiveDueSoonPercent();
    return [this.previewKmTrack(r, warnStartPercent), this.previewTimeTrack(r, warnStartPercent)]
      .filter((track: RoutineTrackVm | null): track is RoutineTrackVm => track !== null);
  });

  onKmInput(event: Event): void {
    this.kmInput.set((event.target as HTMLInputElement).value);
    this.validationError.set(null);
  }

  onMonthsInput(event: Event): void {
    this.monthsInput.set((event.target as HTMLInputElement).value);
    this.validationError.set(null);
  }

  onPercentInput(event: Event): void {
    this.percentInput.set((event.target as HTMLInputElement).value);
    this.validationError.set(null);
  }

  onResetDefaults(): void {
    const r: RoutineVm | null = this.routine();
    if (!r) {
      return;
    }
    this.kmInput.set(textOf(r.defaultIntervalKm));
    this.monthsInput.set(textOf(r.defaultIntervalMonths));
    this.percentInput.set('');
    this.validationError.set(null);
  }

  onCancel(): void {
    this.closed.emit();
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    const request: UpdateRoutineParametersRequest | null = this.buildRequest();
    if (!request) {
      return;
    }
    this.validationError.set(null);
    this.saved.emit(request);
  }

  private previewKmTrack(r: RoutineVm, warnStartPercent: number): RoutineTrackVm | null {
    const intervalKm: number | null = positiveNumberOf(this.kmInput());
    if (intervalKm === null) {
      return null;
    }
    const odometer: number = this.currentOdometerKm();
    const remaining: number = r.cycleStartKm + intervalKm - odometer;
    const consumed: number = Math.max(
      0,
      Math.round(((odometer - r.cycleStartKm) / intervalKm) * PERCENT_MAX)
    );
    return {
      kind: 'km',
      consumedPercent: consumed,
      dueSoonPercent: this.effectiveDueSoonPercent(),
      remaining,
      status: statusOf(remaining, consumed, warnStartPercent),
    };
  }

  private previewTimeTrack(r: RoutineVm, warnStartPercent: number): RoutineTrackVm | null {
    const months: number | null = positiveNumberOf(this.monthsInput());
    if (months === null) {
      return null;
    }
    const totalDays: number = months * DAYS_PER_MONTH;
    const elapsedDays: number = this.elapsedDaysOf(r);
    const remaining: number = totalDays - elapsedDays;
    const consumed: number = Math.max(0, Math.round((elapsedDays / totalDays) * PERCENT_MAX));
    return {
      kind: 'time',
      consumedPercent: consumed,
      dueSoonPercent: this.effectiveDueSoonPercent(),
      remaining,
      status: statusOf(remaining, consumed, warnStartPercent),
    };
  }

  private elapsedDaysOf(r: RoutineVm): number {
    const timeTrack: RoutineTrackVm | undefined = r.tracks.find(
      (t: RoutineTrackVm) => t.kind === 'time'
    );
    if (!timeTrack || r.intervalMonths === null) {
      return 0;
    }
    return Math.round(
      (timeTrack.consumedPercent / PERCENT_MAX) * (r.intervalMonths * DAYS_PER_MONTH)
    );
  }

  private buildRequest(): UpdateRoutineParametersRequest | null {
    const km: ParsedField = parseField(this.kmInput().trim(), isPositiveInteger);
    const months: ParsedField = parseField(this.monthsInput().trim(), isMonths);
    const percent: ParsedField = parseField(this.percentInput().trim(), isPercent);
    const error: string | null = this.firstError(km, months, percent);
    if (error) {
      this.validationError.set(error);
      return null;
    }
    return {
      intervalKm: km.value,
      intervalMonths: months.value,
      dueSoonThresholdPercent: percent.value,
    };
  }

  private firstError(km: ParsedField, months: ParsedField, percent: ParsedField): string | null {
    if (km.value === null && months.value === null && km.valid && months.valid) {
      return this.texts.errorAtLeastOneInterval;
    }
    if (!km.valid) {
      return this.texts.errorInvalidKm;
    }
    if (!months.valid) {
      return this.texts.errorInvalidMonths;
    }
    return percent.valid ? null : this.texts.errorInvalidPercent;
  }
}
