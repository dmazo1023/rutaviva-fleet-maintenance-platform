import { ChangeDetectionStrategy, Component } from '@angular/core';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';

export const REPORTS_TEXTS = {
  title: 'Reportes',
  emptyTitle: 'Reportes operativos',
  emptyDesc: 'Consulta y exportación de estado de flota, órdenes, costos y alertas.',
} as const;

@Component({
  selector: 'app-reports-page',
  imports: [EmptyStateComponent],
  templateUrl: './reports.page.html',
  styleUrl: './reports.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsPageComponent {
  readonly texts = REPORTS_TEXTS;
}

