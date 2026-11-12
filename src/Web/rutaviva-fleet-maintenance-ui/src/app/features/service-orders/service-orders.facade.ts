import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, inject, Injectable, Signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, Observable, tap, throwError } from 'rxjs';
import { SessionStore } from '../../core/context/session.store';
import { ToastStore } from '../../core/feedback/toast.store';
import { NotificationsService } from '../../core/realtime/notifications.service';
import {
  ApprovalRequestedEvent,
  ServiceOrderChangedEvent,
} from '../../core/realtime/realtime-events';
import { ViewRefreshService } from '../../core/shell/view-refresh.service';
import { Loadable } from '../../shared/state/loadable';
import { ServiceOrdersApi } from './data/service-orders.api';
import {
  ServiceOrderDetailVm,
  ServiceOrderListItemVm,
} from './models/service-orders.vm';
import { SERVICE_ORDERS_TEXTS } from './service-orders.texts';
import { OrdersFilterKey, ServiceOrdersStore } from './state/service-orders.store';

const HTTP_CONFLICT = 409;

@Injectable({
  providedIn: 'root',
})
export class ServiceOrdersFacade {
  private readonly api: ServiceOrdersApi = inject(ServiceOrdersApi);
  private readonly store: ServiceOrdersStore = inject(ServiceOrdersStore);
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly notifications: NotificationsService = inject(NotificationsService);
  private readonly toastStore: ToastStore = inject(ToastStore);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);
  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  private activeOrderId: string | null = null;

  readonly listLoadable: Signal<Loadable<readonly ServiceOrderListItemVm[]>> =
    this.store.listLoadable;
  readonly detailLoadable: Signal<Loadable<ServiceOrderDetailVm | null>> =
    this.store.detailLoadable;
  readonly sortedOrders: Signal<readonly ServiceOrderListItemVm[]> = this.store.sortedOrders;
  readonly filteredOrders: Signal<readonly ServiceOrderListItemVm[]> = this.store.filteredOrders;
  readonly filterCounts: Signal<Readonly<Record<OrdersFilterKey, number>>> =
    this.store.filterCounts;
  readonly activeFilter: Signal<OrdersFilterKey> = this.store.activeFilter;
  readonly flashedOrderIds: Signal<ReadonlySet<string>> = this.store.flashedOrderIds;

  constructor() {
    this.bindRealtimeEvents();
    this.viewRefresh.refresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadOrders();
        if (this.activeOrderId) {
          this.loadOrderDetail(this.activeOrderId);
        }
      });
  }

  loadOrders(): void {
    const customerId: string | undefined = this.sessionStore.customerId() || undefined;
    this.store.setListLoading();

    this.api.getOrders({ customerId }).subscribe({
      next: (orders: readonly ServiceOrderListItemVm[]) => {
        this.store.setListReady(orders);
        const pendingCount: number = orders.filter(
          (o: ServiceOrderListItemVm) => o.status === 'PendingApproval'
        ).length;
        this.sessionStore.setPendingApprovalsCount(pendingCount);
      },
      error: () => {
        this.store.setListError(SERVICE_ORDERS_TEXTS.errorMessage);
      },
    });
  }

  loadOrderDetail(orderId: string): void {
    this.activeOrderId = orderId;
    this.store.setDetailLoading();

    this.api.getOrderById(orderId, this.sessionStore.profile()).subscribe({
      next: (detail: ServiceOrderDetailVm | null) => {
        this.store.setDetailReady(detail);
      },
      error: () => {
        this.store.setDetailError(SERVICE_ORDERS_TEXTS.errorMessage);
      },
    });
  }

  setActiveFilter(filter: OrdersFilterKey): void {
    this.store.setActiveFilter(filter);
  }

  quote(orderId: string, amount: number): Observable<ServiceOrderDetailVm> {
    return this.executeMutation(orderId, () =>
      this.api.quote(orderId, { amount, quotedBy: this.sessionStore.currentActorName() })
    );
  }

  approve(orderId: string): Observable<ServiceOrderDetailVm> {
    return this.asApprover((approverId: string) =>
      this.executeMutation(orderId, () =>
        this.api.approve(orderId, {
          approvedBy: this.sessionStore.currentActorName(),
          approverId,
        })
      )
    );
  }

  reject(orderId: string): Observable<ServiceOrderDetailVm> {
    return this.asApprover((approverId: string) =>
      this.executeMutation(orderId, () =>
        this.api.reject(orderId, {
          rejectedBy: this.sessionStore.currentActorName(),
          approverId,
        })
      )
    );
  }

  cancel(orderId: string): Observable<ServiceOrderDetailVm> {
    return this.executeMutation(orderId, () =>
      this.api.cancel(orderId, { cancelledBy: this.sessionStore.currentActorName() })
    );
  }

  start(orderId: string): Observable<ServiceOrderDetailVm> {
    return this.executeMutation(orderId, () =>
      this.api.start(orderId, { startedBy: this.sessionStore.currentActorName() })
    );
  }

  complete(orderId: string, servicedKm: number): Observable<ServiceOrderDetailVm> {
    return this.executeMutation(orderId, () =>
      this.api.complete(orderId, {
        completedBy: this.sessionStore.currentActorName(),
        servicedKm,
      })
    );
  }

  vehicleOdometerKm(vehicleId: string): Observable<number | null> {
    return this.api.getVehicleOdometerKm(vehicleId);
  }

  private asApprover(
    call: (approverId: string) => Observable<ServiceOrderDetailVm>
  ): Observable<ServiceOrderDetailVm> {
    const approverId: string | null = this.sessionStore.actorId();
    if (!approverId) {
      this.toastStore.show({ message: SERVICE_ORDERS_TEXTS.approverRequiredMessage, tone: 'warn' });
      return EMPTY;
    }
    return call(approverId);
  }

  private executeMutation(
    orderId: string,
    call: () => Observable<ServiceOrderDetailVm>
  ): Observable<ServiceOrderDetailVm> {
    return call().pipe(
      tap((updated: ServiceOrderDetailVm) => {
        this.store.setDetailReady(updated);
        this.store.markOrderFlashed(orderId);
        this.loadOrders();
      }),
      catchError((err: unknown) => {
        if (err instanceof HttpErrorResponse && err.status === HTTP_CONFLICT) {
          this.loadOrderDetail(orderId);
          this.loadOrders();
        }
        return throwError(() => err);
      })
    );
  }

  private bindRealtimeEvents(): void {
    this.notifications.serviceOrderChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: ServiceOrderChangedEvent) => {
        this.store.markOrderFlashed(event.serviceOrderId);
        this.loadOrders();
        if (this.activeOrderId === event.serviceOrderId) {
          this.loadOrderDetail(event.serviceOrderId);
        }
      });

    this.notifications.approvalRequested$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: ApprovalRequestedEvent) => {
        this.store.markOrderFlashed(event.serviceOrderId);
        this.loadOrders();
        if (this.activeOrderId === event.serviceOrderId) {
          this.loadOrderDetail(event.serviceOrderId);
        }
        if (event.priority === 'High') {
          this.toastStore.show({
            message: `${SERVICE_ORDERS_TEXTS.highPriorityToastPrefix}${event.number} (${event.plate})`,
            tone: 'warn',
            persistent: true,
            action: {
              label: SERVICE_ORDERS_TEXTS.viewOrderAction,
              route: `/ordenes/${event.serviceOrderId}`,
            },
          });
        }
      });
  }
}
