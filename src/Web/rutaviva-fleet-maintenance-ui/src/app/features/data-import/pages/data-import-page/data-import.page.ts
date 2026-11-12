import { ChangeDetectionStrategy, Component } from '@angular/core';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { DATA_IMPORT_TEXTS } from '../../data-import.texts';

@Component({
  selector: 'app-data-import-page',
  imports: [EmptyStateComponent],
  templateUrl: './data-import.page.html',
  styleUrl: './data-import.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DataImportPageComponent {
  readonly texts: typeof DATA_IMPORT_TEXTS = DATA_IMPORT_TEXTS;
}

