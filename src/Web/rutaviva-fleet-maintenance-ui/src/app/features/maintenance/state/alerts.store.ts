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
import { MaintenanceApi } from '../data/maintenance.api';
import { MAINTENANCE_TEXTS } from '../maintenance.texts';
import { AlertVm } from '../models/maintenance.vm';

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

  readonly countsByStatus: Signal<Readonly<Record<AlertStatus, number>>> = computed(() => {
    const items: readonly AlertVm[] = this.alertsLoadableSignal().data;
    let active = 0;
    let acknowledged = 0;
    let resolved = 0;

    for (const item of items) {
      if (item.status === 'Active') {
        active += 1;
      } else if (item.status === 'Acknowledged') {
        acknowledged += 1;
      } else {
        resolved += 1;
      }
    }

    return {
      Active: active,
      Acknowledged: acknowledged,
      Resolved: resolved,
    };
  });

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

    this.maintenanceApi.getAlerts({ customerId }).subscribe({
      next: (alerts: readonly AlertVm[]) => {
        this.alertsLoadableSignal.set(createReadyLoadable(alerts));
        const activeCount: number = alerts.filter((a: AlertVm) => a.status === 'Active').length;
        this.sessionStore.setActiveAlertsCount(activeCount);
      },
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
    const actor: string = this.sessionStore.currentActorName();
    this.maintenanceApi.acknowledgeAlert(alertId, actor).subscribe({
      next: (updated: AlertVm) => {
        this.alertsLoadableSignal.update((prev: Loadable<readonly AlertVm[]>) => {
          const nextList: readonly AlertVm[] = prev.data.map((a: AlertVm) =>
            a.alertId === updated.alertId ? updated : a
          );
          const activeCount: number = nextList.filter((a: AlertVm) => a.status === 'Active').length;
          this.sessionStore.setActiveAlertsCount(activeCount);
          return createReadyLoadable(nextList);
        });
        this.toastStore.show({ message: MAINTENANCE_TEXTS.acknowledgedToast });
      },
    });
  }

  markAlertFlashed(alertId: string, durationMs = 600): void {
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
}

