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
import { RoutineTrackComponent } from '../../../../shared/routine-track/routine-track.component';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { SwitchComponent } from '../../../../shared/switch/switch.component';
import { MAINTENANCE_TEXTS } from '../../maintenance.texts';
import { RoutineVm } from '../../models/maintenance.vm';

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

  readonly texts: typeof MAINTENANCE_TEXTS = MAINTENANCE_TEXTS;

  readonly isActive: Signal<boolean> = computed(() => this.routine().state === 'Active');

  readonly canRegisterService: Signal<boolean> = computed(
    () =>
      this.isActive() &&
      this.vehicleActive() &&
      this.canManage() &&
      this.routine().dueStatus !== 'UpToDate'
  );

  readonly canShowCreateOrder: Signal<boolean> = computed(
    () => this.isActive() && this.vehicleActive() && this.canManage()
  );

  readonly thresholdLabel: Signal<string> = computed(() => {
    const r: RoutineVm = this.routine();
    const sourceLabel: string =
      r.thresholdSource === 'Routine'
        ? this.texts.thresholdFromRoutine
        : this.texts.thresholdFromType;
    return `${r.effectiveDueSoonPercent}% · ${sourceLabel}`;
  });

  onSwitchToggle(): void {
    this.toggleState.emit(this.routine());
  }

  onEditParameters(): void {
    this.editParams.emit(this.routine());
  }

  onRegisterService(): void {
    this.registerService.emit(this.routine());
  }

  onOpenOrder(): void {
    const orderId: string | null = this.routine().openServiceOrderId;
    if (orderId) {
      this.openOrder.emit(orderId);
    }
  }
}
