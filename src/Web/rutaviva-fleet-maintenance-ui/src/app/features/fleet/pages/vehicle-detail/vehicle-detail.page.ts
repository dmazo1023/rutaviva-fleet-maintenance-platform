import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  input,
  InputSignal,
  OnInit,
  Signal,
  signal,
  viewChildren,
  WritableSignal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ENUM_LABELS } from '../../../../core/models/enum-labels';
import { VehicleStatus } from '../../../../core/models/enums';
import { SessionStore } from '../../../../core/context/session.store';
import { ShowToastOptions, ToastStore } from '../../../../core/feedback/toast.store';
import { ViewRefreshService } from '../../../../core/shell/view-refresh.service';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../../shared/error-state/error-state.component';
import { PlateChipComponent } from '../../../../shared/plate-chip/plate-chip.component';
import {
  SegmentedFilterComponent,
  SegmentedFilterOption,
} from '../../../../shared/segmented-filter/segmented-filter.component';
import { SkeletonComponent } from '../../../../shared/skeleton/skeleton.component';
import { SwitchComponent } from '../../../../shared/switch/switch.component';
import { OdometerReadingsComponent } from '../../../maintenance/components/odometer-readings/odometer-readings.component';
import { RoutineRowComponent } from '../../../maintenance/components/routine-row/routine-row.component';
import { ServiceRegistrationHostComponent } from '../../../maintenance/components/service-registration-host/service-registration-host.component';
import { UpdateRoutineParametersRequest } from '../../../maintenance/data/maintenance.api';
import { RoutineParamsDialogComponent } from '../../../maintenance/dialogs/routine-params/routine-params.dialog';
import { RoutineVm } from '../../../maintenance/models/maintenance.vm';
import { ServiceRegistrationStore } from '../../../maintenance/state/service-registration.store';
import { DeactivateVehicleDialogComponent } from '../../dialogs/deactivate-vehicle/deactivate-vehicle.dialog';
import { FleetFacade } from '../../fleet.facade';
import { FLEET_TEXTS } from '../../fleet.texts';
import { VehicleDetailVm } from '../../models/fleet.vm';
import { FleetFilterKey } from '../../state/fleet.store';
import { VEHICLE_DETAIL_TEXTS } from './vehicle-detail.texts';
import { VehicleMasterListComponent } from './vehicle-master-list/vehicle-master-list.component';

export type VehicleDetailTab = 'rutinas' | 'ordenes' | 'lecturas';

const NO_OPEN_ORDERS = 0;
const OPERATION_ERROR_TOAST: ShowToastOptions = {
  message: FLEET_TEXTS.operationErrorMessage,
  tone: 'danger',
  isError: true,
};
const TAB_ORDER: readonly VehicleDetailTab[] = ['rutinas', 'ordenes', 'lecturas'];
const NUMBER_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');
const VALID_FILTERS: ReadonlySet<string> = new Set<string>([
  'All',
  'Overdue',
  'DueSoon',
  'UpToDate',
  'Inactive',
]);

export interface VehicleDetailTabItem {
  readonly key: VehicleDetailTab;
  readonly label: string;
  readonly count: number;
}

