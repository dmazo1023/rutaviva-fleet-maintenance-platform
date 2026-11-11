import { ChangeDetectionStrategy, Component } from '@angular/core';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';

export const DATA_IMPORT_TEXTS = {
  title: 'Carga masiva',
  emptyTitle: 'Carga masiva de datos',
  emptyDesc: 'Importación por etapas (Talleres, Clientes, Aprobadores, Vehículos y Rutinas).',
} as const;

@Component({
  selector: 'app-data-import-page',
  imports: [EmptyStateComponent],
  templateUrl: './data-import.page.html',
  styleUrl: './data-import.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DataImportPageComponent {
  readonly texts = DATA_IMPORT_TEXTS;
}

