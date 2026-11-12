import { ChangeDetectionStrategy, Component, computed, input, InputSignal, Signal } from '@angular/core';
import { ENUM_LABELS } from '../../../../core/models/enum-labels';
import { ServiceOrderStatus } from '../../../../core/models/enums';
import { SERVICE_ORDERS_TEXTS } from '../../service-orders.texts';

export interface StepItemVm {
  readonly key: ServiceOrderStatus;
  readonly label: string;
  readonly state: 'done' | 'current' | 'upcoming';
}

const STEP_SEQUENCE: readonly ServiceOrderStatus[] = [
  'Created',
  'Quoted',
  'Approved',
  'InProgress',
  'Completed',
];

@Component({
  selector: 'app-order-stepper',
  templateUrl: './order-stepper.component.html',
  styleUrl: './order-stepper.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderStepperComponent {
  readonly status: InputSignal<ServiceOrderStatus> = input.required<ServiceOrderStatus>();
  readonly statusBeforeHold: InputSignal<ServiceOrderStatus | null> =
    input<ServiceOrderStatus | null>(null);

  readonly texts: typeof SERVICE_ORDERS_TEXTS = SERVICE_ORDERS_TEXTS;
  readonly labels = ENUM_LABELS;

  readonly isPaused: Signal<boolean> = computed(() => this.status() === 'OnHold');

  readonly pausedBannerText: Signal<string> = computed(() => {
    const prev: ServiceOrderStatus | null = this.statusBeforeHold();
    const prevLabel: string = prev ? this.labels.serviceOrderStatus[prev] : '';
    return `${this.texts.pausedBannerPrefix}${prevLabel}`;
  });

  readonly steps: Signal<readonly StepItemVm[]> = computed(() => {
    const rawStatus: ServiceOrderStatus =
      this.status() === 'OnHold' && this.statusBeforeHold()
        ? this.statusBeforeHold()!
        : this.status();

    const effectiveKey: ServiceOrderStatus =
      rawStatus === 'PendingApproval' ? 'Quoted' : rawStatus;
    const activeIdx: number = STEP_SEQUENCE.indexOf(effectiveKey);

    return STEP_SEQUENCE.map((key: ServiceOrderStatus, idx: number): StepItemVm => {
      const label: string =
        key === 'Quoted' && rawStatus === 'PendingApproval'
          ? this.labels.serviceOrderStatus.PendingApproval
          : this.labels.serviceOrderStatus[key];

      if (rawStatus === 'Completed') {
        return { key, label, state: 'done' };
      }
      if (activeIdx === -1) {
        return { key, label, state: 'upcoming' };
      }
      if (idx < activeIdx) {
        return { key, label, state: 'done' };
      }
      if (idx === activeIdx) {
        return { key, label, state: 'current' };
      }
      return { key, label, state: 'upcoming' };
    });
  });
}

