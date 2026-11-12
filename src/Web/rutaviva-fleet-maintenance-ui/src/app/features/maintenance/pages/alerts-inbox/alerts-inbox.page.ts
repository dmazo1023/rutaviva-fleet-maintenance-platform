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
import { ENUM_LABELS } from '../../../../core/models/enum-labels';
import { AlertResolutionReason, AlertStatus } from '../../../../core/models/enums';
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
import { MAINTENANCE_TEXTS } from '../../maintenance.texts';
import { AlertVm } from '../../models/maintenance.vm';
import { AlertsStore } from '../../state/alerts.store';

@Component({
  selector: 'app-alerts-inbox-page',
  imports: [
    SegmentedFilterComponent,
    PlateChipComponent,
    StatusPillComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SkeletonComponent,
  ],
  templateUrl: './alerts-inbox.page.html',
  styleUrl: './alerts-inbox.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlertsInboxPageComponent implements OnInit {
  private readonly alertsStore: AlertsStore = inject(AlertsStore);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);

  readonly status: InputSignal<string | undefined> = input<string | undefined>(undefined);

  readonly texts: typeof MAINTENANCE_TEXTS = MAINTENANCE_TEXTS;
  readonly labels = ENUM_LABELS;
  readonly alertsLoadable = this.alertsStore.alertsLoadable;
  readonly activeStatus = this.alertsStore.activeStatus;
  readonly filteredAlerts = this.alertsStore.filteredAlerts;
  readonly flashedAlertIds = this.alertsStore.flashedAlertIds;

  readonly filterOptions: Signal<readonly SegmentedFilterOption[]> = computed(() => {
    const counts = this.alertsStore.countsByStatus();
    return [
      {
        value: 'Active',
        label: this.texts.alertsFilterActive,
        count: counts.Active,
        toneClass: 'is-danger',
      },
      {
        value: 'Acknowledged',
        label: this.texts.alertsFilterAcknowledged,
        count: counts.Acknowledged,
        toneClass: 'is-warn',
      },
      {
        value: 'Resolved',
        label: this.texts.alertsFilterResolved,
        count: counts.Resolved,
        toneClass: 'is-ok',
      },
    ];
  });

  constructor() {
    effect(() => {
      const param: string | undefined = this.status();
      if (param === 'Active' || param === 'Acknowledged' || param === 'Resolved') {
        this.alertsStore.setActiveStatus(param);
      } else if (param === undefined) {
        this.alertsStore.setActiveStatus('Active');
      }
    });
  }

  ngOnInit(): void {
    this.viewRefresh.setActiveVehicleId(null);
    this.alertsStore.loadAlerts();
  }

  isFlashed(alertId: string): boolean {
    return this.flashedAlertIds().has(alertId);
  }

  getResolutionLabel(reason: AlertResolutionReason | null): string {
    if (!reason) {
      return '';
    }
    return this.labels.alertResolutionReason[reason];
  }

  onFilterChange(value: string): void {
    if (value === 'Active' || value === 'Acknowledged' || value === 'Resolved') {
      this.alertsStore.setActiveStatus(value as AlertStatus);
    }
  }

  onAcknowledge(alert: AlertVm): void {
    this.alertsStore.acknowledge(alert.alertId);
  }

  onRetry(): void {
    this.alertsStore.loadAlerts();
  }
}

