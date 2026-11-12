import { ChangeDetectionStrategy, Component } from '@angular/core';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { REPORTS_TEXTS } from '../../reports.texts';

@Component({
  selector: 'app-reports-page',
  imports: [EmptyStateComponent],
  templateUrl: './reports.page.html',
  styleUrl: './reports.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsPageComponent {
  readonly texts: typeof REPORTS_TEXTS = REPORTS_TEXTS;
}

