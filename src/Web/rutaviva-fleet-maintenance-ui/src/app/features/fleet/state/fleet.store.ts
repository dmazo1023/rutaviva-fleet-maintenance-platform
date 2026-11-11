import { computed, Injectable, Signal, signal, WritableSignal } from '@angular/core';
import {
  createErrorLoadable,
  createIdleLoadable,
  createLoadingLoadable,
  createReadyLoadable,
  Loadable,
} from '../../../shared/state/loadable';
import { VehicleDetailVm, VehicleListItemVm } from '../models/fleet.vm';

export type FleetFilterKey = 'All' | 'Overdue' | 'DueSoon' | 'UpToDate' | 'Inactive';

const URGENCY_RANK: Readonly<Record<string, number>> = {
  Overdue: 0,
  DueSoon: 1,
  UpToDate: 2,
  Inactive: 3,
};

const INACTIVE_RANK = 3;
const UP_TO_DATE_RANK = 2;

export function getVehicleUrgencyRank(vehicle: VehicleListItemVm): number {
  if (vehicle.status === 'Inactive') {
    return INACTIVE_RANK;
  }
  if (!vehicle.worstDueStatus) {
    return UP_TO_DATE_RANK;
  }
  return URGENCY_RANK[vehicle.worstDueStatus] ?? UP_TO_DATE_RANK;
}

export function sortVehiclesByUrgency(
  items: readonly VehicleListItemVm[]
): readonly VehicleListItemVm[] {
  return [...items].sort((a: VehicleListItemVm, b: VehicleListItemVm) => {
    const rankDiff: number = getVehicleUrgencyRank(a) - getVehicleUrgencyRank(b);
    if (rankDiff !== 0) {
      return rankDiff;
    }
    const percentDiff: number = b.worstConsumedPercent - a.worstConsumedPercent;
    if (percentDiff !== 0) {
      return percentDiff;
    }
    return a.plate.localeCompare(b.plate);
  });
}

@Injectable({
  providedIn: 'root',
})
export class FleetStore {
  private readonly listLoadableSignal: WritableSignal<Loadable<readonly VehicleListItemVm[]>> =
    signal<Loadable<readonly VehicleListItemVm[]>>(createIdleLoadable([]));
  private readonly detailLoadableSignal: WritableSignal<Loadable<VehicleDetailVm | null>> =
    signal<Loadable<VehicleDetailVm | null>>(createIdleLoadable(null));
  private readonly searchQuerySignal: WritableSignal<string> = signal<string>('');
  private readonly activeFilterSignal: WritableSignal<FleetFilterKey> =
    signal<FleetFilterKey>('All');
  private readonly flashedVehicleIdsSignal: WritableSignal<ReadonlySet<string>> =
    signal<ReadonlySet<string>>(new Set<string>());

  readonly listLoadable: Signal<Loadable<readonly VehicleListItemVm[]>> =
    this.listLoadableSignal.asReadonly();
  readonly detailLoadable: Signal<Loadable<VehicleDetailVm | null>> =
    this.detailLoadableSignal.asReadonly();
  readonly searchQuery: Signal<string> = this.searchQuerySignal.asReadonly();
  readonly activeFilter: Signal<FleetFilterKey> = this.activeFilterSignal.asReadonly();
  readonly flashedVehicleIds: Signal<ReadonlySet<string>> =
    this.flashedVehicleIdsSignal.asReadonly();

  readonly sortedVehicles: Signal<readonly VehicleListItemVm[]> = computed(() =>
    sortVehiclesByUrgency(this.listLoadableSignal().data)
  );

  readonly filteredVehicles: Signal<readonly VehicleListItemVm[]> = computed(() => {
    const items: readonly VehicleListItemVm[] = this.sortedVehicles();
    const filter: FleetFilterKey = this.activeFilterSignal();
    const query: string = this.searchQuerySignal().trim().toLowerCase();

    return items.filter((item: VehicleListItemVm) => {
      if (!this.matchesFilter(item, filter)) {
        return false;
      }
      if (!query) {
        return true;
      }
      return (
        item.plate.toLowerCase().includes(query) ||
        item.brand.toLowerCase().includes(query) ||
        item.model.toLowerCase().includes(query) ||
        item.customerName.toLowerCase().includes(query)
      );
    });
  });

  readonly filterCounts: Signal<Readonly<Record<FleetFilterKey, number>>> = computed(() => {
    const items: readonly VehicleListItemVm[] = this.sortedVehicles();
    let overdueCount = 0;
    let dueSoonCount = 0;
    let upToDateCount = 0;
    let inactiveCount = 0;

    for (const item of items) {
      if (item.status === 'Inactive') {
        inactiveCount += 1;
      } else if (item.worstDueStatus === 'Overdue') {
        overdueCount += 1;
      } else if (item.worstDueStatus === 'DueSoon') {
        dueSoonCount += 1;
      } else {
        upToDateCount += 1;
      }
    }

    return {
      All: items.length,
      Overdue: overdueCount,
      DueSoon: dueSoonCount,
      UpToDate: upToDateCount,
      Inactive: inactiveCount,
    };
  });

  setListLoading(): void {
    this.listLoadableSignal.update((prev: Loadable<readonly VehicleListItemVm[]>) =>
      createLoadingLoadable(prev.data)
    );
  }

  setListReady(items: readonly VehicleListItemVm[]): void {
    this.listLoadableSignal.set(createReadyLoadable(items));
  }

  setListError(message: string): void {
    this.listLoadableSignal.update((prev: Loadable<readonly VehicleListItemVm[]>) =>
      createErrorLoadable(prev.data, message)
    );
  }

  setDetailLoading(): void {
    this.detailLoadableSignal.update((prev: Loadable<VehicleDetailVm | null>) =>
      createLoadingLoadable(prev.data)
    );
  }

  setDetailReady(detail: VehicleDetailVm | null): void {
    this.detailLoadableSignal.set(createReadyLoadable(detail));
  }

  setDetailError(message: string): void {
    this.detailLoadableSignal.update((prev: Loadable<VehicleDetailVm | null>) =>
      createErrorLoadable(prev.data, message)
    );
  }

  setSearchQuery(query: string): void {
    this.searchQuerySignal.set(query);
  }

  setActiveFilter(filter: FleetFilterKey): void {
    this.activeFilterSignal.set(filter);
  }

  clearFilters(): void {
    this.searchQuerySignal.set('');
    this.activeFilterSignal.set('All');
  }

  markVehicleFlashed(vehicleId: string, durationMs = 600): void {
    this.flashedVehicleIdsSignal.update((prev: ReadonlySet<string>) => {
      const next: Set<string> = new Set<string>(prev);
      next.add(vehicleId);
      return next;
    });
    if (durationMs > 0) {
      setTimeout(() => {
        this.flashedVehicleIdsSignal.update((prev: ReadonlySet<string>) => {
          const next: Set<string> = new Set<string>(prev);
          next.delete(vehicleId);
          return next;
        });
      }, durationMs);
    }
  }

  private matchesFilter(item: VehicleListItemVm, filter: FleetFilterKey): boolean {
    if (filter === 'All') {
      return true;
    }
    if (filter === 'Inactive') {
      return item.status === 'Inactive';
    }
    if (item.status === 'Inactive') {
      return false;
    }
    if (filter === 'Overdue') {
      return item.worstDueStatus === 'Overdue';
    }
    if (filter === 'DueSoon') {
      return item.worstDueStatus === 'DueSoon';
    }
    return item.worstDueStatus === 'UpToDate' || item.worstDueStatus === null;
  }
}
