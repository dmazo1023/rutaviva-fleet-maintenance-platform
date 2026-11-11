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
import { Observable } from 'rxjs';
import { SessionStore } from '../../../../core/context/session.store';
import { ServiceOrderAction } from '../../../../core/models/enums';
import { ViewRefreshService } from '../../../../core/shell/view-refresh.service';
import { ErrorStateComponent } from '../../../../shared/error-state/error-state.component';
import { PlateChipComponent } from '../../../../shared/plate-chip/plate-chip.component';
import { SkeletonComponent } from '../../../../shared/skeleton/skeleton.component';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { OrderActionsComponent } from '../../components/order-actions/order-actions.component';
import { OrderHistoryComponent } from '../../components/order-history/order-history.component';
import { OrderStepperComponent } from '../../components/order-stepper/order-stepper.component';
import {
  PriceLineEvent,
  ServiceLinesComponent,
} from '../../components/service-lines/service-lines.component';
import {
  ACTION_FORMS,
  ACTION_IMPACT,
  OrderFormDialogComponent,
  OrderFormSubmitEvent,
} from '../../dialogs/order-form.dialog';
import { ServiceOrderDetailVm } from '../../models/service-orders.vm';
import { ServiceOrdersFacade } from '../../service-orders.facade';
import { SERVICE_ORDERS_TEXTS } from '../../service-orders.texts';

const COP_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');

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

  readonly dialogAction: WritableSignal<ServiceOrderAction | null> =
    signal<ServiceOrderAction | null>(null);
  readonly submittingAction: WritableSignal<boolean> = signal<boolean>(false);

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

  onActionTriggered(orderId: string, action: ServiceOrderAction): void {
    if (ACTION_FORMS[action] || ACTION_IMPACT[action]) {
      this.dialogAction.set(action);
      return;
    }

    if (action === 'Approve') {
      this.facade.approve(orderId).subscribe();
    } else if (action === 'MarkOnSite') {
      this.facade.markOnSite(orderId).subscribe();
    } else if (action === 'Start') {
      this.facade.start(orderId).subscribe();
    } else if (action === 'Complete') {
      this.facade.complete(orderId).subscribe();
    }
  }

  onDialogClosed(): void {
    this.dialogAction.set(null);
  }

  onDialogSubmitted(orderId: string, event: OrderFormSubmitEvent): void {
    this.submittingAction.set(true);
    let call$: Observable<ServiceOrderDetailVm> | null = null;

    if (event.action === 'Quote' && event.amount !== undefined) {
      call$ = this.facade.quote(orderId, event.amount);
    } else if (event.action === 'ScheduleAppointment' && event.appointmentAt) {
      call$ = this.facade.scheduleAppointment(orderId, event.appointmentAt);
    } else if (
      event.action === 'AddAdditionalService' &&
      event.description &&
      event.amount !== undefined
    ) {
      call$ = this.facade.addAdditionalService(orderId, event.description, event.amount);
    } else if (event.action === 'RequestAdditionalService' && event.description) {
      call$ = this.facade.requestAdditionalService(orderId, event.description);
    } else if (event.action === 'Reject') {
      call$ = this.facade.reject(orderId);
    } else if (event.action === 'Cancel') {
      call$ = this.facade.cancel(orderId);
    }

    if (!call$) {
      this.submittingAction.set(false);
      return;
    }

    call$.subscribe({
      next: () => {
        this.submittingAction.set(false);
        this.dialogAction.set(null);
      },
      error: () => {
        this.submittingAction.set(false);
      },
    });
  }

  onPriceSaved(orderId: string, event: PriceLineEvent): void {
    this.facade.priceAdditionalService(orderId, event.lineId, event.amount).subscribe();
  }

  onRetry(): void {
    this.facade.loadOrderDetail(this.id());
  }
}

