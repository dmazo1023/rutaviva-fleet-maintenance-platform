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
const MONTHS_MAX = 120;

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
      this.kmInput.set(
        r.intervalKmOverride !== null
          ? String(r.intervalKmOverride)
          : r.intervalKm !== null
            ? String(r.intervalKm)
            : String(r.defaultIntervalKm)
      );
      this.monthsInput.set(
        r.intervalMonthsOverride !== null
          ? String(r.intervalMonthsOverride)
          : r.intervalMonths !== null
            ? String(r.intervalMonths)
            : String(r.defaultIntervalMonths)
      );
      this.percentInput.set(
        r.dueSoonPercentOverride !== null ? String(r.dueSoonPercentOverride) : ''
      );
      this.validationError.set(null);
    });
  }

  readonly effectiveDueSoonPercent: Signal<number> = computed(() => {
    const r: RoutineVm | null = this.routine();
    const raw: string = this.percentInput().trim();
    if (raw !== '') {
      const parsed: number = Number(raw);
      if (Number.isFinite(parsed) && parsed >= 0 && parsed <= PERCENT_MAX) {
        return parsed;
      }
    }
    return r?.defaultDueSoonPercent ?? 10;
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
    const tracks: RoutineTrackVm[] = [];
    const dueSoonPct: number = this.effectiveDueSoonPercent();
    const warnStartPct: number = PERCENT_MAX - dueSoonPct;

    const kmVal: number = Number(this.kmInput().trim());
    if (this.kmInput().trim() !== '' && Number.isFinite(kmVal) && kmVal > 0) {
      const targetKm: number = r.cycleStartKm + kmVal;
      const remainingKm: number = targetKm - this.currentOdometerKm();
      const consumedKm: number = Math.max(
        0,
        Math.round(((this.currentOdometerKm() - r.cycleStartKm) / kmVal) * PERCENT_MAX)
      );
      const kmStatus: DueStatus =
        remainingKm <= 0
          ? 'Overdue'
          : consumedKm >= warnStartPct
            ? 'DueSoon'
            : 'UpToDate';
      tracks.push({
        kind: 'km',
        consumedPercent: consumedKm,
        dueSoonPercent: dueSoonPct,
        remaining: remainingKm,
        status: kmStatus,
      });
    }

    const monthsVal: number = Number(this.monthsInput().trim());
    if (this.monthsInput().trim() !== '' && Number.isFinite(monthsVal) && monthsVal > 0) {
      const totalDays: number = monthsVal * DAYS_PER_MONTH;
      const existingTimeTrack: RoutineTrackVm | undefined = r.tracks.find(
        (t: RoutineTrackVm) => t.kind === 'time'
      );
      const baseElapsedDays: number = existingTimeTrack
        ? Math.round((existingTimeTrack.consumedPercent / PERCENT_MAX) * ((r.intervalMonths ?? r.defaultIntervalMonths) * DAYS_PER_MONTH))
        : 30;
      const remainingDays: number = totalDays - baseElapsedDays;
      const consumedTime: number = Math.max(
        0,
        Math.round((baseElapsedDays / totalDays) * PERCENT_MAX)
      );
      const timeStatus: DueStatus =
        remainingDays <= 0
          ? 'Overdue'
          : consumedTime >= warnStartPct
            ? 'DueSoon'
            : 'UpToDate';
      tracks.push({
        kind: 'time',
        consumedPercent: consumedTime,
        dueSoonPercent: dueSoonPct,
        remaining: remainingDays,
        status: timeStatus,
      });
    }

    return tracks;
  });

  onKmInput(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.kmInput.set(target.value);
    this.validationError.set(null);
  }

  onMonthsInput(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.monthsInput.set(target.value);
    this.validationError.set(null);
  }

  onPercentInput(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.percentInput.set(target.value);
    this.validationError.set(null);
  }

  onResetDefaults(): void {
    const r: RoutineVm | null = this.routine();
    if (!r) {
      return;
    }
    this.kmInput.set(String(r.defaultIntervalKm));
    this.monthsInput.set(String(r.defaultIntervalMonths));
    this.percentInput.set('');
    this.validationError.set(null);
  }

  onCancel(): void {
    this.closed.emit();
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    const rawKm: string = this.kmInput().trim();
    const rawMonths: string = this.monthsInput().trim();
    const rawPercent: string = this.percentInput().trim();

    if (rawKm === '' && rawMonths === '') {
      this.validationError.set(this.texts.errorAtLeastOneInterval);
      return;
    }

    let intervalKm: number | null = null;
    if (rawKm !== '') {
      const parsedKm: number = Number(rawKm);
      if (!Number.isInteger(parsedKm) || parsedKm <= 0) {
        this.validationError.set(this.texts.errorInvalidKm);
        return;
      }
      intervalKm = parsedKm;
    }

    let intervalMonths: number | null = null;
    if (rawMonths !== '') {
      const parsedMonths: number = Number(rawMonths);
      if (!Number.isInteger(parsedMonths) || parsedMonths < 1 || parsedMonths > MONTHS_MAX) {
        this.validationError.set(this.texts.errorInvalidMonths);
        return;
      }
      intervalMonths = parsedMonths;
    }

    let dueSoonThresholdPercent: number | null = null;
    if (rawPercent !== '') {
      const parsedPct: number = Number(rawPercent);
      if (!Number.isFinite(parsedPct) || parsedPct < 0 || parsedPct > PERCENT_MAX) {
        this.validationError.set(this.texts.errorInvalidPercent);
        return;
      }
      dueSoonThresholdPercent = parsedPct;
    }

    this.validationError.set(null);
    this.saved.emit({
      intervalKm,
      intervalMonths,
      dueSoonThresholdPercent,
    });
  }
}

