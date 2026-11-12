import { DestroyRef, inject, Injectable } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiClient } from '../api/api-client';
import { ApiRoutes } from '../api/api-routes';
import { SessionStore } from '../context/session.store';
import { NotificationsService } from '../realtime/notifications.service';
import { ViewRefreshService } from './view-refresh.service';

interface CountedPageDto {
  readonly totalCount: number;
}

@Injectable({
  providedIn: 'root',
})
export class ShellCountersService {
  private readonly apiClient: ApiClient = inject(ApiClient);
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly notifications: NotificationsService = inject(NotificationsService);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);
  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor() {
    this.viewRefresh.refresh$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadActiveAlerts();
    });
    this.notifications.alertChanged$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadActiveAlerts();
    });
  }

  loadActiveAlerts(): void {
    const customerId: string | undefined = this.sessionStore.customerId() || undefined;
    this.apiClient
      .get<CountedPageDto>(ApiRoutes.maintenance.alerts({ customerId, status: 'Active' }))
      .subscribe({
        next: (page: CountedPageDto) => this.sessionStore.setActiveAlertsCount(page.totalCount),
        error: () => undefined,
      });
  }
}
