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
  untracked,
} from '@angular/core';
import { Router } from '@angular/router';
import { ENUM_LABELS } from '../../../../core/models/enum-labels';
import { ViewRefreshService } from '../../../../core/shell/view-refresh.service';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../../../shared/error-state/error-state.component';
import { PlateChipComponent } from '../../../../shared/plate-chip/plate-chip.component';
import {
  SegmentedFilterComponent,
  SegmentedFilterOption,
} from '../../../../shared/segmented-filter/segmented-filter.component';
import { SkeletonComponent } from '../../../../shared/skeleton/skeleton.component';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { ServiceOrderListItemVm } from '../../models/service-orders.vm';
import { ServiceOrdersFacade } from '../../service-orders.facade';
import { SERVICE_ORDERS_TEXTS } from '../../service-orders.texts';
import { OrdersFilterKey } from '../../state/service-orders.store';

const COP_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');
const VALID_FILTERS: ReadonlySet<string> = new Set<string>([
  'PendingApproval',
  'Open',
  'Closed',
  'All',
]);

@Component({
  selector: 'app-orders-inbox-page',
  imports: [
    SegmentedFilterComponent,
    PlateChipComponent,
    StatusPillComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SkeletonComponent,
  ],
  templateUrl: './orders-inbox.page.html',
  styleUrl: './orders-inbox.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrdersInboxPageComponent implements OnInit {
  private readonly facade: ServiceOrdersFacade = inject(ServiceOrdersFacade);
  private readonly router: Router = inject(Router);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);

  readonly statusGroup: InputSignal<string | undefined> = input<string | undefined>(undefined);

  readonly texts: typeof SERVICE_ORDERS_TEXTS = SERVICE_ORDERS_TEXTS;
  readonly labels = ENUM_LABELS;
  readonly listLoadable = this.facade.listLoadable;
  readonly filteredOrders = this.facade.filteredOrders;
  readonly activeFilter = this.facade.activeFilter;
  readonly flashedIds = this.facade.flashedOrderIds;

  readonly filterOptions: Signal<readonly SegmentedFilterOption[]> = computed(() => {
    const counts = this.facade.filterCounts();
    return [
      {
        value: 'PendingApproval',
        label: this.texts.filterPendingApproval,
        count: counts.PendingApproval,
        toneClass: 'is-warn',
      },
      {
        value: 'Open',
        label: this.texts.filterOpen,
        count: counts.Open,
        toneClass: 'is-info',
      },
      {
        value: 'Closed',
        label: this.texts.filterClosed,
        count: counts.Closed,
      },
      {
        value: 'All',
        label: this.texts.filterAll,
        count: counts.All,
      },
    ];
  });

  readonly emptyStateTitle: Signal<string> = computed(() => {
    const f: OrdersFilterKey = this.activeFilter();
    if (f === 'PendingApproval') {
      return this.texts.emptyPendingTitle;
    }
    if (f === 'Open') {
      return this.texts.emptyOpenTitle;
    }
    if (f === 'Closed') {
      return this.texts.emptyClosedTitle;
    }
    return this.texts.emptyAllTitle;
  });

  readonly actorRequiredMessage: Signal<string | null> = this.facade.actorRequiredMessage;

  constructor() {
    effect(() => {
      this.facade.ordersScope();
      untracked(() => this.facade.loadOrders());
    });

    effect(() => {
      const param: string | undefined = this.statusGroup();
      if (param && VALID_FILTERS.has(param)) {
        this.facade.setActiveFilter(param as OrdersFilterKey);
      } else if (param === undefined) {
        this.facade.setActiveFilter('PendingApproval');
      }
    });
  }

  ngOnInit(): void {
    this.viewRefresh.setActiveVehicleId(null);
  }

  isFlashed(orderId: string): boolean {
    return this.flashedIds().has(orderId);
  }

  formatCurrency(amount: number | null): string {
    if (amount === null) {
      return this.texts.pendingPriceText;
    }
    return `$ ${COP_FORMATTER.format(amount)}`;
  }

  getReasonLabel(order: ServiceOrderListItemVm): string | null {
    if (!order.reason) {
      return null;
    }
    return this.labels.approvalReason[order.reason];
  }

  onFilterChange(value: string): void {
    if (VALID_FILTERS.has(value)) {
      this.facade.setActiveFilter(value as OrdersFilterKey);
    }
  }

  onSelectOrder(orderId: string): void {
    void this.router.navigate(['/ordenes', orderId]);
  }

  onRetry(): void {
    this.facade.loadOrders();
  }
}

