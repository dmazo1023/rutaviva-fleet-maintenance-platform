import { RoutineTypeStatus } from '../../../core/models/enums';

export interface RoutineTypeVm {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly defaultIntervalKm: number | null;
  readonly defaultIntervalMonths: number | null;
  readonly dueSoonThresholdPercent: number;
  readonly status: RoutineTypeStatus;
}

export interface ApprovalPolicyEntryVm {
  readonly id: string;
  readonly currency: string;
  readonly autoApprovalMaxAmount: number;
  readonly effectiveFrom: string;
  readonly createdBy: string;
}

export interface ApprovalPolicyVm {
  readonly currentAmount: number;
  readonly currency: string;
  readonly history: readonly ApprovalPolicyEntryVm[];
}

