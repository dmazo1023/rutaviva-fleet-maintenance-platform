import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

export type PlateChipSize = 'md' | 'lg';

@Component({
  selector: 'app-plate-chip',
  imports: [RouterLink],
  templateUrl: './plate-chip.component.html',
  styleUrl: './plate-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlateChipComponent {
  readonly plate = input.required<string>();
  readonly size = input<PlateChipSize>('md');
  readonly link = input<string | null>(null);
}

