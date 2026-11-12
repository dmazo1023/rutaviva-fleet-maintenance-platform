import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  InputSignal,
  Signal,
  signal,
  WritableSignal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize, Observable } from 'rxjs';
import { SessionStore } from '../../../../core/context/session.store';
import { ServiceOrderAction, ServiceOrderStatus } from '../../../../core/models/enums';
import { ViewRefreshService } from '../../../../core/shell/view-refresh.service';
import { ErrorStateComponent } from '../../../../shared/error-state/error-state.component';
import { PlateChipComponent } from '../../../../shared/plate-chip/plate-chip.component';
import { SkeletonComponent } from '../../../../shared/skeleton/skeleton.component';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { OrderActionsComponent } from '../../components/order-actions/order-actions.component';
import { OrderHistoryComponent } from '../../components/order-history/order-history.component';
import { OrderStepperComponent } from '../../components/order-stepper/order-stepper.component';
import { ServiceLinesComponent } from '../../components/service-lines/service-lines.component';
import {
  OrderFormDialogComponent,
  OrderFormSubmitEvent,
} from '../../dialogs/order-form.dialog';
import { ServiceOrderActionVm, ServiceOrderDetailVm } from '../../models/service-orders.vm';
import { ServiceOrdersFacade } from '../../service-orders.facade';
import { SERVICE_ORDERS_TEXTS } from '../../service-orders.texts';

const COP_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');

type DirectAction = 'Approve' | 'Start';

const DIRECT_ACTIONS: ReadonlySet<ServiceOrderAction> = new Set<ServiceOrderAction>([
  'Approve',
  'Start',
]);

function isDirectAction(action: ServiceOrderAction): action is DirectAction {
  return DIRECT_ACTIONS.has(action);
}

interface NextStepText {
  readonly text: string;
  readonly withWorkshop: boolean;
}

const NEXT_STEP_BY_STATUS: Readonly<Partial<Record<ServiceOrderStatus, NextStepText>>> = {
  Created: { text: SERVICE_ORDERS_TEXTS.nextStepWaitingQuote, withWorkshop: true },
  PendingApproval: { text: SERVICE_ORDERS_TEXTS.nextStepWaitingApproval, withWorkshop: false },
  Approved: { text: SERVICE_ORDERS_TEXTS.nextStepWaitingStart, withWorkshop: true },
  InProgress: { text: SERVICE_ORDERS_TEXTS.nextStepWaitingCompletion, withWorkshop: true },
};

const NON_PROGRESS_ACTIONS: ReadonlySet<ServiceOrderAction> = new Set<ServiceOrderAction>([
  'Cancel',
]);

function hasProgressAction(order: ServiceOrderDetailVm): boolean {
  return order.availableActions.some(
    (item: ServiceOrderActionVm) => item.enabled && !NON_PROGRESS_ACTIONS.has(item.action)
  );
}

export function nextStepNoticeOf(order: ServiceOrderDetailVm | null): string | null {
  const step: NextStepText | undefined = order ? NEXT_STEP_BY_STATUS[order.status] : undefined;
  if (!order || !step || hasProgressAction(order)) {
    return null;
  }
  return step.withWorkshop
    ? step.text + SERVICE_ORDERS_TEXTS.nextStepSeparator + order.workshopName
    : step.text;
}

@Component({
  selector: 'app-order-detail-page',
  imports: [
    RouterLink,
    PlateChipComponent,
    StatusPillComponent,
    SkeletonComponent,
    ErrorStateComponent,
    OrderStepperComponent,
    ServiceLinesComponent,
    OrderActionsComponent,
    OrderHistoryComponent,
    OrderFormDialogComponent,
  ],
  templateUrl: './order-detail.page.html',
  styleUrl: './order-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderDetailPageComponent {
  private readonly facade: ServiceOrdersFacade = inject(ServiceOrdersFacade);
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);

  readonly id: InputSignal<string> = input.required<string>();

  readonly texts: typeof SERVICE_ORDERS_TEXTS = SERVICE_ORDERS_TEXTS;
  readonly detailLoadable = this.facade.detailLoadable;
  readonly nextStepNotice: Signal<string | null> = computed(() =>
    nextStepNoticeOf(this.detailLoadable().data)
  );

  readonly dialogAction: WritableSignal<ServiceOrderAction | null> =
    signal<ServiceOrderAction | null>(null);
  readonly submittingAction: WritableSignal<boolean> = signal<boolean>(false);
  readonly dialogInitialAmount: WritableSignal<number | null> = signal<number | null>(null);

  readonly isWorkshop: Signal<boolean> = computed(
    () => this.sessionStore.profile() === 'Workshop'
  );

  constructor() {
    effect(() => {
      const orderId: string = this.id();
      if (orderId) {
        this.viewRefresh.setActiveVehicleId(null);
        this.facade.loadOrderDetail(orderId);
      }
    });
  }

  formatCop(amount: number): string {
    return `$ ${COP_FORMATTER.format(amount)}`;
  }

  onActionTriggered(order: ServiceOrderDetailVm, action: ServiceOrderAction): void {
    if (isDirectAction(action)) {
      this.runDirectAction(order.id, action);
      return;
    }
    this.openDialog(order, action);
  }

  onDialogClosed(): void {
    this.dialogAction.set(null);
  }

  onDialogSubmitted(orderId: string, event: OrderFormSubmitEvent): void {
    const call$: Observable<ServiceOrderDetailVm> | null = this.mutationFor(orderId, event);
    if (!call$) {
      return;
    }

    this.submittingAction.set(true);
    call$.pipe(finalize(() => this.submittingAction.set(false))).subscribe({
      next: () => this.dialogAction.set(null),
      error: () => undefined,
    });
  }

  private runDirectAction(orderId: string, action: DirectAction): void {
    const call$: Observable<ServiceOrderDetailVm> =
      action === 'Approve' ? this.facade.approve(orderId) : this.facade.start(orderId);
    call$.subscribe({ error: () => undefined });
  }

  private openDialog(order: ServiceOrderDetailVm, action: ServiceOrderAction): void {
    this.dialogInitialAmount.set(null);
    this.dialogAction.set(action);
    if (action === 'Complete') {
      this.facade
        .vehicleOdometerKm(order.vehicleId)
        .subscribe((km: number | null) => this.dialogInitialAmount.set(km));
    }
  }

  private mutationFor(
    orderId: string,
    event: OrderFormSubmitEvent
  ): Observable<ServiceOrderDetailVm> | null {
    const mutations: Partial<Record<ServiceOrderAction, () => Observable<ServiceOrderDetailVm>>> = {
      Quote: () => this.facade.quote(orderId, event.amount ?? 0),
      Complete: () => this.facade.complete(orderId, event.amount ?? 0),
      Reject: () => this.facade.reject(orderId),
      Cancel: () => this.facade.cancel(orderId),
    };
    return mutations[event.action]?.() ?? null;
  }

  onRetry(): void {
    this.facade.loadOrderDetail(this.id());
  }
}

