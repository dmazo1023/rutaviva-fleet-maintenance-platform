import { computed, Injectable, Signal, signal, WritableSignal } from '@angular/core';
import { ServiceOrderStatus } from '../../../core/models/enums';
import {
  createErrorLoadable,
  createIdleLoadable,
  createLoadingLoadable,
  createReadyLoadable,
  Loadable,
} from '../../../shared/state/loadable';
import {
  ServiceOrderDetailVm,
  ServiceOrderListItemVm,
} from '../models/service-orders.vm';

export type OrdersFilterKey = 'PendingApproval' | 'Open' | 'Closed' | 'All';

const CLOSED_STATUSES: ReadonlySet<ServiceOrderStatus> = new Set<ServiceOrderStatus>([
  'Completed',
  'Cancelled',
]);

export function getOrderSortRank(order: ServiceOrderListItemVm): number {
  if (order.status === 'PendingApproval' && order.priority === 'High') {
    return 0;
  }
  if (order.status === 'PendingApproval') {
    return 1;
  }
  if (!CLOSED_STATUSES.has(order.status)) {
    return 2;
  }
  return 3;
}

export function sortServiceOrders(
  items: readonly ServiceOrderListItemVm[]
): readonly ServiceOrderListItemVm[] {
  return [...items].sort((a: ServiceOrderListItemVm, b: ServiceOrderListItemVm) => {
    const rankDiff: number = getOrderSortRank(a) - getOrderSortRank(b);
    if (rankDiff !== 0) {
      return rankDiff;
    }
    return a.number.localeCompare(b.number);
  });
}

@Injectable({
  providedIn: 'root',
})
export class ServiceOrdersStore {
  private readonly listLoadableSignal: WritableSignal<
    Loadable<readonly ServiceOrderListItemVm[]>
  > = signal<Loadable<readonly ServiceOrderListItemVm[]>>(createIdleLoadable([]));
  private readonly detailLoadableSignal: WritableSignal<Loadable<ServiceOrderDetailVm | null>> =
    signal<Loadable<ServiceOrderDetailVm | null>>(createIdleLoadable(null));
  private readonly activeFilterSignal: WritableSignal<OrdersFilterKey> =
    signal<OrdersFilterKey>('PendingApproval');
  private readonly flashedOrderIdsSignal: WritableSignal<ReadonlySet<string>> = signal<
    ReadonlySet<string>
  >(new Set<string>());

  readonly listLoadable: Signal<Loadable<readonly ServiceOrderListItemVm[]>> =
    this.listLoadableSignal.asReadonly();
  readonly detailLoadable: Signal<Loadable<ServiceOrderDetailVm | null>> =
    this.detailLoadableSignal.asReadonly();
  readonly activeFilter: Signal<OrdersFilterKey> = this.activeFilterSignal.asReadonly();
  readonly flashedOrderIds: Signal<ReadonlySet<string>> =
    this.flashedOrderIdsSignal.asReadonly();

  readonly sortedOrders: Signal<readonly ServiceOrderListItemVm[]> = computed(() =>
    sortServiceOrders(this.listLoadableSignal().data)
  );

  readonly filteredOrders: Signal<readonly ServiceOrderListItemVm[]> = computed(() => {
    const items: readonly ServiceOrderListItemVm[] = this.sortedOrders();
    const filter: OrdersFilterKey = this.activeFilterSignal();

    return items.filter((item: ServiceOrderListItemVm) => {
      if (filter === 'All') {
        return true;
      }
      if (filter === 'PendingApproval') {
        return item.status === 'PendingApproval';
      }
      if (filter === 'Closed') {
        return CLOSED_STATUSES.has(item.status);
      }
      return !CLOSED_STATUSES.has(item.status);
    });
  });

  readonly filterCounts: Signal<Readonly<Record<OrdersFilterKey, number>>> = computed(() => {
    const items: readonly ServiceOrderListItemVm[] = this.sortedOrders();
    let pendingApproval = 0;
    let open = 0;
    let closed = 0;

    for (const item of items) {
      if (item.status === 'PendingApproval') {
        pendingApproval += 1;
      }
      if (CLOSED_STATUSES.has(item.status)) {
        closed += 1;
      } else {
        open += 1;
      }
    }

    return {
      PendingApproval: pendingApproval,
      Open: open,
      Closed: closed,
      All: items.length,
    };
  });

  setListLoading(): void {
    this.listLoadableSignal.update((prev: Loadable<readonly ServiceOrderListItemVm[]>) =>
      createLoadingLoadable(prev.data)
    );
  }

  setListReady(items: readonly ServiceOrderListItemVm[]): void {
    this.listLoadableSignal.set(createReadyLoadable(items));
  }

  setListError(message: string): void {
    this.listLoadableSignal.update((prev: Loadable<readonly ServiceOrderListItemVm[]>) =>
      createErrorLoadable(prev.data, message)
    );
  }

  setDetailLoading(): void {
    this.detailLoadableSignal.update((prev: Loadable<ServiceOrderDetailVm | null>) =>
      createLoadingLoadable(prev.data)
    );
  }

  setDetailReady(detail: ServiceOrderDetailVm | null): void {
    this.detailLoadableSignal.set(createReadyLoadable(detail));
  }

  setDetailError(message: string): void {
    this.detailLoadableSignal.update((prev: Loadable<ServiceOrderDetailVm | null>) =>
      createErrorLoadable(prev.data, message)
    );
  }

  setActiveFilter(filter: OrdersFilterKey): void {
    this.activeFilterSignal.set(filter);
  }

  markOrderFlashed(orderId: string, durationMs = 600): void {
    this.flashedOrderIdsSignal.update((prev: ReadonlySet<string>) => {
      const next: Set<string> = new Set<string>(prev);
      next.add(orderId);
      return next;
    });
    if (durationMs > 0) {
      setTimeout(() => {
        this.flashedOrderIdsSignal.update((prev: ReadonlySet<string>) => {
          const next: Set<string> = new Set<string>(prev);
          next.delete(orderId);
          return next;
        });
      }, durationMs);
    }
  }
}

