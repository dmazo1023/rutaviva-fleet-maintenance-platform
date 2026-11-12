import { ChangeDetectionStrategy, Component, input, InputSignal, output, OutputEmitterRef } from '@angular/core';
import { MiniBarComponent } from '../../../../shared/mini-bar/mini-bar.component';
import { PlateChipComponent } from '../../../../shared/plate-chip/plate-chip.component';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { FLEET_TEXTS } from '../../fleet.texts';
import { VehicleListItemVm, VehicleMaintenanceSummaryVm } from '../../models/fleet.vm';

const NUMBER_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');
const NON_BREAKING_SPACE = '\u00A0';
const COUNT_SEPARATOR = ' · ';

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

  readonly texts: typeof FLEET_TEXTS = FLEET_TEXTS;

  isFlashed(vehicleId: string): boolean {
    return this.flashedIds().has(vehicleId);
  }

  formatOdometer(km: number | null): string {
    if (km === null) {
      return this.texts.emptyValue;
    }
    return `${NUMBER_FORMATTER.format(km)}${NON_BREAKING_SPACE}${this.texts.kmUnit}`;
  }

  formatConsumed(percent: number): string {
    return `${NUMBER_FORMATTER.format(Math.round(percent))}${NON_BREAKING_SPACE}%${NON_BREAKING_SPACE}${this.texts.consumedLabel}`;
  }

  formatCounts(summary: VehicleMaintenanceSummaryVm): string {
    return [
      this.formatCount(summary.overdueCount, this.texts.overdueCountLabel),
      this.formatCount(summary.dueSoonCount, this.texts.dueSoonCountLabel),
      this.formatCount(summary.upToDateCount, this.texts.upToDateCountLabel),
    ]
      .filter((part: string) => part !== '')
      .join(COUNT_SEPARATOR);
  }

  onRowClick(plate: string): void {
    this.selectVehicle.emit(plate);
  }

  private formatCount(count: number, label: string): string {
    return count > 0 ? `${count}${NON_BREAKING_SPACE}${label}` : '';
  }
}
