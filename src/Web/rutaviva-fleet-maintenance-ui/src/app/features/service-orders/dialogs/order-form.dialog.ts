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
  readonly showDescription: boolean;
  readonly showAmount: boolean;
  readonly showAppointmentDate: boolean;
  readonly amountLabel: string;
}

export interface OrderFormSubmitEvent {
  readonly action: ServiceOrderAction;
  readonly description?: string;
  readonly amount?: number;
  readonly appointmentAt?: string;
}

export const ACTION_FORMS: Readonly<Partial<Record<ServiceOrderAction, OrderActionFormConfig>>> = {
  Quote: {
    title: SERVICE_ORDERS_TEXTS.quoteDialogTitle,
    showDescription: false,
    showAmount: true,
    showAppointmentDate: false,
    amountLabel: SERVICE_ORDERS_TEXTS.quoteAmountLabel,
  },
  ScheduleAppointment: {
    title: SERVICE_ORDERS_TEXTS.scheduleDialogTitle,
    showDescription: false,
    showAmount: false,
    showAppointmentDate: true,
    amountLabel: '',
  },
  AddAdditionalService: {
    title: SERVICE_ORDERS_TEXTS.addAdditionalDialogTitle,
    showDescription: true,
    showAmount: true,
    showAppointmentDate: false,
    amountLabel: SERVICE_ORDERS_TEXTS.additionalAmountLabel,
  },
  RequestAdditionalService: {
    title: SERVICE_ORDERS_TEXTS.requestAdditionalDialogTitle,
    showDescription: true,
    showAmount: false,
    showAppointmentDate: false,
    amountLabel: '',
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

  readonly submitted: OutputEmitterRef<OrderFormSubmitEvent> = output<OrderFormSubmitEvent>();
  readonly closed: OutputEmitterRef<void> = output<void>();

  readonly texts: typeof SERVICE_ORDERS_TEXTS = SERVICE_ORDERS_TEXTS;

  readonly descriptionInput: WritableSignal<string> = signal<string>('');
  readonly amountInput: WritableSignal<string> = signal<string>('');
  readonly dateInput: WritableSignal<string> = signal<string>('');
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
        this.descriptionInput.set('');
        this.amountInput.set('');
        this.dateInput.set('');
        this.validationError.set(null);
      }
    });
  }

  onDescriptionChange(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.descriptionInput.set(target.value);
    this.validationError.set(null);
  }

  onAmountChange(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.amountInput.set(target.value);
    this.validationError.set(null);
  }

  onDateChange(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.dateInput.set(target.value);
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

    if (cfg.showDescription && this.descriptionInput().trim() === '') {
      this.validationError.set(this.texts.validationRequiredDesc);
      return;
    }

    let parsedAmount: number | undefined;
    if (cfg.showAmount) {
      const num: number = Number(this.amountInput().trim());
      if (!Number.isFinite(num) || num <= 0) {
        this.validationError.set(this.texts.validationPositiveAmount);
        return;
      }
      parsedAmount = num;
    }

    if (cfg.showAppointmentDate && this.dateInput().trim() === '') {
      this.validationError.set(this.texts.validationRequiredDate);
      return;
    }

    this.validationError.set(null);
    this.submitted.emit({
      action: act,
      description: cfg.showDescription ? this.descriptionInput().trim() : undefined,
      amount: parsedAmount,
      appointmentAt: cfg.showAppointmentDate ? this.dateInput().trim() : undefined,
    });
  }
}
