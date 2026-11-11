import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ENUM_LABELS } from '../../core/models/enum-labels';
import {
  DueStatus,
  RoutineState,
  ServiceLineApprovalStatus,
  ServiceOrderStatus,
  TriggeredBy,
  VehicleStatus,
} from '../../core/models/enums';

export type PillStatusKind =
  | DueStatus
  | RoutineState
  | VehicleStatus
  | ServiceOrderStatus
  | ServiceLineApprovalStatus;

export type PillTone = 'is-ok' | 'is-warn' | 'is-danger' | 'is-info' | 'is-brand' | 'is-neutral';

interface StatusPresentation {
  readonly label: string;
  readonly tone: PillTone;
  readonly icon: string;
}

const STATUS_MAP: Readonly<Record<string, StatusPresentation>> = {
  UpToDate: { label: ENUM_LABELS.routineDueStatus.UpToDate, tone: 'is-ok', icon: 'check_circle' },
  DueSoon: { label: ENUM_LABELS.routineDueStatus.DueSoon, tone: 'is-warn', icon: 'schedule' },
  Overdue: { label: ENUM_LABELS.routineDueStatus.Overdue, tone: 'is-danger', icon: 'error' },
  Inactive: { label: ENUM_LABELS.routineState.Inactive, tone: 'is-neutral', icon: 'pause_circle' },
  Active: { label: ENUM_LABELS.vehicleStatus.Active, tone: 'is-ok', icon: 'check_circle' },
  Created: { label: ENUM_LABELS.serviceOrderStatus.Created, tone: 'is-neutral', icon: 'note_add' },
  Quoted: {
    label: ENUM_LABELS.serviceOrderStatus.Quoted,
    tone: 'is-info',
    icon: 'request_quote',
  },
  PendingApproval: {
    label: ENUM_LABELS.serviceOrderStatus.PendingApproval,
    tone: 'is-warn',
    icon: 'gavel',
  },
  Approved: {
    label: ENUM_LABELS.serviceOrderStatus.Approved,
    tone: 'is-info',
    icon: 'task_alt',
  },
  OnSite: { label: ENUM_LABELS.serviceOrderStatus.OnSite, tone: 'is-info', icon: 'garage' },
  InProgress: {
    label: ENUM_LABELS.serviceOrderStatus.InProgress,
    tone: 'is-brand',
    icon: 'build',
  },
  OnHold: {
    label: ENUM_LABELS.serviceOrderStatus.OnHold,
    tone: 'is-neutral',
    icon: 'pause_circle',
  },
  Completed: {
    label: ENUM_LABELS.serviceOrderStatus.Completed,
    tone: 'is-ok',
    icon: 'check_circle',
  },
  Cancelled: {
    label: ENUM_LABELS.serviceOrderStatus.Cancelled,
    tone: 'is-neutral',
    icon: 'block',
  },
  Rejected: {
    label: ENUM_LABELS.serviceLineApprovalStatus.Rejected,
    tone: 'is-neutral',
    icon: 'close',
  },
};

const TRIGGER_ICONS: Readonly<Record<TriggeredBy, readonly string[]>> = {
  Distance: ['speed'],
  Time: ['event'],
  DistanceAndTime: ['speed', 'event'],
};

@Component({
  selector: 'app-status-pill',
  templateUrl: './status-pill.component.html',
  styleUrl: './status-pill.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusPillComponent {
  readonly status = input.required<string>();
  readonly trigger = input<TriggeredBy | null>(null);
  readonly small = input<boolean>(false);
  readonly customLabel = input<string | null>(null);
  readonly customTone = input<PillTone | null>(null);
  readonly customIcon = input<string | null>(null);

  protected readonly presentation = computed<StatusPresentation>(() => {
    const base: StatusPresentation = STATUS_MAP[this.status()] ?? {
      label: this.status(),
      tone: 'is-neutral',
      icon: 'info',
    };
    return {
      label: this.customLabel() ?? base.label,
      tone: this.customTone() ?? base.tone,
      icon: this.customIcon() ?? base.icon,
    };
  });

  protected readonly triggerIcons = computed<readonly string[]>(() => {
    const trig: TriggeredBy | null = this.trigger();
    if (!trig) {
      return [];
    }
    return TRIGGER_ICONS[trig];
  });

  protected readonly triggerAccessibleText = computed<string | null>(() => {
    const trig: TriggeredBy | null = this.trigger();
    if (!trig) {
      return null;
    }
    return ENUM_LABELS.alertTrigger[trig];
  });
}

