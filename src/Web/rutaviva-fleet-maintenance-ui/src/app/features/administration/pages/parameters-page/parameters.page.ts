import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  Signal,
  signal,
  WritableSignal,
} from '@angular/core';
import { SessionStore } from '../../../../core/context/session.store';
import { ToastStore } from '../../../../core/feedback/toast.store';
import { ViewRefreshService } from '../../../../core/shell/view-refresh.service';
import { ADMINISTRATION_TEXTS } from '../../administration.texts';
import { AdministrationApi } from '../../data/administration.api';
import { ApprovalPolicyVm, RoutineTypeVm } from '../../models/administration.vm';

interface RoutineTypeDraft {
  readonly km: string;
  readonly months: string;
  readonly percent: string;
}

const COP_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');

@Component({
  selector: 'app-parameters-page',
  templateUrl: './parameters.page.html',
  styleUrl: './parameters.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ParametersPageComponent implements OnInit {
  private readonly api: AdministrationApi = inject(AdministrationApi);
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly toastStore: ToastStore = inject(ToastStore);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);

  readonly texts: typeof ADMINISTRATION_TEXTS = ADMINISTRATION_TEXTS;

  readonly routineTypes: WritableSignal<readonly RoutineTypeVm[]> = signal<
    readonly RoutineTypeVm[]
  >([]);
  readonly drafts: WritableSignal<Readonly<Record<string, RoutineTypeDraft>>> = signal<
    Readonly<Record<string, RoutineTypeDraft>>
  >({});
  readonly policy: WritableSignal<ApprovalPolicyVm | null> = signal<ApprovalPolicyVm | null>(null);
  readonly policyInput: WritableSignal<string> = signal<string>('');

  readonly isOperator: Signal<boolean> = computed(
    () => this.sessionStore.profile() === 'Operator'
  );

  ngOnInit(): void {
    this.viewRefresh.setActiveVehicleId(null);
    this.loadData();
  }

  loadData(): void {
    this.api.getRoutineTypes().subscribe((types: readonly RoutineTypeVm[]) => {
      this.routineTypes.set(types);
      const nextDrafts: Record<string, RoutineTypeDraft> = {};
      for (const item of types) {
        nextDrafts[item.id] = {
          km: item.defaultIntervalKm !== null ? String(item.defaultIntervalKm) : '',
          months: item.defaultIntervalMonths !== null ? String(item.defaultIntervalMonths) : '',
          percent: String(item.dueSoonThresholdPercent),
        };
      }
      this.drafts.set(nextDrafts);
    });

    this.api.getApprovalPolicy().subscribe((pol: ApprovalPolicyVm) => {
      this.policy.set(pol);
      this.policyInput.set(String(pol.currentAmount));
    });
  }

  formatCop(amount: number): string {
    return `$ ${COP_FORMATTER.format(amount)}`;
  }

  getDraft(id: string): RoutineTypeDraft {
    return this.drafts()[id] ?? { km: '', months: '', percent: '10' };
  }

  onDraftFieldChange(id: string, field: 'km' | 'months' | 'percent', event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    const current: RoutineTypeDraft = this.getDraft(id);
    this.drafts.update((prev) => ({
      ...prev,
      [id]: {
        ...current,
        [field]: target.value,
      },
    }));
  }

  onSaveRoutineType(item: RoutineTypeVm): void {
    if (!this.isOperator()) {
      return;
    }
    const draft: RoutineTypeDraft = this.getDraft(item.id);
    const defaultIntervalKm: number | null =
      draft.km.trim() !== '' ? Number(draft.km.trim()) : null;
    const defaultIntervalMonths: number | null =
      draft.months.trim() !== '' ? Number(draft.months.trim()) : null;
    const dueSoonThresholdPercent: number = Number(draft.percent.trim() || 10);

    this.api
      .updateRoutineType(item.id, {
        defaultIntervalKm,
        defaultIntervalMonths,
        dueSoonThresholdPercent,
      })
      .subscribe((updated: RoutineTypeVm) => {
        this.routineTypes.update((list: readonly RoutineTypeVm[]) =>
          list.map((r: RoutineTypeVm) => (r.id === updated.id ? updated : r))
        );
        this.toastStore.show({ message: this.texts.savedToast });
      });
  }

  onPolicyInputChange(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.policyInput.set(target.value);
  }

  onSavePolicy(): void {
    if (!this.isOperator()) {
      return;
    }
    const nextAmount: number = Number(this.policyInput().trim());
    if (!Number.isFinite(nextAmount) || nextAmount < 0) {
      return;
    }
    this.api
      .updateApprovalPolicy(nextAmount, this.sessionStore.currentActorName())
      .subscribe((updated: ApprovalPolicyVm) => {
        this.policy.set(updated);
        this.policyInput.set(String(updated.currentAmount));
        this.toastStore.show({ message: this.texts.policySavedToast });
      });
  }
}

