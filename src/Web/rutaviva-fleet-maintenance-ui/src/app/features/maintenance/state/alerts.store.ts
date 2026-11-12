import { computed, DestroyRef, inject, Injectable, Signal, signal, WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SessionStore } from '../../../core/context/session.store';
import { ToastStore } from '../../../core/feedback/toast.store';
import { AlertStatus } from '../../../core/models/enums';
import { NotificationsService } from '../../../core/realtime/notifications.service';
import { AlertChangedEvent } from '../../../core/realtime/realtime-events';
import { ViewRefreshService } from '../../../core/shell/view-refresh.service';
import {
  createErrorLoadable,
  createIdleLoadable,
  createLoadingLoadable,
  createReadyLoadable,
  Loadable,
} from '../../../shared/state/loadable';
import { forkJoin } from 'rxjs';
import { ALERT_STATUSES_TO_COUNT, MaintenanceApi } from '../data/maintenance.api';
import { MAINTENANCE_TEXTS } from '../maintenance.texts';
import { AlertsPageVm, AlertVm } from '../models/maintenance.vm';

const FLASH_DURATION_MS = 600;

@Injectable({
  providedIn: 'root',
})
export class AlertsStore {
  private readonly maintenanceApi: MaintenanceApi = inject(MaintenanceApi);
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly notifications: NotificationsService = inject(NotificationsService);
  private readonly toastStore: ToastStore = inject(ToastStore);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);
  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  private readonly alertsLoadableSignal: WritableSignal<Loadable<readonly AlertVm[]>> = signal<
    Loadable<readonly AlertVm[]>
  >(createIdleLoadable([]));
  private readonly activeStatusSignal: WritableSignal<AlertStatus> =
    signal<AlertStatus>('Active');
  private readonly countsSignal: WritableSignal<Readonly<Record<AlertStatus, number>>> = signal<
    Readonly<Record<AlertStatus, number>>
  >({ Active: 0, Acknowledged: 0, Resolved: 0 });
  private readonly flashedAlertIdsSignal: WritableSignal<ReadonlySet<string>> = signal<
    ReadonlySet<string>
  >(new Set<string>());

  readonly alertsLoadable: Signal<Loadable<readonly AlertVm[]>> =
    this.alertsLoadableSignal.asReadonly();
  readonly activeStatus: Signal<AlertStatus> = this.activeStatusSignal.asReadonly();
  readonly flashedAlertIds: Signal<ReadonlySet<string>> =
    this.flashedAlertIdsSignal.asReadonly();

  readonly filteredAlerts: Signal<readonly AlertVm[]> = computed(() => {
    const status: AlertStatus = this.activeStatusSignal();
    return this.alertsLoadableSignal().data.filter((a: AlertVm) => a.status === status);
  });

  readonly countsByStatus: Signal<Readonly<Record<AlertStatus, number>>> =
    this.countsSignal.asReadonly();

  constructor() {
    this.notifications.alertChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event: AlertChangedEvent) => {
        this.markAlertFlashed(event.alertId);
        this.loadAlerts();
      });

    this.viewRefresh.refresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadAlerts();
      });
  }

  loadAlerts(): void {
    const customerId: string | undefined = this.sessionStore.customerId() || undefined;
    this.alertsLoadableSignal.update((prev: Loadable<readonly AlertVm[]>) =>
      createLoadingLoadable(prev.data)
    );

    forkJoin(
      ALERT_STATUSES_TO_COUNT.map((status: AlertStatus) =>
        this.maintenanceApi.getAlerts({ customerId, status })
      )
    ).subscribe({
      next: (pages: readonly AlertsPageVm[]) => this.applyPages(pages),
      error: () => {
        this.alertsLoadableSignal.update((prev: Loadable<readonly AlertVm[]>) =>
          createErrorLoadable(prev.data, MAINTENANCE_TEXTS.alertsErrorMessage)
        );
      },
    });
  }

  setActiveStatus(status: AlertStatus): void {
    this.activeStatusSignal.set(status);
  }

  acknowledge(alertId: string): void {
    this.maintenanceApi.acknowledgeAlert(alertId).subscribe({
      next: () => {
        this.toastStore.show({ message: MAINTENANCE_TEXTS.acknowledgedToast });
        this.loadAlerts();
      },
      error: () => {
        this.toastStore.show({ message: MAINTENANCE_TEXTS.acknowledgeErrorToast, isError: true });
      },
    });
  }

  markAlertFlashed(alertId: string, durationMs = FLASH_DURATION_MS): void {
    this.flashedAlertIdsSignal.update((prev: ReadonlySet<string>) => {
      const next: Set<string> = new Set<string>(prev);
      next.add(alertId);
      return next;
    });
    if (durationMs > 0) {
      setTimeout(() => {
        this.flashedAlertIdsSignal.update((prev: ReadonlySet<string>) => {
          const next: Set<string> = new Set<string>(prev);
          next.delete(alertId);
          return next;
        });
      }, durationMs);
    }
  }

  private applyPages(pages: readonly AlertsPageVm[]): void {
    const counts: Record<AlertStatus, number> = { Active: 0, Acknowledged: 0, Resolved: 0 };
    ALERT_STATUSES_TO_COUNT.forEach((status: AlertStatus, index: number) => {
      counts[status] = pages[index]?.totalCount ?? 0;
    });
    this.countsSignal.set(counts);
    this.alertsLoadableSignal.set(
      createReadyLoadable(pages.flatMap((page: AlertsPageVm) => page.items))
    );
    this.sessionStore.setActiveAlertsCount(counts.Active);
  }
}
