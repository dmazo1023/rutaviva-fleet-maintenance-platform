import { ChangeDetectionStrategy, Component, input, InputSignal, output, OutputEmitterRef } from '@angular/core';
import { MiniBarComponent } from '../../../../shared/mini-bar/mini-bar.component';
import { PlateChipComponent } from '../../../../shared/plate-chip/plate-chip.component';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { FLEET_TEXTS } from '../../fleet.texts';
import { VehicleListItemVm, VehicleNextMaintenanceVm } from '../../models/fleet.vm';

const NUMBER_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');

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

  readonly selectVehicle: OutputEmitterRef<string> = output<string>();
  readonly selectOrder: OutputEmitterRef<string> = output<string>();

  readonly texts: typeof FLEET_TEXTS = FLEET_TEXTS;

  isFlashed(vehicleId: string): boolean {
    return this.flashedIds().has(vehicleId);
  }

  formatKm(km: number): string {
    return `${NUMBER_FORMATTER.format(km)}\u00A0${this.texts.kmUnit}`;
  }

  formatRemaining(next: VehicleNextMaintenanceVm): string {
    const absVal: string = NUMBER_FORMATTER.format(Math.abs(next.remainingValue));
    const sign: string = next.remainingValue < 0 ? '−' : '';
    const unit: string = next.remainingKind === 'km' ? this.texts.kmUnit : this.texts.daysUnit;
    return `${sign}${absVal}\u00A0${unit}`;
  }

  onRowClick(plate: string): void {
    this.selectVehicle.emit(plate);
  }

  onOrderClick(event: Event, orderId: string): void {
    event.stopPropagation();
    this.selectOrder.emit(orderId);
  }
}

