import { ChangeDetectionStrategy, Component, input, InputSignal } from '@angular/core';
import { ServiceOrderHistoryEntryVm } from '../../models/service-orders.vm';
import { SERVICE_ORDERS_TEXTS } from '../../service-orders.texts';

@Component({
  selector: 'app-order-history',
  templateUrl: './order-history.component.html',
  styleUrl: './order-history.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderHistoryComponent {
  readonly entries: InputSignal<readonly ServiceOrderHistoryEntryVm[]> =
    input.required<readonly ServiceOrderHistoryEntryVm[]>();

  readonly texts: typeof SERVICE_ORDERS_TEXTS = SERVICE_ORDERS_TEXTS;
}

