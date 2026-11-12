import { ChangeDetectionStrategy, Component, computed, inject, Signal } from '@angular/core';
import { ServiceRegistrationDialogComponent } from '../../dialogs/service-registration/service-registration.dialog';
import { RegisterServiceRequest } from '../../models/maintenance.vm';
import { PlanStore } from '../../state/plan.store';
import { ServiceRegistrationStore } from '../../state/service-registration.store';

@Component({
  selector: 'app-service-registration-host',
  imports: [ServiceRegistrationDialogComponent],
  templateUrl: './service-registration-host.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceRegistrationHostComponent {
  protected readonly store: ServiceRegistrationStore = inject(ServiceRegistrationStore);
  private readonly planStore: PlanStore = inject(PlanStore);

  protected readonly planOdometerKm: Signal<number> = computed(
    () => this.planStore.planLoadable().data?.odometerKm ?? 0
  );

  protected onConfirmed(request: RegisterServiceRequest): void {
    this.store.confirm(request);
  }
}
