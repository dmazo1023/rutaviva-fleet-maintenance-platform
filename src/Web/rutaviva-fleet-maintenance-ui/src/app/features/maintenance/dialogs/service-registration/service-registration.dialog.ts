import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
  signal,
  WritableSignal,
} from '@angular/core';
import { MAINTENANCE_TEXTS } from '../../maintenance.texts';
import { RegisterServiceRequest, RoutineVm } from '../../models/maintenance.vm';

export const SERVICE_TIMEZONE_OFFSET = '-05:00';
export const SERVICE_TIME_OF_DAY = '00:00:00';
const DATE_LOCALE = 'sv-SE';
const MIN_SERVICE_KM = 0;

export function todayIsoDate(): string {
  return new Date().toLocaleDateString(DATE_LOCALE);
}

export function toServicedAt(isoDate: string): string {
  return `${isoDate}T${SERVICE_TIME_OF_DAY}${SERVICE_TIMEZONE_OFFSET}`;
}

@Component({
  selector: 'app-service-registration-dialog',
  templateUrl: './service-registration.dialog.html',
  styleUrl: './service-registration.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceRegistrationDialogComponent {
  readonly routine: InputSignal<RoutineVm | null> = input<RoutineVm | null>(null);
  readonly planOdometerKm: InputSignal<number> = input<number>(0);
  readonly busy: InputSignal<boolean> = input<boolean>(false);
  readonly serverError: InputSignal<string | null> = input<string | null>(null);

  readonly confirmed: OutputEmitterRef<RegisterServiceRequest> = output<RegisterServiceRequest>();
  readonly closed: OutputEmitterRef<void> = output<void>();

  readonly texts: typeof MAINTENANCE_TEXTS = MAINTENANCE_TEXTS;

  readonly kmInput: WritableSignal<string> = signal<string>('');
  readonly dateInput: WritableSignal<string> = signal<string>('');
  readonly validationError: WritableSignal<string | null> = signal<string | null>(null);

  constructor() {
    effect(() => {
      if (!this.routine()) {
        return;
      }
      this.kmInput.set(String(this.planOdometerKm()));
      this.dateInput.set(todayIsoDate());
      this.validationError.set(null);
    });
  }

  onKmInput(event: Event): void {
    this.kmInput.set((event.target as HTMLInputElement).value);
    this.validationError.set(null);
  }

  onDateInput(event: Event): void {
    this.dateInput.set((event.target as HTMLInputElement).value);
    this.validationError.set(null);
  }

  onCancel(): void {
    this.closed.emit();
  }

  onSubmit(event: Event): void {
    event.preventDefault();
    const error: string | null = this.validate();
    this.validationError.set(error);
    if (error) {
      return;
    }
    this.confirmed.emit({
      servicedKm: Number(this.kmInput().trim()),
      servicedAt: toServicedAt(this.dateInput()),
    });
  }

  private validate(): string | null {
    const raw: string = this.kmInput().trim();
    const km: number = Number(raw);
    if (raw === '' || !Number.isInteger(km) || km < MIN_SERVICE_KM) {
      return this.texts.errorInvalidServiceKm;
    }
    return this.dateInput() === '' ? this.texts.errorInvalidServiceDate : null;
  }
}
