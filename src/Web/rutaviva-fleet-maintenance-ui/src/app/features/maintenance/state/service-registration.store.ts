import { inject, Injectable, Signal, signal, WritableSignal } from '@angular/core';
import { ToastStore } from '../../../core/feedback/toast.store';
import { MaintenanceApi } from '../data/maintenance.api';
import { MAINTENANCE_TEXTS } from '../maintenance.texts';
import {
  MaintenancePlanDetailVm,
  RegisterServiceRequest,
  RoutineVm,
} from '../models/maintenance.vm';
import { PlanStore } from './plan.store';

@Injectable({
  providedIn: 'root',
})
export class ServiceRegistrationStore {
  private readonly maintenanceApi: MaintenanceApi = inject(MaintenanceApi);
  private readonly planStore: PlanStore = inject(PlanStore);
  private readonly toastStore: ToastStore = inject(ToastStore);

  private readonly routineSignal: WritableSignal<RoutineVm | null> = signal<RoutineVm | null>(null);
  private readonly busySignal: WritableSignal<boolean> = signal<boolean>(false);
  private readonly errorSignal: WritableSignal<string | null> = signal<string | null>(null);

  readonly routine: Signal<RoutineVm | null> = this.routineSignal.asReadonly();
  readonly busy: Signal<boolean> = this.busySignal.asReadonly();
  readonly errorMessage: Signal<string | null> = this.errorSignal.asReadonly();

  open(routine: RoutineVm): void {
    this.errorSignal.set(null);
    this.routineSignal.set(routine);
  }

  close(): void {
    this.routineSignal.set(null);
    this.errorSignal.set(null);
  }

  confirm(request: RegisterServiceRequest): void {
    const routine: RoutineVm | null = this.routineSignal();
    if (!routine || this.busySignal()) {
      return;
    }
    this.busySignal.set(true);
    this.maintenanceApi.registerService(routine.vehicleId, routine.routineId, request).subscribe({
      next: (plan: MaintenancePlanDetailVm | null) => this.onRegistered(plan, routine),
      error: () => this.onFailed(),
    });
  }

  private onRegistered(plan: MaintenancePlanDetailVm | null, routine: RoutineVm): void {
    this.planStore.setPlanReady(plan);
    this.planStore.markRoutineFlashed(routine.routineId);
    this.toastStore.show({ message: MAINTENANCE_TEXTS.serviceRegisteredToast });
    this.busySignal.set(false);
    this.close();
  }

  private onFailed(): void {
    this.busySignal.set(false);
    this.errorSignal.set(MAINTENANCE_TEXTS.serviceErrorMessage);
  }
}
