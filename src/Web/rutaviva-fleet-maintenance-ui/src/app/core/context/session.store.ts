import { computed, inject, Injectable, InjectionToken, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../api/api-client';
import { ApiRoutes } from '../api/api-routes';
import { ApproverStatus, CustomerStatus, Profile, WorkshopStatus } from '../models/enums';
import { NotificationsService } from '../realtime/notifications.service';
import { SESSION_TEXTS } from './session.texts';

export const SESSION_STORAGE_KEY = 'rutaviva.crm.session.v2';

export interface StorageLike {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export const BROWSER_STORAGE = new InjectionToken<StorageLike | null>('BROWSER_STORAGE', {
  providedIn: 'root',
  factory: (): StorageLike | null => {
    try {
      return typeof localStorage !== 'undefined' ? localStorage : null;
    } catch {
      return null;
    }
  },
});

export interface ApproverOption {
  readonly id: string;
  readonly customerId: string;
  readonly fullName: string;
  readonly email: string;
  readonly status: ApproverStatus;
}

export interface CustomerOption {
  readonly id: string;
  readonly taxId: string;
  readonly name: string;
  readonly status: CustomerStatus;
  readonly approvers: readonly ApproverOption[];
}

export interface WorkshopOption {
  readonly id: string;
  readonly taxId: string;
  readonly name: string;
  readonly city: string;
  readonly status: WorkshopStatus;
}

type CustomerSummary = Omit<CustomerOption, 'approvers'> & {
  readonly approvers?: readonly ApproverOption[];
};

interface CustomerDetail {
  readonly approvers?: readonly Partial<ApproverOption>[];
}

interface PersistedSession {
  readonly customerId: string;
  readonly profile: Profile;
  readonly actorId: string | null;
}

function withApprovers(customer: CustomerSummary): CustomerOption {
  return { ...customer, approvers: customer.approvers ?? [] };
}

function toApprover(approver: Partial<ApproverOption>, customerId: string): ApproverOption {
  return {
    id: approver.id ?? '',
    customerId: approver.customerId ?? customerId,
    fullName: approver.fullName ?? '',
    email: approver.email ?? '',
    status: approver.status ?? 'Active',
  };
}

@Injectable({
  providedIn: 'root',
})
export class SessionStore {
  private readonly apiClient: ApiClient = inject(ApiClient);
  private readonly notifications: NotificationsService = inject(NotificationsService);
  private readonly storage: StorageLike | null = inject(BROWSER_STORAGE);

  private readonly customerIdSignal = signal<string>('');
  private readonly profileSignal = signal<Profile>('Operator');
  private readonly actorIdSignal = signal<string | null>(null);
  private readonly customersSignal = signal<readonly CustomerOption[]>([]);
  private readonly workshopsSignal = signal<readonly WorkshopOption[]>([]);
  private readonly activeAlertsCountSignal = signal<number>(0);
  private readonly pendingApprovalsCountSignal = signal<number>(0);

  readonly customerId = this.customerIdSignal.asReadonly();
  readonly profile = this.profileSignal.asReadonly();
  readonly actorId = this.actorIdSignal.asReadonly();
  readonly customers = this.customersSignal.asReadonly();
  readonly workshops = this.workshopsSignal.asReadonly();
  readonly activeAlertsCount = this.activeAlertsCountSignal.asReadonly();
  readonly pendingApprovalsCount = this.pendingApprovalsCountSignal.asReadonly();

  readonly currentCustomer = computed<CustomerOption | null>(() => {
    const id: string = this.customerIdSignal();
    if (!id) {
      return null;
    }
    return this.customersSignal().find((c: CustomerOption) => c.id === id) ?? null;
  });

  readonly availableApprovers = computed<readonly ApproverOption[]>(() => {
    const customer: CustomerOption | null = this.currentCustomer();
    if (customer) {
      return customer.approvers.filter((a: ApproverOption) => a.status === 'Active');
    }
    return this.customersSignal()
      .flatMap((c: CustomerOption) => c.approvers)
      .filter((a: ApproverOption) => a.status === 'Active');
  });

  readonly currentActorName = computed<string>(() => {
    const profile: Profile = this.profileSignal();
    const actorId: string | null = this.actorIdSignal();
    if (profile === 'Approver') {
      const approver: ApproverOption | undefined = this.availableApprovers().find(
        (a: ApproverOption) => a.id === actorId
      );
      return approver?.fullName ?? SESSION_TEXTS.approverActorName;
    }
    if (profile === 'Workshop') {
      const workshop: WorkshopOption | undefined = this.workshopsSignal().find(
        (w: WorkshopOption) => w.id === actorId
      );
      return workshop?.name ?? SESSION_TEXTS.workshopActorName;
    }
    return SESSION_TEXTS.operatorActorName;
  });

  constructor() {
    this.restoreFromStorage();
  }

  async loadContextCatalogs(): Promise<void> {
    try {
      const [customers, workshops] = await Promise.all([
        firstValueFrom(this.apiClient.get<readonly CustomerSummary[]>(ApiRoutes.customers.list())),
        firstValueFrom(
          this.apiClient.get<readonly WorkshopOption[]>(ApiRoutes.workshops.list('Active'))
        ),
      ]);
      this.customersSignal.set(customers.map(withApprovers));
      this.workshopsSignal.set(workshops);
      this.discardUnknownCustomer();
      await this.loadApproversOf(this.approverScope());
      this.ensureValidActor();
      await this.notifications.syncSession(
        this.customerIdSignal(),
        this.profileSignal(),
        this.actorIdSignal()
      );
    } catch {
      return;
    }
  }

  setCustomer(customerId: string): void {
    this.customerIdSignal.set(customerId);
    this.ensureValidActor();
    this.persistToStorage();
    void this.loadApprovers(customerId).then(() => this.refreshActor());
    void this.notifications.syncSession(
      this.customerIdSignal(),
      this.profileSignal(),
      this.actorIdSignal()
    );
  }

  setProfile(profile: Profile): void {
    this.profileSignal.set(profile);
    this.ensureValidActor();
    this.persistToStorage();
    void this.notifications.syncSession(
      this.customerIdSignal(),
      this.profileSignal(),
      this.actorIdSignal()
    );
  }

  setActor(actorId: string | null): void {
    this.actorIdSignal.set(actorId);
    this.persistToStorage();
    void this.notifications.syncSession(
      this.customerIdSignal(),
      this.profileSignal(),
      this.actorIdSignal()
    );
  }

  setActiveAlertsCount(count: number): void {
    this.activeAlertsCountSignal.set(count);
  }

  setPendingApprovalsCount(count: number): void {
    this.pendingApprovalsCountSignal.set(count);
  }

  private refreshActor(): void {
    this.ensureValidActor();
    this.persistToStorage();
    void this.notifications.syncSession(
      this.customerIdSignal(),
      this.profileSignal(),
      this.actorIdSignal()
    );
  }

  private discardUnknownCustomer(): void {
    const id: string = this.customerIdSignal();
    if (!id || this.customersSignal().some((c: CustomerOption) => c.id === id)) {
      return;
    }
    this.customerIdSignal.set('');
    this.persistToStorage();
  }

  private approverScope(): readonly string[] {
    const customerId: string = this.customerIdSignal();
    return customerId ? [customerId] : this.customersSignal().map((c: CustomerOption) => c.id);
  }

  private async loadApproversOf(customerIds: readonly string[]): Promise<void> {
    await Promise.all(customerIds.map((id: string) => this.loadApprovers(id)));
  }

  private async loadApprovers(customerId: string): Promise<void> {
    if (!customerId) {
      return;
    }
    try {
      const detail: CustomerDetail = await firstValueFrom(
        this.apiClient.get<CustomerDetail>(ApiRoutes.customers.byId(customerId))
      );
      const approvers: readonly ApproverOption[] = (detail.approvers ?? []).map(
        (a: Partial<ApproverOption>) => toApprover(a, customerId)
      );
      this.customersSignal.update((list: readonly CustomerOption[]) =>
        list.map((c: CustomerOption) => (c.id === customerId ? { ...c, approvers } : c))
      );
    } catch {
      return;
    }
  }

  private ensureValidActor(): void {
    const profile: Profile = this.profileSignal();
    const currentActor: string | null = this.actorIdSignal();

    if (profile === 'Operator') {
      this.actorIdSignal.set(null);
      return;
    }

    if (profile === 'Approver') {
      const approvers: readonly ApproverOption[] = this.availableApprovers();
      const exists: boolean = approvers.some((a: ApproverOption) => a.id === currentActor);
      if (!exists) {
        this.actorIdSignal.set(approvers[0]?.id ?? null);
      }
      return;
    }

    const workshops: readonly WorkshopOption[] = this.workshopsSignal();
    const exists: boolean = workshops.some((w: WorkshopOption) => w.id === currentActor);
    if (!exists) {
      this.actorIdSignal.set(workshops[0]?.id ?? null);
    }
  }

  private restoreFromStorage(): void {
    if (!this.storage) {
      return;
    }
    try {
      const raw: string | null = this.storage.getItem(SESSION_STORAGE_KEY);
      if (!raw) {
        return;
      }
      const parsed: PersistedSession = JSON.parse(raw) as PersistedSession;
      if (typeof parsed.customerId === 'string') {
        this.customerIdSignal.set(parsed.customerId);
      }
      if (
        parsed.profile === 'Operator' ||
        parsed.profile === 'Approver' ||
        parsed.profile === 'Workshop'
      ) {
        this.profileSignal.set(parsed.profile);
      }
      if (typeof parsed.actorId === 'string' || parsed.actorId === null) {
        this.actorIdSignal.set(parsed.actorId);
      }
    } catch {
      return;
    }
  }

  private persistToStorage(): void {
    if (!this.storage) {
      return;
    }
    try {
      const payload: PersistedSession = {
        customerId: this.customerIdSignal(),
        profile: this.profileSignal(),
        actorId: this.actorIdSignal(),
      };
      this.storage.setItem(SESSION_STORAGE_KEY, JSON.stringify(payload));
    } catch {
      return;
    }
  }
}

