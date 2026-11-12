import {
  ApprovalPriority,
  ApprovalReason,
  PricingStatus,
  ServiceLineApprovalStatus,
  ServiceLineRequester,
  ServiceLineType,
  ServiceOrderAction,
  ServiceOrderBlockedReason,
  ServiceOrderChangeType,
  ServiceOrderOrigin,
  ServiceOrderStatus,
} from '../../../core/models/enums';

export interface ServiceOrderActionVm {
  readonly action: ServiceOrderAction;
  readonly enabled: boolean;
  readonly blockedReason: ServiceOrderBlockedReason | null;
}

export interface ServiceLineVm {
  readonly id: string;
  readonly lineType: ServiceLineType;
  readonly description: string;
  readonly amount: number | null;
  readonly pricingStatus: PricingStatus;
  readonly requestedBy: ServiceLineRequester;
  readonly approvalStatus: ServiceLineApprovalStatus;
}

export interface ServiceOrderHistoryEntryVm {
  readonly id: string;
  readonly changeType: ServiceOrderChangeType;
  readonly detail: string;
  readonly changedBy: string;
  readonly changedAt: string;
  readonly icon: string;
}

export interface ServiceOrderListItemVm {
  readonly id: string;
  readonly number: string;
  readonly origin: ServiceOrderOrigin;
  readonly vehicleId: string;
  readonly plate: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode: string;
  readonly customerId: string;
  readonly status: ServiceOrderStatus;
  readonly statusBeforeHold: ServiceOrderStatus | null;
  readonly reason: ApprovalReason | null;
  readonly priority: ApprovalPriority | null;
  readonly total: number | null;
  readonly appointmentAt: string | null;
  readonly updatedAt: string;
}

export interface ServiceOrderDetailVm {
  readonly id: string;
  readonly number: string;
  readonly origin: ServiceOrderOrigin;
  readonly vehicleId: string;
  readonly plate: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode: string;
  readonly routineIcon: string;
  readonly customerId: string;
  readonly workshopId: string;
  readonly workshopName: string;
  readonly status: ServiceOrderStatus;
  readonly statusBeforeHold: ServiceOrderStatus | null;
  readonly reason: ApprovalReason | null;
  readonly priority: ApprovalPriority | null;
  readonly availableActions: readonly ServiceOrderActionVm[];
  readonly lines: readonly ServiceLineVm[];
  readonly total: number;
  readonly autoApproved: boolean;
  readonly autoApprovalMaxAmountApplied: number;
  readonly appointmentAt: string | null;
  readonly history: readonly ServiceOrderHistoryEntryVm[];
  readonly updatedAt: string;
}

