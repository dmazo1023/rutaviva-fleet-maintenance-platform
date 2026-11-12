import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { ApiClient } from '../api/api-client';
import { ApiRoutes } from '../api/api-routes';
import { SessionStore } from '../context/session.store';
import { ToastStore } from '../feedback/toast.store';
import { ENUM_LABELS } from '../models/enum-labels';
import { PROFILE_VALUES, Profile } from '../models/enums';
import { NotificationsService, RealtimeConnectionState } from '../realtime/notifications.service';
import { SHELL_TEXTS } from './shell.texts';
import { ViewRefreshService } from './view-refresh.service';

export interface ProfileOption {
  readonly value: Profile;
  readonly label: string;
}

@Component({
  selector: 'app-top-bar',
  templateUrl: './top-bar.component.html',
  styleUrl: './top-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopBarComponent {
  protected readonly sessionStore: SessionStore = inject(SessionStore);
  protected readonly notifications: NotificationsService = inject(NotificationsService);
  private readonly apiClient: ApiClient = inject(ApiClient);
  private readonly toastStore: ToastStore = inject(ToastStore);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);

  protected readonly texts = SHELL_TEXTS;
  protected readonly isRefreshing = signal<boolean>(false);

  protected readonly profileOptions: readonly ProfileOption[] = PROFILE_VALUES.map(
    (value: Profile): ProfileOption => ({
      value,
      label: ENUM_LABELS.profile[value],
    })
  );

  protected readonly isLiveConnected = computed<boolean>(
    () => this.notifications.connectionState() === 'connected'
  );

  protected readonly liveLabel = computed<string>(() => {
    const labels: Readonly<Record<RealtimeConnectionState, string>> = {
      connected: this.texts.liveOnline,
      reconnecting: this.texts.liveReconnecting,
      disconnected: this.texts.liveReconnecting,
      unavailable: this.texts.liveUnavailable,
    };
    return labels[this.notifications.connectionState()];
  });

  protected readonly refreshButtonLabel = computed<string>(() =>
    this.isRefreshing() ? this.texts.refreshBusyText : this.texts.refreshTooltip
  );

  protected onCustomerChange(event: Event): void {
    const selectEl: HTMLSelectElement = event.target as HTMLSelectElement;
    this.sessionStore.setCustomer(selectEl.value);
    this.viewRefresh.requestRefresh();
  }

  protected onProfileChange(event: Event): void {
    const selectEl: HTMLSelectElement = event.target as HTMLSelectElement;
    this.sessionStore.setProfile(selectEl.value as Profile);
    this.viewRefresh.requestRefresh();
  }

  protected onActorChange(event: Event): void {
    const selectEl: HTMLSelectElement = event.target as HTMLSelectElement;
    this.sessionStore.setActor(selectEl.value || null);
    this.viewRefresh.requestRefresh();
  }

  protected onRefreshClick(): void {
    if (this.isRefreshing()) {
      return;
    }
    this.isRefreshing.set(true);
    const vehicleId: string | null = this.viewRefresh.getActiveVehicleId();
    const payload: { readonly vehicleId?: string } = vehicleId ? { vehicleId } : {};

    this.apiClient
      .post(ApiRoutes.maintenance.reevaluations(), payload)
      .pipe(finalize(() => this.isRefreshing.set(false)))
      .subscribe({
        next: () => {
          this.viewRefresh.requestRefresh();
          this.toastStore.show({ message: this.texts.refreshDoneToast });
        },
        error: () => {
          return;
        },
      });
  }
}
