import { ChangeDetectionStrategy, Component, input, InputSignal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DueStatus } from '../../../../../core/models/enums';
import { MiniBarComponent } from '../../../../../shared/mini-bar/mini-bar.component';
import { PlateChipComponent } from '../../../../../shared/plate-chip/plate-chip.component';
import { StatusPillComponent } from '../../../../../shared/status-pill/status-pill.component';
import { FLEET_TEXTS } from '../../../fleet.texts';
import { VEHICLE_DETAIL_TEXTS } from '../vehicle-detail.texts';
import { NextMaintenanceVm } from '../../../../maintenance/models/maintenance.vm';
import { VehicleListItemVm } from '../../../models/fleet.vm';
import { formatRemaining, toneOfDueStatus } from '../../../models/next-maintenance.format';


@Component({
  selector: 'app-vehicle-master-list',
  imports: [RouterLink, PlateChipComponent, StatusPillComponent, MiniBarComponent],
  templateUrl: './vehicle-master-list.component.html',
  styleUrl: './vehicle-master-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleMasterListComponent {
  readonly vehicles: InputSignal<readonly VehicleListItemVm[]> =
    input.required<readonly VehicleListItemVm[]>();
  readonly selectedPlate: InputSignal<string> = input.required<string>();
  readonly flashedIds: InputSignal<ReadonlySet<string>> = input<ReadonlySet<string>>(
    new Set<string>()
  );

  readonly texts: typeof FLEET_TEXTS = FLEET_TEXTS;
  readonly detailTexts: typeof VEHICLE_DETAIL_TEXTS = VEHICLE_DETAIL_TEXTS;

  isFlashed(vehicleId: string): boolean {
    return this.flashedIds().has(vehicleId);
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
}
