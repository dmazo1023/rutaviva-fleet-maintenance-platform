import { HttpErrorResponse } from '@angular/common/http';
import { computed, DestroyRef, inject, Injectable, Signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, Observable, tap, throwError } from 'rxjs';
import { ApproverOption, SessionStore } from '../../core/context/session.store';
import { ServiceOrdersQuery } from '../../core/api/api-routes';
import { Profile } from '../../core/models/enums';
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

const ACTOR_REQUIRED_MESSAGES: Readonly<Partial<Record<Profile, string>>> = {
  Approver: SERVICE_ORDERS_TEXTS.approverRequiredInboxMessage,
  Workshop: SERVICE_ORDERS_TEXTS.workshopRequiredInboxMessage,
};

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

  readonly ordersScope: Signal<ServiceOrdersQuery | null> = computed(() => {
    const scopes: Readonly<Record<Profile, () => ServiceOrdersQuery | null>> = {
      Operator: () => ({ customerId: this.sessionStore.customerId() || undefined }),
      Approver: () => this.approverScope(),
      Workshop: () => this.workshopScope(),
    };
    return scopes[this.sessionStore.profile()]();
  });

  readonly actorRequiredMessage: Signal<string | null> = computed(() =>
    this.ordersScope() ? null : (ACTOR_REQUIRED_MESSAGES[this.sessionStore.profile()] ?? null)
  );

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
    const scope: ServiceOrdersQuery | null = this.ordersScope();
    if (!scope) {
      this.store.setListReady([]);
      this.sessionStore.setPendingApprovalsCount(0);
      return;
    }
    this.store.setListLoading();

    this.api.getOrders(scope).subscribe({
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
    return this.asWorkshop((workshopId: string) =>
      this.executeMutation(orderId, () =>
        this.api.quote(orderId, {
          amount,
          quotedBy: this.sessionStore.currentActorName(),
          workshopId,
        })
      )
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
    return this.asWorkshop((workshopId: string) =>
      this.executeMutation(orderId, () =>
        this.api.start(orderId, { startedBy: this.sessionStore.currentActorName(), workshopId })
      )
    );
  }

  complete(orderId: string, servicedKm: number): Observable<ServiceOrderDetailVm> {
    return this.asWorkshop((workshopId: string) =>
      this.executeMutation(orderId, () =>
        this.api.complete(orderId, {
          completedBy: this.sessionStore.currentActorName(),
          servicedKm,
          workshopId,
        })
      )
    );
  }

  vehicleOdometerKm(vehicleId: string): Observable<number | null> {
    return this.api.getVehicleOdometerKm(vehicleId);
  }

  private approverScope(): ServiceOrdersQuery | null {
    const actorId: string | null = this.sessionStore.actorId();
    const approver: ApproverOption | undefined = this.sessionStore
      .availableApprovers()
      .find((item: ApproverOption) => item.id === actorId);
    return approver ? { customerId: approver.customerId } : null;
  }

  private workshopScope(): ServiceOrdersQuery | null {
    const actorId: string | null = this.sessionStore.actorId();
    return actorId ? { workshopId: actorId } : null;
  }

  private asWorkshop(
    call: (workshopId: string) => Observable<ServiceOrderDetailVm>
  ): Observable<ServiceOrderDetailVm> {
    const workshopId: string | null = this.sessionStore.actorId();
    if (!workshopId) {
      this.toastStore.show({ message: SERVICE_ORDERS_TEXTS.workshopRequiredMessage, tone: 'warn' });
      return EMPTY;
    }
    return call(workshopId);
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
