import { Injectable, Signal, signal, WritableSignal } from '@angular/core';
import {
  createErrorLoadable,
  createIdleLoadable,
  createLoadingLoadable,
  createReadyLoadable,
  Loadable,
} from '../../../shared/state/loadable';
import { VehicleServiceOrderSummaryDto } from '../../fleet/data/fleet.api';
import { MaintenancePlanDetailVm, OdometerReadingVm, RoutineVm } from '../models/maintenance.vm';

@Injectable({
  providedIn: 'root',
})
export class PlanStore {
  private readonly planLoadableSignal: WritableSignal<Loadable<MaintenancePlanDetailVm | null>> =
    signal<Loadable<MaintenancePlanDetailVm | null>>(createIdleLoadable(null));
  private readonly readingsLoadableSignal: WritableSignal<Loadable<readonly OdometerReadingVm[]>> =
    signal<Loadable<readonly OdometerReadingVm[]>>(createIdleLoadable([]));
  private readonly ordersLoadableSignal: WritableSignal<
    Loadable<readonly VehicleServiceOrderSummaryDto[]>
  > = signal<Loadable<readonly VehicleServiceOrderSummaryDto[]>>(createIdleLoadable([]));
  private readonly flashedRoutineIdsSignal: WritableSignal<ReadonlySet<string>> =
    signal<ReadonlySet<string>>(new Set<string>());

  readonly planLoadable: Signal<Loadable<MaintenancePlanDetailVm | null>> =
    this.planLoadableSignal.asReadonly();
  readonly readingsLoadable: Signal<Loadable<readonly OdometerReadingVm[]>> =
    this.readingsLoadableSignal.asReadonly();
  readonly ordersLoadable: Signal<Loadable<readonly VehicleServiceOrderSummaryDto[]>> =
    this.ordersLoadableSignal.asReadonly();
  readonly flashedRoutineIds: Signal<ReadonlySet<string>> =
    this.flashedRoutineIdsSignal.asReadonly();

  setPlanLoading(): void {
    this.planLoadableSignal.update((prev: Loadable<MaintenancePlanDetailVm | null>) =>
      createLoadingLoadable(prev.data)
    );
  }

  setPlanReady(plan: MaintenancePlanDetailVm | null): void {
    this.planLoadableSignal.set(createReadyLoadable(plan));
  }

  setPlanError(message: string): void {
    this.planLoadableSignal.update((prev: Loadable<MaintenancePlanDetailVm | null>) =>
      createErrorLoadable(prev.data, message)
    );
  }

  replaceRoutine(updated: RoutineVm): void {
    this.planLoadableSignal.update((prev: Loadable<MaintenancePlanDetailVm | null>) => {
      if (!prev.data) {
        return prev;
      }
      const nextRoutines: readonly RoutineVm[] = prev.data.routines.map((r: RoutineVm) =>
        r.routineId === updated.routineId ? updated : r
      );
      return createReadyLoadable({
        ...prev.data,
        routines: nextRoutines,
      });
    });
  }

  setReadingsLoading(): void {
    this.readingsLoadableSignal.update((prev: Loadable<readonly OdometerReadingVm[]>) =>
      createLoadingLoadable(prev.data)
    );
  }

  setReadingsReady(readings: readonly OdometerReadingVm[]): void {
    this.readingsLoadableSignal.set(createReadyLoadable(readings));
  }

  setReadingsError(message: string): void {
    this.readingsLoadableSignal.update((prev: Loadable<readonly OdometerReadingVm[]>) =>
      createErrorLoadable(prev.data, message)
    );
  }

  setOrdersLoading(): void {
    this.ordersLoadableSignal.update((prev: Loadable<readonly VehicleServiceOrderSummaryDto[]>) =>
      createLoadingLoadable(prev.data)
    );
  }

  setOrdersReady(orders: readonly VehicleServiceOrderSummaryDto[]): void {
    this.ordersLoadableSignal.set(createReadyLoadable(orders));
  }

  setOrdersError(message: string): void {
    this.ordersLoadableSignal.update((prev: Loadable<readonly VehicleServiceOrderSummaryDto[]>) =>
      createErrorLoadable(prev.data, message)
    );
  }

  markRoutineFlashed(routineId: string, durationMs = 600): void {
    this.flashedRoutineIdsSignal.update((prev: ReadonlySet<string>) => {
      const next: Set<string> = new Set<string>(prev);
      next.add(routineId);
      return next;
    });
    if (durationMs > 0) {
      setTimeout(() => {
        this.flashedRoutineIdsSignal.update((prev: ReadonlySet<string>) => {
          const next: Set<string> = new Set<string>(prev);
          next.delete(routineId);
          return next;
        });
      }, durationMs);
    }
  }
}
