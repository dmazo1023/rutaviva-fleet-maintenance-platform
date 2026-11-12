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
import {
  ConfirmImpactDialogComponent,
  ImpactItemVm,
} from '../../../../shared/confirm-impact-dialog/confirm-impact.dialog';
import { FLEET_TEXTS } from '../../fleet.texts';
import { DEACTIVATE_VEHICLE_TEXTS } from './deactivate-vehicle.texts';

@Component({
  selector: 'app-deactivate-vehicle-dialog',
  imports: [ConfirmImpactDialogComponent],
  templateUrl: './deactivate-vehicle.dialog.html',
  styleUrl: './deactivate-vehicle.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeactivateVehicleDialogComponent {
  readonly open: InputSignal<boolean> = input.required<boolean>();
  readonly plate: InputSignal<string> = input.required<string>();
  readonly openOrdersCount: InputSignal<number> = input<number>(0);
  readonly activeRoutinesCount: InputSignal<number> = input<number>(0);
  readonly busy: InputSignal<boolean> = input<boolean>(false);

  readonly confirmed: OutputEmitterRef<void> = output<void>();
  readonly closed: OutputEmitterRef<void> = output<void>();

  readonly texts: typeof FLEET_TEXTS = FLEET_TEXTS;
  readonly dialogTexts: typeof DEACTIVATE_VEHICLE_TEXTS = DEACTIVATE_VEHICLE_TEXTS;

  readonly dialogTitle: Signal<string> = computed(
    () => `${this.texts.deactivateTitlePrefix}${this.plate()}`
  );

  readonly confirmText: Signal<string> = computed(() =>
    this.busy() ? this.texts.deactivatingConfirm : this.texts.deactivateConfirm
  );

  readonly impactItems: Signal<readonly ImpactItemVm[]> = computed(() => {
    const orderItems: readonly ImpactItemVm[] =
      this.openOrdersCount() > 0 ? [{ icon: 'assignment', text: this.ordersText() }] : [];
    return [
      ...orderItems,
      { icon: 'event_busy', text: this.routinesText() },
      { icon: 'speed', text: this.texts.impactOdometerContinues },
    ];
  });

  private ordersText(): string {
    const orders: number = this.openOrdersCount();
    return orders === 1
      ? this.texts.impactSingleOrder
      : `${orders}${this.texts.impactMultiOrdersSuffix}`;
  }

  private routinesText(): string {
    const routines: number = this.activeRoutinesCount();
    return routines === 1
      ? this.texts.impactSingleRoutine
      : `${routines}${this.texts.impactMultiRoutinesSuffix}`;
  }
}
