import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
  Signal,
} from '@angular/core';
import { DueStatus, TriggeredBy } from '../../../../core/models/enums';
import { RoutineTrackComponent } from '../../../../shared/routine-track/routine-track.component';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { SwitchComponent } from '../../../../shared/switch/switch.component';
import { MAINTENANCE_TEXTS } from '../../maintenance.texts';
import { RoutineVm } from '../../models/maintenance.vm';

const NUMBER_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');
const DATE_FORMATTER: Intl.DateTimeFormat = new Intl.DateTimeFormat('es-CO', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  timeZone: 'UTC',
});
const NON_BREAKING_SPACE = ' ';
const TONE_BY_DUE_STATUS: Readonly<Record<DueStatus, string>> = {
  UpToDate: 'is-ok',
  DueSoon: 'is-warn',
  Overdue: 'is-danger',
};
const OFF_TONE = 'is-neutral';
const DATE_PREFIX_LENGTH = 10;

function formatDueDate(isoDate: string): string {
  return DATE_FORMATTER.format(new Date(`${isoDate.slice(0, DATE_PREFIX_LENGTH)}T00:00:00Z`));
}

@Component({
  selector: 'app-routine-row',
  imports: [StatusPillComponent, RoutineTrackComponent, SwitchComponent],
  templateUrl: './routine-row.component.html',
  styleUrl: './routine-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoutineRowComponent {
  readonly routine: InputSignal<RoutineVm> = input.required<RoutineVm>();
  readonly vehicleActive: InputSignal<boolean> = input<boolean>(true);
  readonly canManage: InputSignal<boolean> = input<boolean>(true);
  readonly flashed: InputSignal<boolean> = input<boolean>(false);

  readonly toggleState: OutputEmitterRef<RoutineVm> = output<RoutineVm>();
  readonly editParams: OutputEmitterRef<RoutineVm> = output<RoutineVm>();
  readonly registerService: OutputEmitterRef<RoutineVm> = output<RoutineVm>();
  readonly openOrder: OutputEmitterRef<string> = output<string>();
  readonly createOrder: OutputEmitterRef<RoutineVm> = output<RoutineVm>();

  readonly texts: typeof MAINTENANCE_TEXTS = MAINTENANCE_TEXTS;

  readonly isActive: Signal<boolean> = computed(() => this.routine().state === 'Active');

  readonly isOff: Signal<boolean> = computed(() => !this.isActive() || !this.vehicleActive());

  readonly iconTone: Signal<string> = computed(() =>
    this.isOff() ? OFF_TONE : TONE_BY_DUE_STATUS[this.routine().dueStatus]
  );

  readonly pillTrigger: Signal<TriggeredBy | null> = computed(() =>
    this.routine().dueStatus === 'UpToDate' ? null : this.routine().triggeredBy
  );

  readonly hasOwnParameters: Signal<boolean> = computed(
    () => this.routine().thresholdSource === 'Routine'
  );

  readonly nextKmText: Signal<string | null> = computed(() => {
    const km: number | null = this.routine().nextDueKm;
    return km === null
      ? null
      : `${NUMBER_FORMATTER.format(km)}${NON_BREAKING_SPACE}${this.texts.kmUnit}`;
  });

  readonly nextDateText: Signal<string | null> = computed(() => {
    const date: string | null = this.routine().nextDueDate;
    return date === null ? null : formatDueDate(date);
  });

  readonly canRegisterService: Signal<boolean> = computed(
    () => !this.isOff() && this.canManage() && this.routine().dueStatus !== 'UpToDate'
  );

  readonly canShowCreateOrder: Signal<boolean> = computed(
    () =>
      !this.isOff() &&
      this.canManage() &&
      this.routine().dueStatus !== 'UpToDate' &&
      !this.routine().openServiceOrderId
  );

  onSwitchToggle(): void {
    this.toggleState.emit(this.routine());
  }

  onEditParameters(): void {
    this.editParams.emit(this.routine());
  }

  onRegisterService(): void {
    this.registerService.emit(this.routine());
  }

  onCreateOrder(): void {
    this.createOrder.emit(this.routine());
  }

  onOpenOrder(): void {
    const orderId: string | null = this.routine().openServiceOrderId;
    if (orderId) {
      this.openOrder.emit(orderId);
    }
  }
}
