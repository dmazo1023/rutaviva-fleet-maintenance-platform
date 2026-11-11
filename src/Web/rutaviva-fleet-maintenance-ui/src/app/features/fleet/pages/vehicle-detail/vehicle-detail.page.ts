import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  input,
  InputSignal,
  Signal,
  signal,
  viewChildren,
  WritableSignal,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SessionStore } from '../../../../core/context/session.store';
import { ViewRefreshService } from '../../../../core/shell/view-refresh.service';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../../shared/error-state/error-state.component';
import { PlateChipComponent } from '../../../../shared/plate-chip/plate-chip.component';
import { SkeletonComponent } from '../../../../shared/skeleton/skeleton.component';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { SwitchComponent } from '../../../../shared/switch/switch.component';
import { OdometerReadingsComponent } from '../../../maintenance/components/odometer-readings/odometer-readings.component';
import { RoutineRowComponent } from '../../../maintenance/components/routine-row/routine-row.component';
import { UpdateRoutineParametersRequest } from '../../../maintenance/data/maintenance.api';
import { RoutineParamsDialogComponent } from '../../../maintenance/dialogs/routine-params/routine-params.dialog';
import { RoutineVm } from '../../../maintenance/models/maintenance.vm';
import { DeactivateVehicleDialogComponent } from '../../dialogs/deactivate-vehicle/deactivate-vehicle.dialog';
import { FleetFacade } from '../../fleet.facade';
import { FLEET_TEXTS } from '../../fleet.texts';
import { VehicleDetailVm } from '../../models/fleet.vm';

export type VehicleDetailTab = 'rutinas' | 'ordenes' | 'lecturas';

const TAB_ORDER: readonly VehicleDetailTab[] = ['rutinas', 'ordenes', 'lecturas'];
const NUMBER_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');

@Component({
  selector: 'app-vehicle-detail-page',
  imports: [
    RouterLink,
    PlateChipComponent,
    StatusPillComponent,
    SwitchComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    RoutineRowComponent,
    OdometerReadingsComponent,
    RoutineParamsDialogComponent,
    DeactivateVehicleDialogComponent,
  ],
  templateUrl: './vehicle-detail.page.html',
  styleUrl: './vehicle-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleDetailPageComponent {
  private readonly facade: FleetFacade = inject(FleetFacade);
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly router: Router = inject(Router);
  private readonly route: ActivatedRoute = inject(ActivatedRoute);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);

  readonly plate: InputSignal<string> = input.required<string>();
  readonly tab: InputSignal<string | undefined> = input<string | undefined>(undefined);

  readonly tabButtons = viewChildren<ElementRef<HTMLButtonElement>>('tabBtn');

  readonly texts: typeof FLEET_TEXTS = FLEET_TEXTS;
  readonly detailLoadable = this.facade.detailLoadable;
  readonly planLoadable = this.facade.planLoadable;
  readonly readingsLoadable = this.facade.readingsLoadable;
  readonly ordersLoadable = this.facade.ordersLoadable;
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

  readonly openOrdersCount: Signal<number> = computed(() => {
    const orders = this.ordersLoadable().data;
    return orders.filter((o) => o.status !== 'Completed' && o.status !== 'Cancelled').length;
  });

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

  formatKm(km: number): string {
    return `${NUMBER_FORMATTER.format(km)}\u00A0${this.texts.kmUnit}`;
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
    this.facade.updateVehicleStatus(vehicle.vehicleId, 'Active').subscribe();
  }

  onConfirmDeactivate(vehicle: VehicleDetailVm): void {
    this.deactivatingVehicle.set(true);
    this.facade.updateVehicleStatus(vehicle.vehicleId, 'Inactive').subscribe({
      next: () => {
        this.deactivatingVehicle.set(false);
        this.deactivateDialogOpen.set(false);
      },
      error: () => {
        this.deactivatingVehicle.set(false);
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

  onCreateManualOrder(routine: RoutineVm): void {
    this.facade.createManualOrder(routine.vehicleId, routine.routineId).subscribe({
      next: (created) => {
        void this.router.navigate(['/ordenes', created.id]);
      },
    });
  }

  onOpenOrder(orderId: string): void {
    void this.router.navigate(['/ordenes', orderId]);
  }

  onRetry(): void {
    this.facade.loadVehicleDetail(this.plate());
  }
}

