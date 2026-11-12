import { DestroyRef, inject, Injectable, Signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, Observable, tap } from 'rxjs';
import { SessionStore } from '../../core/context/session.store';
import { ToastStore } from '../../core/feedback/toast.store';
import { RoutineState, VehicleStatus } from '../../core/models/enums';
import { NotificationsService } from '../../core/realtime/notifications.service';
import {
  OdometerReadingRecordedEvent,
  RoutineChangedEvent,
  ServiceOrderChangedEvent,
  VehicleChangedEvent,
} from '../../core/realtime/realtime-events';
import { ViewRefreshService } from '../../core/shell/view-refresh.service';
import { Loadable } from '../../shared/state/loadable';
import {
  MaintenanceApi,
  UpdateRoutineParametersRequest,
} from '../maintenance/data/maintenance.api';
import { MAINTENANCE_TEXTS } from '../maintenance/maintenance.texts';
import {
  MaintenancePlanDetailVm,
  MaintenancePlanSummaryVm,
  OdometerReadingVm,
  RoutineVm,
} from '../maintenance/models/maintenance.vm';
import { PlanStore } from '../maintenance/state/plan.store';
import { FleetApi } from './data/fleet.api';
import { FLEET_TEXTS } from './fleet.texts';
import {
  VehicleDetailVm,
  VehicleListItemVm,
  VehicleMaintenanceSummaryVm,
  VehicleSummaryVm,
} from './models/fleet.vm';
import { FleetFilterKey, FleetStore } from './state/fleet.store';

function toMaintenanceSummary(plan: MaintenancePlanSummaryVm): VehicleMaintenanceSummaryVm {
  return {
    worstDueStatus: plan.worstDueStatus,
    maxConsumedPercent: plan.maxConsumedPercent,
    upToDateCount: plan.upToDateCount,
    dueSoonCount: plan.dueSoonCount,
    overdueCount: plan.overdueCount,
    nextMaintenance: plan.nextMaintenance,
  };
}

function composeVehicleListItem(
  vehicle: VehicleSummaryVm,
  plan: MaintenancePlanSummaryVm | undefined
): VehicleListItemVm {
  const isActive: boolean = vehicle.status === 'Active';
  const maintenance: VehicleMaintenanceSummaryVm | null =
    isActive && plan ? toMaintenanceSummary(plan) : null;

  return {
    vehicleId: vehicle.vehicleId,
    plate: vehicle.plate,
    brand: vehicle.brand,
    model: vehicle.model,
    modelYear: vehicle.modelYear,
    customerId: vehicle.customerId,
    customerName: vehicle.customerName,
    status: vehicle.status,
    worstDueStatus: maintenance?.worstDueStatus ?? null,
    maxConsumedPercent: maintenance?.maxConsumedPercent ?? 0,
    odometerKm: plan?.odometerKm ?? null,
    maintenance,
  };
}

export function composeVehicleListItems(
  vehicles: readonly VehicleSummaryVm[],
  planSummaries: readonly MaintenancePlanSummaryVm[]
): readonly VehicleListItemVm[] {
  const planMap: Map<string, MaintenancePlanSummaryVm> = new Map<
    string,
    MaintenancePlanSummaryVm
  >(planSummaries.map((p: MaintenancePlanSummaryVm) => [p.vehicleId, p]));

  return vehicles.map((vehicle: VehicleSummaryVm) =>
    composeVehicleListItem(vehicle, planMap.get(vehicle.vehicleId))
  );
}

