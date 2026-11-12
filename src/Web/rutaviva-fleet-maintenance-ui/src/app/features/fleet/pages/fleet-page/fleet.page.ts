import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  InputSignal,
  OnInit,
  Signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { ViewRefreshService } from '../../../../core/shell/view-refresh.service';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../../shared/error-state/error-state.component';
import {
  SegmentedFilterComponent,
  SegmentedFilterOption,
} from '../../../../shared/segmented-filter/segmented-filter.component';
import { SkeletonComponent } from '../../../../shared/skeleton/skeleton.component';
import { VehicleListComponent } from '../../components/vehicle-list/vehicle-list.component';
import { FleetFacade } from '../../fleet.facade';
import { FLEET_TEXTS } from '../../fleet.texts';
import { FleetFilterKey } from '../../state/fleet.store';

const VALID_FILTERS: ReadonlySet<string> = new Set<string>([
  'All',
  'Overdue',
  'DueSoon',
  'UpToDate',
  'Inactive',
]);

@Component({
  selector: 'app-fleet-page',
  imports: [
    SegmentedFilterComponent,
    SkeletonComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    VehicleListComponent,
  ],
  templateUrl: './fleet.page.html',
  styleUrl: './fleet.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FleetPageComponent implements OnInit {
  private readonly facade: FleetFacade = inject(FleetFacade);
  private readonly router: Router = inject(Router);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);

  readonly status: InputSignal<string | undefined> = input<string | undefined>(undefined);

  readonly texts: typeof FLEET_TEXTS = FLEET_TEXTS;
  readonly listLoadable = this.facade.listLoadable;
  readonly sortedVehicles = this.facade.sortedVehicles;
  readonly filteredVehicles = this.facade.filteredVehicles;
  readonly searchQuery = this.facade.searchQuery;
  readonly activeFilter = this.facade.activeFilter;
  readonly flashedIds = this.facade.flashedVehicleIds;
  readonly openOrders = this.facade.openOrdersByVehicle;

  readonly filterOptions: Signal<readonly SegmentedFilterOption[]> = computed(() => {
    const counts = this.facade.filterCounts();
    const active: FleetFilterKey = this.activeFilter();
    const options: readonly SegmentedFilterOption[] = [
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
      {
        value: 'Inactive',
        label: this.texts.filterInactive,
        count: counts.Inactive,
        toneClass: 'is-neutral',
      },
    ];
    return options.filter(
      (option: SegmentedFilterOption) =>
        option.value === 'All' || (option.count ?? 0) > 0 || option.value === active
    );
  });

  constructor() {
    effect(() => {
      const paramStatus: string | undefined = this.status();
      if (paramStatus && VALID_FILTERS.has(paramStatus)) {
        this.facade.setActiveFilter(paramStatus as FleetFilterKey);
      } else if (paramStatus === undefined) {
        this.facade.setActiveFilter('All');
      }
    });
  }

  ngOnInit(): void {
    this.viewRefresh.setActiveVehicleId(null);
    this.facade.loadFleetList();
  }

  onSearchInput(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.facade.setSearchQuery(target.value);
  }

  onFilterChange(value: string): void {
    if (VALID_FILTERS.has(value)) {
      this.facade.setActiveFilter(value as FleetFilterKey);
    }
  }

  onRetry(): void {
    this.facade.loadFleetList();
  }

  onClearFilters(): void {
    this.facade.clearFilters();
  }

  onSelectVehicle(plate: string): void {
    void this.router.navigate(['/flota', plate]);
  }
}
