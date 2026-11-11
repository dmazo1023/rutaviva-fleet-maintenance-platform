import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
  Signal,
} from '@angular/core';
import { ENUM_LABELS } from '../../../../core/models/enum-labels';
import { ServiceOrderAction, ServiceOrderBlockedReason } from '../../../../core/models/enums';
import { ServiceOrderActionVm } from '../../models/service-orders.vm';

const PRIMARY_ACTIONS: ReadonlySet<ServiceOrderAction> = new Set<ServiceOrderAction>([
  'Approve',
  'Quote',
  'MarkOnSite',
  'Start',
  'Complete',
]);

@Component({
  selector: 'app-order-actions',
  templateUrl: './order-actions.component.html',
  styleUrl: './order-actions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderActionsComponent {
  readonly availableActions: InputSignal<readonly ServiceOrderActionVm[]> =
    input.required<readonly ServiceOrderActionVm[]>();

  readonly actionTriggered: OutputEmitterRef<ServiceOrderAction> = output<ServiceOrderAction>();

  readonly labels = ENUM_LABELS;

  readonly cancelAction: Signal<ServiceOrderActionVm | null> = computed(
    () =>
      this.availableActions().find((a: ServiceOrderActionVm) => a.action === 'Cancel') ?? null
  );

  readonly rightActions: Signal<readonly ServiceOrderActionVm[]> = computed(() =>
    this.availableActions().filter(
      (a: ServiceOrderActionVm) =>
        a.action !== 'Cancel' && a.action !== 'PriceAdditionalService'
    )
  );

  getActionLabel(action: ServiceOrderAction): string {
    return this.labels.serviceOrderAction[action];
  }

  getBlockedLabel(reason: ServiceOrderBlockedReason | null): string {
    if (!reason) {
      return '';
    }
    return this.labels.serviceOrderBlockedReason[reason];
  }

  getButtonClass(action: ServiceOrderAction): string {
    if (action === 'Reject' || action === 'Cancel') {
      return 'btn btn--danger';
    }
    if (PRIMARY_ACTIONS.has(action)) {
      return 'btn btn--primary';
    }
    return 'btn btn--secondary';
  }

  onTrigger(item: ServiceOrderActionVm): void {
    if (!item.enabled) {
      return;
    }
    this.actionTriggered.emit(item.action);
  }
}

