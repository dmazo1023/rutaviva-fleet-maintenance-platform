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
import { ServiceOrderAction } from '../../../core/models/enums';
import {
  ConfirmImpactDialogComponent,
  ImpactItemVm,
} from '../../../shared/confirm-impact-dialog/confirm-impact.dialog';
import { SERVICE_ORDERS_TEXTS } from '../service-orders.texts';

export interface OrderActionFormConfig {
  readonly title: string;
  readonly amountLabel: string;
  readonly invalidAmountMessage: string;
}

export interface OrderFormSubmitEvent {
  readonly action: ServiceOrderAction;
  readonly amount?: number;
}

export const ACTION_FORMS: Readonly<Partial<Record<ServiceOrderAction, OrderActionFormConfig>>> = {
  Quote: {
    title: SERVICE_ORDERS_TEXTS.quoteDialogTitle,
    amountLabel: SERVICE_ORDERS_TEXTS.quoteAmountLabel,
    invalidAmountMessage: SERVICE_ORDERS_TEXTS.validationPositiveAmount,
  },
  Complete: {
    title: SERVICE_ORDERS_TEXTS.completeDialogTitle,
    amountLabel: SERVICE_ORDERS_TEXTS.completeKmLabel,
    invalidAmountMessage: SERVICE_ORDERS_TEXTS.validationPositiveKm,
  },
};

export const ACTION_IMPACT: Readonly<
  Partial<Record<ServiceOrderAction, { readonly title: string; readonly items: readonly ImpactItemVm[] }>>
> = {
  Reject: {
    title: SERVICE_ORDERS_TEXTS.rejectConfirmTitle,
    items: [
      { icon: 'block', text: SERVICE_ORDERS_TEXTS.rejectImpactOrderCancelled },
      { icon: 'replay', text: SERVICE_ORDERS_TEXTS.rejectImpactRoutineOpen },
    ],
  },
  Cancel: {
    title: SERVICE_ORDERS_TEXTS.cancelConfirmTitle,
    items: [
      { icon: 'block', text: SERVICE_ORDERS_TEXTS.cancelImpactOrderCancelled },
      { icon: 'replay', text: SERVICE_ORDERS_TEXTS.cancelImpactRoutineOpen },
    ],
  },
};

function toInputValue(amount: number | null): string {
  return amount === null ? '' : String(amount);
}

@Component({
  selector: 'app-order-form-dialog',
  imports: [ConfirmImpactDialogComponent],
  templateUrl: './order-form.dialog.html',
  styleUrl: './order-form.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderFormDialogComponent {
  readonly open: InputSignal<boolean> = input.required<boolean>();
  readonly action: InputSignal<ServiceOrderAction | null> = input<ServiceOrderAction | null>(null);
  readonly busy: InputSignal<boolean> = input<boolean>(false);
  readonly initialAmount: InputSignal<number | null> = input<number | null>(null);

  readonly submitted: OutputEmitterRef<OrderFormSubmitEvent> = output<OrderFormSubmitEvent>();
  readonly closed: OutputEmitterRef<void> = output<void>();

  readonly texts: typeof SERVICE_ORDERS_TEXTS = SERVICE_ORDERS_TEXTS;

  readonly amountInput: WritableSignal<string> = signal<string>('');
  readonly validationError: WritableSignal<string | null> = signal<string | null>(null);

  readonly formConfig: Signal<OrderActionFormConfig | null> = computed(() => {
    const act: ServiceOrderAction | null = this.action();
    if (!act) {
      return null;
    }
    return ACTION_FORMS[act] ?? null;
  });

  readonly impactConfig: Signal<{ readonly title: string; readonly items: readonly ImpactItemVm[] } | null> =
    computed(() => {
      const act: ServiceOrderAction | null = this.action();
      if (!act) {
        return null;
      }
      return ACTION_IMPACT[act] ?? null;
    });

  constructor() {
    effect(() => {
      if (this.open()) {
        this.amountInput.set(toInputValue(this.initialAmount()));
        this.validationError.set(null);
      }
    });
  }

  onAmountChange(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.amountInput.set(target.value);
    this.validationError.set(null);
  }

  onImpactConfirmed(): void {
    const act: ServiceOrderAction | null = this.action();
    if (!act) {
      return;
    }
    this.submitted.emit({ action: act });
  }

  onCancel(): void {
    this.closed.emit();
  }

  onSubmitForm(event: Event): void {
    event.preventDefault();
    const act: ServiceOrderAction | null = this.action();
    const cfg: OrderActionFormConfig | null = this.formConfig();
    if (!act || !cfg) {
      return;
    }

    const amount: number = Number(this.amountInput().trim());
    if (!Number.isFinite(amount) || amount <= 0) {
      this.validationError.set(cfg.invalidAmountMessage);
      return;
    }

    this.validationError.set(null);
    this.submitted.emit({ action: act, amount });
  }
}
