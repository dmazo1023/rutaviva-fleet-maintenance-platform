import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Params, Router } from '@angular/router';
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

interface ProfileLanding {
  readonly path: readonly string[];
  readonly queryParams: Params;
}

const PROFILE_LANDINGS: Readonly<Record<Profile, ProfileLanding>> = {
  Operator: { path: ['/flota'], queryParams: {} },
  Approver: { path: ['/ordenes'], queryParams: { statusGroup: 'PendingApproval' } },
  Workshop: { path: ['/ordenes'], queryParams: { statusGroup: 'Open' } },
};

const WORKING_AS_ICON = 'badge';

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
  private readonly router: Router = inject(Router);

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
    const profile: Profile = selectEl.value as Profile;
    this.sessionStore.setProfile(profile);
    this.viewRefresh.requestRefresh();
    this.announceWorkingAs();
    this.goToProfileLanding(profile);
  }

  protected onActorChange(event: Event): void {
    const selectEl: HTMLSelectElement = event.target as HTMLSelectElement;
    this.sessionStore.setActor(selectEl.value || null);
    this.viewRefresh.requestRefresh();
    this.announceWorkingAs();
  }

  private goToProfileLanding(profile: Profile): void {
    const landing: ProfileLanding = PROFILE_LANDINGS[profile];
    void this.router.navigate(landing.path, { queryParams: landing.queryParams });
  }

  private announceWorkingAs(): void {
    this.toastStore.show({ message: this.workingAsMessage(), icon: WORKING_AS_ICON });
  }

  private workingAsMessage(): string {
    const profile: Profile = this.sessionStore.profile();
    const base: string = this.texts.workingAsPrefix + ENUM_LABELS.profile[profile];
    const hasNamedActor: boolean = profile !== 'Operator' && this.sessionStore.actorId() !== null;
    return hasNamedActor
      ? base + this.texts.workingAsSeparator + this.sessionStore.currentActorName()
      : base;
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
