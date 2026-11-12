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
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { APPROVAL_POLICY_FEATURE_ENABLED } from '../../administration.feature';
import { ADMINISTRATION_TEXTS } from '../../administration.texts';
import { AdministrationApi, UpdateRoutineTypeRequest } from '../../data/administration.api';
import { ApprovalPolicyVm, RoutineTypeVm } from '../../models/administration.vm';

interface RoutineTypeDraft {
  readonly km: string;
  readonly months: string;
  readonly percent: string;
}

type DraftField = keyof RoutineTypeDraft;

const DEFAULT_DUE_SOON_PERCENT = 10;
const COP_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');
const EMPTY_DRAFT: RoutineTypeDraft = {
  km: '',
  months: '',
  percent: String(DEFAULT_DUE_SOON_PERCENT),
};

function numberToText(value: number | null): string {
  return value === null ? '' : String(value);
}

function textToNumberOrNull(value: string): number | null {
  const trimmed: string = value.trim();
  return trimmed === '' ? null : Number(trimmed);
}

function toDraft(item: RoutineTypeVm): RoutineTypeDraft {
  return {
    km: numberToText(item.defaultIntervalKm),
    months: numberToText(item.defaultIntervalMonths),
    percent: String(item.dueSoonThresholdPercent),
  };
}

function toDrafts(types: readonly RoutineTypeVm[]): Readonly<Record<string, RoutineTypeDraft>> {
  return Object.fromEntries(types.map((item: RoutineTypeVm) => [item.id, toDraft(item)]));
}

@Component({
  selector: 'app-parameters-page',
  imports: [EmptyStateComponent],
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
  readonly policyEnabled: boolean = APPROVAL_POLICY_FEATURE_ENABLED;

  readonly routineTypes: WritableSignal<readonly RoutineTypeVm[]> = signal<
    readonly RoutineTypeVm[]
  >([]);
  readonly drafts: WritableSignal<Readonly<Record<string, RoutineTypeDraft>>> = signal<
    Readonly<Record<string, RoutineTypeDraft>>
  >({});
  readonly policy: WritableSignal<ApprovalPolicyVm | null> = signal<ApprovalPolicyVm | null>(null);
  readonly policyInput: WritableSignal<string> = signal<string>('');
  readonly loadFailed: WritableSignal<boolean> = signal<boolean>(false);

  readonly isOperator: Signal<boolean> = computed(
    () => this.sessionStore.profile() === 'Operator'
  );

  ngOnInit(): void {
    this.viewRefresh.setActiveVehicleId(null);
    this.loadData();
  }

  loadData(): void {
    this.loadRoutineTypes();
    this.loadPolicy();
  }

  formatCop(amount: number): string {
    return `$ ${COP_FORMATTER.format(amount)}`;
  }

  getDraft(id: string): RoutineTypeDraft {
    return this.drafts()[id] ?? EMPTY_DRAFT;
  }

  onDraftFieldChange(id: string, field: DraftField, event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    const current: RoutineTypeDraft = this.getDraft(id);
    this.drafts.update((prev) => ({
      ...prev,
      [id]: { ...current, [field]: target.value },
    }));
  }

  onSaveRoutineType(item: RoutineTypeVm): void {
    if (!this.isOperator()) {
      return;
    }
    this.api.updateRoutineType(item.id, this.buildUpdateRequest(item.id)).subscribe({
      next: () => this.onRoutineTypeSaved(),
      error: () => this.toastStore.show({ message: this.texts.saveErrorToast }),
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

  private loadRoutineTypes(): void {
    this.api.getRoutineTypes().subscribe({
      next: (types: readonly RoutineTypeVm[]) => {
        this.loadFailed.set(false);
        this.routineTypes.set(types);
        this.drafts.set(toDrafts(types));
      },
      error: () => this.loadFailed.set(true),
    });
  }

  private loadPolicy(): void {
    if (!this.policyEnabled) {
      return;
    }
    this.api.getApprovalPolicy().subscribe((pol: ApprovalPolicyVm) => {
      this.policy.set(pol);
      this.policyInput.set(String(pol.currentAmount));
    });
  }

  private buildUpdateRequest(id: string): UpdateRoutineTypeRequest {
    const draft: RoutineTypeDraft = this.getDraft(id);
    return {
      intervalKm: textToNumberOrNull(draft.km),
      intervalMonths: textToNumberOrNull(draft.months),
      dueSoonThresholdPercent: textToNumberOrNull(draft.percent) ?? DEFAULT_DUE_SOON_PERCENT,
      updatedBy: this.sessionStore.currentActorName(),
    };
  }

  private onRoutineTypeSaved(): void {
    this.toastStore.show({ message: this.texts.savedToast });
    this.loadRoutineTypes();
  }
}
