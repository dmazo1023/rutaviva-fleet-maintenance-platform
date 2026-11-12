import { ChangeDetectionStrategy, Component } from '@angular/core';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { SERVICE_ORDERS_TEXTS } from '../../service-orders.texts';

@Component({
  selector: 'app-service-orders-coming-soon-page',
  imports: [EmptyStateComponent],
  templateUrl: './coming-soon.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceOrdersComingSoonPageComponent {
  readonly texts: typeof SERVICE_ORDERS_TEXTS = SERVICE_ORDERS_TEXTS;
}
