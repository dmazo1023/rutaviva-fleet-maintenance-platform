import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DueStatus } from '../../core/models/enums';
import { RoutineTrackVm } from '../models/routine-track.vm';

const PERCENT_MIN = 0;
const PERCENT_MAX = 100;
const NUMBER_FORMATTER = new Intl.NumberFormat('es-CO');

export const ROUTINE_TRACK_TEXTS = {
  kmMeterLabel: 'Kilometraje del ciclo',
  timeMeterLabel: 'Tiempo del ciclo',
  kmUnit: 'km',
  daysUnit: 'd',
} as const;

const TONE_BY_STATUS: Readonly<Record<DueStatus, string>> = {
  UpToDate: 'is-ok',
  DueSoon: 'is-warn',
  Overdue: 'is-danger',
};

@Component({
  selector: 'app-routine-track',
  templateUrl: './routine-track.component.html',
  styleUrl: './routine-track.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoutineTrackComponent {
  readonly track = input.required<RoutineTrackVm>();
  readonly inactive = input<boolean>(false);

  protected readonly iconName = computed<string>(() =>
    this.track().kind === 'km' ? 'speed' : 'event'
  );

  protected readonly toneClass = computed<string>(() => {
    if (this.inactive()) {
      return 'trk is-neutral is-inactive';
    }
    return `trk ${TONE_BY_STATUS[this.track().status]}`;
  });

  protected readonly clampedPercent = computed<number>(() => {
    const raw: number = this.track().consumedPercent;
    return Math.min(PERCENT_MAX, Math.max(PERCENT_MIN, raw));
  });

  protected readonly scaleXTransform = computed<string>(
    () => `scaleX(${this.clampedPercent() / PERCENT_MAX})`
  );

  protected readonly zoneLeftPercent = computed<number>(() => {
    const threshold: number = Math.min(
      PERCENT_MAX,
      Math.max(PERCENT_MIN, this.track().dueSoonPercent)
    );
    return PERCENT_MAX - threshold;
  });

  protected readonly zoneWidthPercent = computed<number>(() =>
    Math.min(PERCENT_MAX, Math.max(PERCENT_MIN, this.track().dueSoonPercent))
  );

  protected readonly roundedValueNow = computed<number>(() =>
    Math.round(this.track().consumedPercent)
  );

  protected readonly meterAriaLabel = computed<string>(() => {
    const prefix: string =
      this.track().kind === 'km'
        ? ROUTINE_TRACK_TEXTS.kmMeterLabel
        : ROUTINE_TRACK_TEXTS.timeMeterLabel;
    return `${prefix}: ${this.roundedValueNow()} %`;
  });

  protected readonly isNegativeRemaining = computed<boolean>(() => this.track().remaining < 0);

  protected readonly formattedRemaining = computed<string>(() => {
    const rem: number = this.track().remaining;
    const absFormatted: string = NUMBER_FORMATTER.format(Math.abs(rem));
    const sign: string = rem < 0 ? '−' : '';
    const unit: string =
      this.track().kind === 'km' ? ROUTINE_TRACK_TEXTS.kmUnit : ROUTINE_TRACK_TEXTS.daysUnit;
    return `${sign}${absFormatted}\u00A0${unit}`;
  });
}

