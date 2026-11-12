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

  readonly dialogTitle: Signal<string> = computed(
    () => `${this.texts.deactivateTitlePrefix}${this.plate()}`
  );

  readonly confirmText: Signal<string> = computed(() =>
    this.busy() ? this.texts.deactivatingConfirm : this.texts.deactivateConfirm
  );

  readonly impactItems: Signal<readonly ImpactItemVm[]> = computed(() => {
    const orders: number = this.openOrdersCount();
    const routines: number = this.activeRoutinesCount();
    const orderText: string =
      orders === 1 ? this.texts.impactSingleOrder : `${orders}${this.texts.impactMultiOrdersSuffix}`;
    const routineText: string =
      routines === 1
        ? this.texts.impactSingleRoutine
        : `${routines}${this.texts.impactMultiRoutinesSuffix}`;

    return [
      { icon: 'pause_circle', text: orderText },
      { icon: 'event_busy', text: routineText },
      { icon: 'speed', text: this.texts.impactOdometerContinues },
    ];
  });
}

