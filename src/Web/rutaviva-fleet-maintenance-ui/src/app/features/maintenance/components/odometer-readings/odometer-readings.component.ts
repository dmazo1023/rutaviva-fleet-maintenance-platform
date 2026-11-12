import { ChangeDetectionStrategy, Component, input, InputSignal } from '@angular/core';
import { ENUM_LABELS } from '../../../../core/models/enum-labels';
import { OdometerReadingIgnoredReason } from '../../../../core/models/enums';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { MAINTENANCE_TEXTS } from '../../maintenance.texts';
import { OdometerReadingVm } from '../../models/maintenance.vm';

@Component({
  selector: 'app-odometer-readings',
  imports: [EmptyStateComponent],
  templateUrl: './odometer-readings.component.html',
  styleUrl: './odometer-readings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OdometerReadingsComponent {
  readonly readings: InputSignal<readonly OdometerReadingVm[]> =
    input.required<readonly OdometerReadingVm[]>();

  readonly texts: typeof MAINTENANCE_TEXTS = MAINTENANCE_TEXTS;
  readonly labels = ENUM_LABELS;

  isIgnored(reading: OdometerReadingVm): boolean {
    return reading.outcome === 'Ignored';
  }

  getReasonLabel(reason: OdometerReadingIgnoredReason | null): string {
    if (!reason) {
      return '';
    }
    return this.labels.odometerReadingIgnoredReason[reason];
  }

  getOutcomeLabel(reading: OdometerReadingVm): string {
    return this.labels.odometerReadingOutcome[reading.outcome];
  }

  formatKm(km: number): string {
    return `${km.toLocaleString('es-CO')} ${this.texts.kmUnit}`;
  }
}