@Component({
  selector: 'app-vehicle-detail-page',
  imports: [
    RouterLink,
    PlateChipComponent,
    SegmentedFilterComponent,
    VehicleMasterListComponent,
    SwitchComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    RoutineRowComponent,
    ServiceRegistrationHostComponent,
    OdometerReadingsComponent,
    RoutineParamsDialogComponent,
    DeactivateVehicleDialogComponent,
  ],
  templateUrl: './vehicle-detail.page.html',
  styleUrl: './vehicle-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleDetailPageComponent implements OnInit {
  private readonly facade: FleetFacade = inject(FleetFacade);
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly router: Router = inject(Router);
  private readonly route: ActivatedRoute = inject(ActivatedRoute);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);
  private readonly serviceRegistration: ServiceRegistrationStore = inject(ServiceRegistrationStore);
  private readonly toastStore: ToastStore = inject(ToastStore);

  readonly plate: InputSignal<string> = input.required<string>();
  readonly tab: InputSignal<string | undefined> = input<string | undefined>(undefined);

  readonly tabButtons = viewChildren<ElementRef<HTMLButtonElement>>('tabBtn');

  readonly texts: typeof FLEET_TEXTS = FLEET_TEXTS;
  readonly detailTexts: typeof VEHICLE_DETAIL_TEXTS = VEHICLE_DETAIL_TEXTS;
  readonly listLoadable = this.facade.listLoadable;
  readonly filteredVehicles = this.facade.filteredVehicles;
  readonly searchQuery = this.facade.searchQuery;
  readonly activeFilter = this.facade.activeFilter;
  readonly filterCounts = this.facade.filterCounts;
  readonly flashedVehicleIds = this.facade.flashedVehicleIds;
  readonly detailLoadable = this.facade.detailLoadable;
  readonly planLoadable = this.facade.planLoadable;
  readonly readingsLoadable = this.facade.readingsLoadable;
  readonly flashedRoutineIds = this.facade.flashedRoutineIds;

  readonly activeTab: WritableSignal<VehicleDetailTab> = signal<VehicleDetailTab>('rutinas');
  readonly editingRoutine: WritableSignal<RoutineVm | null> = signal<RoutineVm | null>(null);
  readonly savingRoutineParams: WritableSignal<boolean> = signal<boolean>(false);
  readonly deactivateDialogOpen: WritableSignal<boolean> = signal<boolean>(false);
  readonly deactivatingVehicle: WritableSignal<boolean> = signal<boolean>(false);

  readonly canManage: Signal<boolean> = computed(() => this.sessionStore.profile() === 'Operator');

  readonly activeRoutinesCount: Signal<number> = computed(() => {
    const routines: readonly RoutineVm[] = this.planLoadable().data?.routines ?? [];
    return routines.filter((r: RoutineVm) => r.state === 'Active').length;
  });

  readonly openOrdersCount: number = NO_OPEN_ORDERS;

  readonly filterOptions: Signal<readonly SegmentedFilterOption[]> = computed(() => {
    const counts = this.filterCounts();
    return [
      { value: 'All', label: this.texts.filterAll, count: counts.All },
      {
        value: 'Overdue',
        label: this.texts.filterOverdue,
        count: counts.Overdue,
        toneClass: 'is-danger',
      },
      {
        value: 'DueSoon',
        label: this.texts.filterDueSoon,
        count: counts.DueSoon,
        toneClass: 'is-warn',
      },
      {
        value: 'UpToDate',
        label: this.texts.filterUpToDate,
        count: counts.UpToDate,
        toneClass: 'is-ok',
      },
      { value: 'Inactive', label: this.texts.filterInactive, count: counts.Inactive },
    ];
  });

  readonly tabItems: Signal<readonly VehicleDetailTabItem[]> = computed(() => [
    {
      key: 'rutinas',
      label: this.texts.tabRoutines,
      count: this.planLoadable().data?.routines.length ?? 0,
    },
    { key: 'ordenes', label: this.texts.tabOrders, count: this.openOrdersCount },
    { key: 'lecturas', label: this.texts.tabReadings, count: this.readingsLoadable().data.length },
  ]);

  readonly currentOdometerKm: Signal<number | null> = computed(
    () => this.planLoadable().data?.odometerKm ?? this.detailLoadable().data?.initialOdometerKm ?? null
  );

  constructor() {
    effect(() => {
      const currentPlate: string = this.plate();
      if (currentPlate) {
        this.viewRefresh.setActiveVehicleId(currentPlate);
        this.facade.loadVehicleDetail(currentPlate);
      }
    });

    effect(() => {
      const tabParam: string | undefined = this.tab();
      if (tabParam === 'rutinas' || tabParam === 'ordenes' || tabParam === 'lecturas') {
        this.activeTab.set(tabParam);
      } else {
        this.activeTab.set('rutinas');
      }
    });
  }

  ngOnInit(): void {
    if (this.listLoadable().status === 'idle') {
      this.facade.loadFleetList();
    }
  }

  formatNumber(value: number | null): string {
    return value === null ? this.texts.emptyValue : NUMBER_FORMATTER.format(value);
  }

  statusLabel(status: VehicleStatus): string {
    return ENUM_LABELS.vehicleStatus[status];
  }

  onSearchInput(event: Event): void {
    this.facade.setSearchQuery((event.target as HTMLInputElement).value);
  }

  onFilterChange(value: string): void {
    if (VALID_FILTERS.has(value)) {
      this.facade.setActiveFilter(value as FleetFilterKey);
    }
  }

  isRoutineFlashed(routineId: string): boolean {
    return this.flashedRoutineIds().has(routineId);
  }

  selectTab(nextTab: VehicleDetailTab, focusButton = false): void {
    this.activeTab.set(nextTab);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: nextTab === 'rutinas' ? null : nextTab },
      queryParamsHandling: 'merge',
    });

    if (focusButton) {
      const idx: number = TAB_ORDER.indexOf(nextTab);
      const targetBtn: HTMLButtonElement | undefined = this.tabButtons()[idx]?.nativeElement;
      targetBtn?.focus();
    }
  }

  onTabKeydown(event: KeyboardEvent): void {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') {
      return;
    }
    event.preventDefault();
    const currentIndex: number = TAB_ORDER.indexOf(this.activeTab());
    const delta: number = event.key === 'ArrowRight' ? 1 : -1;
    const nextIndex: number = (currentIndex + delta + TAB_ORDER.length) % TAB_ORDER.length;
    const nextTab: VehicleDetailTab = TAB_ORDER[nextIndex] ?? 'rutinas';
    this.selectTab(nextTab, true);
  }

  onVehicleStatusSwitch(vehicle: VehicleDetailVm): void {
    if (vehicle.status === 'Active') {
      this.deactivateDialogOpen.set(true);
      return;
    }
    this.facade.updateVehicleStatus(vehicle, 'Active').subscribe({
      error: () => this.toastStore.show(OPERATION_ERROR_TOAST),
    });
  }

  onConfirmDeactivate(vehicle: VehicleDetailVm): void {
    this.deactivatingVehicle.set(true);
    this.facade.updateVehicleStatus(vehicle, 'Inactive').subscribe({
      next: () => {
        this.deactivatingVehicle.set(false);
        this.deactivateDialogOpen.set(false);
      },
      error: () => {
        this.deactivatingVehicle.set(false);
        this.toastStore.show(OPERATION_ERROR_TOAST);
      },
    });
  }

  onCloseDeactivateDialog(): void {
    this.deactivateDialogOpen.set(false);
  }

  onToggleRoutine(routine: RoutineVm): void {
    this.facade.toggleRoutineStateWithUndo(routine);
  }

  onOpenRoutineParams(routine: RoutineVm): void {
    this.editingRoutine.set(routine);
  }

  onCloseRoutineParams(): void {
    this.editingRoutine.set(null);
  }

  onSaveRoutineParams(request: UpdateRoutineParametersRequest): void {
    const routine: RoutineVm | null = this.editingRoutine();
    if (!routine) {
      return;
    }
    this.savingRoutineParams.set(true);
    this.facade.updateRoutineParameters(routine.vehicleId, routine.routineId, request).subscribe({
      next: () => {
        this.savingRoutineParams.set(false);
        this.editingRoutine.set(null);
      },
      error: () => {
        this.savingRoutineParams.set(false);
      },
    });
  }

  onRegisterService(routine: RoutineVm): void {
    this.serviceRegistration.open(routine);
  }

  onRetry(): void {
    this.facade.loadVehicleDetail(this.plate());
  }
}

