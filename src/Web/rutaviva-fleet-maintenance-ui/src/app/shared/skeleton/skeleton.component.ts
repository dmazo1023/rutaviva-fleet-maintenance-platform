import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const DEFAULT_ROWS_COUNT = 4;

@Component({
  selector: 'app-skeleton',
  templateUrl: './skeleton.component.html',
  styleUrl: './skeleton.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonComponent {
  readonly rows = input<number>(DEFAULT_ROWS_COUNT);
  readonly ariaLabel = input<string>('Cargando');

  protected readonly rowIndices = computed<readonly number[]>(() =>
    Array.from({ length: this.rows() }, (_, index: number) => index)
  );
}