@Injectable({
  providedIn: 'root',
})
export class FleetFacade {
  private readonly fleetApi: FleetApi = inject(FleetApi);
  private readonly maintenanceApi: MaintenanceApi = inject(MaintenanceApi);
  private readonly fleetStore: FleetStore = inject(FleetStore);
  private readonly planStore: PlanStore = inject(PlanStore);
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly notifications: NotificationsService = inject(NotificationsService);
  private readonly toastStore: ToastStore = inject(ToastStore);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);
  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  private activePlate: string | null = null;

  readonly listLoadable: Signal<Loadable<readonly VehicleListItemVm[]>> =
    this.fleetStore.listLoadable;
  readonly detailLoadable: Signal<Loadable<VehicleDetailVm | null>> =
    this.fleetStore.detailLoadable;
  readonly sortedVehicles: Signal<readonly VehicleListItemVm[]> = this.fleetStore.sortedVehicles;
  readonly filteredVehicles: Signal<readonly VehicleListItemVm[]> =
    this.fleetStore.filteredVehicles;
  readonly filterCounts: Signal<Readonly<Record<FleetFilterKey, number>>> =
    this.fleetStore.filterCounts;
  readonly searchQuery: Signal<string> = this.fleetStore.searchQuery;
  readonly activeFilter: Signal<FleetFilterKey> = this.fleetStore.activeFilter;
  readonly flashedVehicleIds: Signal<ReadonlySet<string>> = this.fleetStore.flashedVehicleIds;

  readonly planLoadable: Signal<Loadable<MaintenancePlanDetailVm | null>> =
    this.planStore.planLoadable;
  readonly readingsLoadable: Signal<Loadable<readonly OdometerReadingVm[]>> =
    this.planStore.readingsLoadable;
  readonly flashedRoutineIds: Signal<ReadonlySet<string>> = this.planStore.flashedRoutineIds;

  constructor() {
    this.bindRealtimeEvents();
    this.viewRefresh.refresh$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadFleetList();
      if (this.activePlate) {
        this.loadVehicleDetail(this.activePlate);
      }
    });
  }

  loadFleetList(): void {
    const customerId: string | undefined = this.sessionStore.customerId() ?? undefined;
    this.fleetStore.setListLoading();

    forkJoin({
      vehicles: this.fleetApi.getVehicles({ customerId }),
      plans: this.maintenanceApi.getPlanSummaries(),
    }).subscribe({
      next: ({ vehicles, plans }) =>
        this.fleetStore.setListReady(composeVehicleListItems(vehicles, plans)),
      error: () => this.fleetStore.setListError(FLEET_TEXTS.errorMessage),
    });
  }

  loadVehicleDetail(plate: string): void {
    this.activePlate = plate;
    this.fleetStore.setDetailLoading();
    this.planStore.setPlanLoading();
    this.planStore.setReadingsLoading();

    this.fleetApi.getVehicleByPlate(plate).subscribe({
      next: (vehicle: VehicleDetailVm | null) => this.onVehicleLoaded(vehicle),
      error: () => this.fleetStore.setDetailError(FLEET_TEXTS.errorMessage),
    });
  }

  setSearchQuery(query: string): void {
    this.fleetStore.setSearchQuery(query);
  }

  setActiveFilter(filter: FleetFilterKey): void {
    this.fleetStore.setActiveFilter(filter);
  }

  clearFilters(): void {
    this.fleetStore.clearFilters();
  }

  toggleRoutineStateWithUndo(routine: RoutineVm): void {
    const previousState: RoutineState = routine.state;
    const nextState: RoutineState = routine.state === 'Active' ? 'Inactive' : 'Active';

    this.maintenanceApi
      .updateRoutineState(routine.vehicleId, routine.routineId, nextState)
      .subscribe({
        next: (updated: RoutineVm) => this.onRoutineToggled(routine, updated, previousState),
        error: () => this.showOperationError(),
      });
  }

  updateRoutineParameters(
    vehicleId: string,
    routineId: string,
    request: UpdateRoutineParametersRequest
  ): Observable<RoutineVm> {
    return this.maintenanceApi.updateRoutineParameters(vehicleId, routineId, request).pipe(
      tap((updated: RoutineVm) => {
        this.planStore.replaceRoutine(updated);
        this.planStore.markRoutineFlashed(updated.routineId);
        this.loadFleetList();
      })
    );
  }

  updateVehicleStatus(
    vehicle: VehicleDetailVm,
    status: VehicleStatus
  ): Observable<VehicleDetailVm | null> {
    return this.fleetApi.updateVehicleStatus(vehicle.vehicleId, vehicle.plate, status).pipe(
      tap((updated: VehicleDetailVm | null) => {
        this.fleetStore.setDetailReady(updated);
        this.loadVehicleSubResources(vehicle.vehicleId);
        this.loadFleetList();
      })
    );
  }

  private onVehicleLoaded(vehicle: VehicleDetailVm | null): void {
    this.fleetStore.setDetailReady(vehicle);
    if (!vehicle) {
      this.planStore.setPlanReady(null);
      this.planStore.setReadingsReady([]);
      return;
    }
    this.loadVehicleSubResources(vehicle.vehicleId);
  }

  private onRoutineToggled(
    routine: RoutineVm,
    updated: RoutineVm,
    previousState: RoutineState
  ): void {
    this.planStore.replaceRoutine(updated);
    this.planStore.markRoutineFlashed(updated.routineId);
    this.loadFleetList();

    const suffix: string =
      updated.state === 'Inactive'
        ? MAINTENANCE_TEXTS.deactivatedSuffix
        : MAINTENANCE_TEXTS.activatedSuffix;

    this.toastStore.show({
      message: `${routine.name}${suffix}`,
      action: {
        label: MAINTENANCE_TEXTS.undoAction,
        callback: () => this.undoRoutineToggle(routine, previousState),
      },
    });
  }

  private undoRoutineToggle(routine: RoutineVm, previousState: RoutineState): void {
    this.maintenanceApi
      .updateRoutineState(routine.vehicleId, routine.routineId, previousState)
      .subscribe({
        next: (restored: RoutineVm) => {
          this.planStore.replaceRoutine(restored);
          this.loadFleetList();
        },
        error: () => this.showOperationError(),
      });
  }

  private showOperationError(): void {
    this.toastStore.show({
      message: FLEET_TEXTS.operationErrorMessage,
      tone: 'danger',
      isError: true,
    });
  }

  private loadVehicleSubResources(vehicleId: string): void {
    this.maintenanceApi.getPlanByVehicle(vehicleId).subscribe({
      next: (plan: MaintenancePlanDetailVm | null) => this.planStore.setPlanReady(plan),
      error: () => this.planStore.setPlanError(FLEET_TEXTS.errorMessage),
    });

    this.maintenanceApi.getOdometerReadings(vehicleId).subscribe({
      next: (readings: readonly OdometerReadingVm[]) => this.planStore.setReadingsReady(readings),
      error: () => this.planStore.setReadingsError(FLEET_TEXTS.errorMessage),
    });
  }

  private isActiveVehicle(vehicleId: string): boolean {
    return (
      this.activePlate !== null && this.fleetStore.detailLoadable().data?.vehicleId === vehicleId
    );
  }

  private bindRealtimeEvents(): void {
    this.notifications.routineChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: RoutineChangedEvent) => {
        this.fleetStore.markVehicleFlashed(event.vehicleId);
        this.planStore.markRoutineFlashed(event.routineId);
        this.loadFleetList();
        if (this.isActiveVehicle(event.vehicleId)) {
          this.loadVehicleSubResources(event.vehicleId);
        }
      });

    this.notifications.odometerReadingRecorded$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: OdometerReadingRecordedEvent) =>
        this.refreshVehicleDetail(event.vehicleId)
      );

    this.notifications.vehicleChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: VehicleChangedEvent) => this.refreshVehicleDetail(event.vehicleId));

    this.notifications.serviceOrderChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: ServiceOrderChangedEvent) => {
        this.fleetStore.markVehicleFlashed(event.vehicleId);
        this.loadFleetList();
        if (this.isActiveVehicle(event.vehicleId)) {
          this.loadVehicleSubResources(event.vehicleId);
        }
      });
  }

  private refreshVehicleDetail(vehicleId: string): void {
    this.fleetStore.markVehicleFlashed(vehicleId);
    this.loadFleetList();
    if (this.activePlate && this.isActiveVehicle(vehicleId)) {
      this.loadVehicleDetail(this.activePlate);
    }
  }
}
