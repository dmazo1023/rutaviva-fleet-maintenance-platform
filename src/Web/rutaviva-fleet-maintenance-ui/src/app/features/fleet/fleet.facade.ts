import { DestroyRef, inject, Injectable, Signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, Observable, tap } from 'rxjs';
import { SessionStore } from '../../core/context/session.store';
import { ToastStore } from '../../core/feedback/toast.store';
import { RoutineState, ServiceOrderStatus, VehicleStatus } from '../../core/models/enums';
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
import { FleetApi, VehicleServiceOrderSummaryDto } from './data/fleet.api';
import { FLEET_TEXTS } from './fleet.texts';
import {
  VehicleDetailVm,
  VehicleListItemVm,
  VehicleNextMaintenanceVm,
  VehicleOpenOrderSummaryVm,
} from './models/fleet.vm';
import { FleetFilterKey, FleetStore } from './state/fleet.store';

const CLOSED_ORDER_STATUSES: ReadonlySet<ServiceOrderStatus> = new Set<ServiceOrderStatus>([
  'Completed',
  'Cancelled',
]);

export function composeVehicleListItems(
  vehicles: readonly VehicleDetailVm[],
  planSummaries: readonly MaintenancePlanSummaryVm[],
  openOrdersByVehicle: ReadonlyMap<string, VehicleOpenOrderSummaryVm>
): readonly VehicleListItemVm[] {
  const planMap: Map<string, MaintenancePlanSummaryVm> = new Map<
    string,
    MaintenancePlanSummaryVm
  >(planSummaries.map((p: MaintenancePlanSummaryVm) => [p.vehicleId, p]));

  return vehicles.map((vehicle: VehicleDetailVm): VehicleListItemVm => {
    const plan: MaintenancePlanSummaryVm | undefined = planMap.get(vehicle.vehicleId);
    const openOrder: VehicleOpenOrderSummaryVm | null =
      openOrdersByVehicle.get(vehicle.vehicleId) ?? null;

    let nextMaintenance: VehicleNextMaintenanceVm | null = null;
    if (
      vehicle.status === 'Active' &&
      plan?.worstDueStatus &&
      plan.nextRoutineName &&
      plan.nextRemainingKind &&
      plan.nextRemainingValue !== null
    ) {
      nextMaintenance = {
        routineName: plan.nextRoutineName,
        status: plan.worstDueStatus,
        consumedPercent: plan.worstConsumedPercent,
        remainingKind: plan.nextRemainingKind,
        remainingValue: plan.nextRemainingValue,
        triggeredBy: plan.worstTriggeredBy,
      };
    }

    return {
      vehicleId: vehicle.vehicleId,
      plate: vehicle.plate,
      brand: vehicle.brand,
      model: vehicle.model,
      modelYear: vehicle.modelYear,
      customerId: vehicle.customerId,
      customerName: vehicle.customerName,
      status: vehicle.status,
      worstDueStatus: vehicle.status === 'Active' ? (plan?.worstDueStatus ?? null) : null,
      worstConsumedPercent:
        vehicle.status === 'Active' ? (plan?.worstConsumedPercent ?? 0) : 0,
      worstTriggeredBy:
        vehicle.status === 'Active' ? (plan?.worstTriggeredBy ?? null) : null,
      odometerKm: plan?.odometerKm ?? vehicle.odometerKm,
      nextMaintenance,
      openOrder,
    };
  });
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
  readonly ordersLoadable: Signal<Loadable<readonly VehicleServiceOrderSummaryDto[]>> =
    this.planStore.ordersLoadable;
  readonly flashedRoutineIds: Signal<ReadonlySet<string>> = this.planStore.flashedRoutineIds;

  constructor() {
    this.bindRealtimeEvents();
    this.viewRefresh.refresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
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
      plans: this.maintenanceApi.getPlanSummaries(customerId),
      orders: this.fleetApi.getVehicleOrders(''),
    }).subscribe({
      next: ({ vehicles, plans, orders }) => {
        const openOrdersMap: Map<string, VehicleOpenOrderSummaryVm> = new Map<
          string,
          VehicleOpenOrderSummaryVm
        >();
        for (const order of orders) {
          if (!CLOSED_ORDER_STATUSES.has(order.status) && !openOrdersMap.has(order.vehicleId)) {
            openOrdersMap.set(order.vehicleId, {
              orderId: order.id,
              number: order.number,
              status: order.status,
            });
          }
        }
        const items: readonly VehicleListItemVm[] = composeVehicleListItems(
          vehicles,
          plans,
          openOrdersMap
        );
        this.fleetStore.setListReady(items);
      },
      error: () => {
        this.fleetStore.setListError(FLEET_TEXTS.errorMessage);
      },
    });
  }

  loadVehicleDetail(plate: string): void {
    this.activePlate = plate;
    this.fleetStore.setDetailLoading();
    this.planStore.setPlanLoading();
    this.planStore.setReadingsLoading();
    this.planStore.setOrdersLoading();

    this.fleetApi.getVehicleByPlate(plate).subscribe({
      next: (vehicle: VehicleDetailVm | null) => {
        this.fleetStore.setDetailReady(vehicle);
        if (!vehicle) {
          this.planStore.setPlanReady(null);
          this.planStore.setReadingsReady([]);
          this.planStore.setOrdersReady([]);
          return;
        }
        this.loadVehicleSubResources(vehicle.vehicleId);
      },
      error: () => {
        this.fleetStore.setDetailError(FLEET_TEXTS.errorMessage);
      },
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
        next: (updated: RoutineVm) => {
          this.planStore.replaceRoutine(updated);
          this.planStore.markRoutineFlashed(updated.routineId);
          this.loadFleetList();

          const message: string =
            nextState === 'Inactive'
              ? `${routine.name}${MAINTENANCE_TEXTS.deactivatedSuffix}`
              : `${routine.name}${MAINTENANCE_TEXTS.activatedSuffix}`;

          this.toastStore.show({
            message,
            action: {
              label: MAINTENANCE_TEXTS.undoAction,
              callback: () => {
                this.maintenanceApi
                  .updateRoutineState(routine.vehicleId, routine.routineId, previousState)
                  .subscribe((restored: RoutineVm) => {
                    this.planStore.replaceRoutine(restored);
                    this.loadFleetList();
                  });
              },
            },
          });
        },
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

  updateVehicleStatus(vehicleId: string, status: VehicleStatus): Observable<VehicleDetailVm> {
    return this.fleetApi.updateVehicleStatus(vehicleId, status).pipe(
      tap((updated: VehicleDetailVm) => {
        this.fleetStore.setDetailReady(updated);
        this.loadVehicleSubResources(updated.vehicleId);
        this.loadFleetList();
      })
    );
  }

  createManualOrder(
    vehicleId: string,
    routineId: string
  ): Observable<{ readonly id: string; readonly number: string }> {
    return this.fleetApi.createManualServiceOrder(vehicleId, routineId).pipe(
      tap(() => {
        this.loadVehicleSubResources(vehicleId);
        this.loadFleetList();
      })
    );
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

    this.fleetApi.getVehicleOrders(vehicleId).subscribe({
      next: (orders: readonly VehicleServiceOrderSummaryDto[]) =>
        this.planStore.setOrdersReady(orders),
      error: () => this.planStore.setOrdersError(FLEET_TEXTS.errorMessage),
    });
  }

  private bindRealtimeEvents(): void {
    this.notifications.routineChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: RoutineChangedEvent) => {
        this.fleetStore.markVehicleFlashed(event.vehicleId);
        this.planStore.markRoutineFlashed(event.routineId);
        this.loadFleetList();
        if (this.activePlate && this.fleetStore.detailLoadable().data?.vehicleId === event.vehicleId) {
          this.loadVehicleSubResources(event.vehicleId);
        }
      });

    this.notifications.odometerReadingRecorded$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: OdometerReadingRecordedEvent) => {
        this.fleetStore.markVehicleFlashed(event.vehicleId);
        this.loadFleetList();
        if (this.activePlate && this.fleetStore.detailLoadable().data?.vehicleId === event.vehicleId) {
          this.loadVehicleDetail(this.activePlate);
        }
      });

    this.notifications.vehicleChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: VehicleChangedEvent) => {
        this.fleetStore.markVehicleFlashed(event.vehicleId);
        this.loadFleetList();
        if (this.activePlate && this.fleetStore.detailLoadable().data?.vehicleId === event.vehicleId) {
          this.loadVehicleDetail(this.activePlate);
        }
      });

    this.notifications.serviceOrderChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: ServiceOrderChangedEvent) => {
        this.fleetStore.markVehicleFlashed(event.vehicleId);
        this.loadFleetList();
        if (this.activePlate && this.fleetStore.detailLoadable().data?.vehicleId === event.vehicleId) {
          this.loadVehicleSubResources(event.vehicleId);
        }
      });
  }
}
