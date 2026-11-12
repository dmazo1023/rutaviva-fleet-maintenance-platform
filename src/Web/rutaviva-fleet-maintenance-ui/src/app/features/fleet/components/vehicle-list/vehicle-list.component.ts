import { ChangeDetectionStrategy, Component, input, InputSignal, output, OutputEmitterRef } from '@angular/core';
import { DueStatus, TriggeredBy } from '../../../../core/models/enums';
import { MiniBarComponent } from '../../../../shared/mini-bar/mini-bar.component';
import { PlateChipComponent } from '../../../../shared/plate-chip/plate-chip.component';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { NextMaintenanceVm, PlanOrderSummaryVm } from '../../../maintenance/models/maintenance.vm';
import { FLEET_TEXTS } from '../../fleet.texts';
import { VehicleListItemVm } from '../../models/fleet.vm';
import { formatRemaining, toneOfDueStatus } from '../../models/next-maintenance.format';

const NUMBER_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');
const NON_BREAKING_SPACE = ' ';


@Component({
  selector: 'app-vehicle-list',
  imports: [PlateChipComponent, MiniBarComponent, StatusPillComponent],
  templateUrl: './vehicle-list.component.html',
  styleUrl: './vehicle-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleListComponent {
  readonly vehicles: InputSignal<readonly VehicleListItemVm[]> =
    input.required<readonly VehicleListItemVm[]>();
  readonly flashedIds: InputSignal<ReadonlySet<string>> = input<ReadonlySet<string>>(
    new Set<string>()
  );

  readonly openOrders: InputSignal<ReadonlyMap<string, PlanOrderSummaryVm>> = input<
    ReadonlyMap<string, PlanOrderSummaryVm>
  >(new Map<string, PlanOrderSummaryVm>());

  readonly selectVehicle: OutputEmitterRef<string> = output<string>();

  readonly texts: typeof FLEET_TEXTS = FLEET_TEXTS;

  orderOf(vehicle: VehicleListItemVm): PlanOrderSummaryVm | null {
    return this.openOrders().get(vehicle.vehicleId) ?? null;
  }

  isPendingApproval(order: PlanOrderSummaryVm): boolean {
    return order.status === 'PendingApproval';
  }

  isFlashed(vehicleId: string): boolean {
    return this.flashedIds().has(vehicleId);
  }

  formatOdometer(km: number | null): string {
    if (km === null) {
      return this.texts.emptyValue;
    }
    return `${NUMBER_FORMATTER.format(km)}${NON_BREAKING_SPACE}${this.texts.kmUnit}`;
  }

  nextOf(vehicle: VehicleListItemVm): NextMaintenanceVm | null {
    return vehicle.maintenance?.nextMaintenance ?? null;
  }

  toneOf(status: DueStatus): string {
    return toneOfDueStatus(status);
  }

  formatRemaining(next: NextMaintenanceVm): string {
    return formatRemaining(next);
  }

  pillStatus(vehicle: VehicleListItemVm): string {
    if (vehicle.status === 'Inactive') {
      return 'Inactive';
    }
    return vehicle.worstDueStatus ?? 'UpToDate';
  }

  pillTrigger(vehicle: VehicleListItemVm): TriggeredBy | null {
    if (vehicle.status === 'Inactive' || this.pillStatus(vehicle) === 'UpToDate') {
      return null;
    }
    return this.nextOf(vehicle)?.triggeredBy ?? null;
  }

  onRowClick(plate: string): void {
    this.selectVehicle.emit(plate);
  }
}
