import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

export interface SegmentedFilterOption {
  readonly value: string;
  readonly label: string;
  readonly count?: number;
  readonly toneClass?: string | null;
}

@Component({
  selector: 'app-segmented-filter',
  templateUrl: './segmented-filter.component.html',
  styleUrl: './segmented-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SegmentedFilterComponent {
  private readonly router: Router = inject(Router);
  private readonly route: ActivatedRoute = inject(ActivatedRoute);

  readonly legend = input.required<string>();
  readonly name = input.required<string>();
  readonly options = input.required<readonly SegmentedFilterOption[]>();
  readonly selectedValue = input.required<string>();
  readonly defaultValue = input<string>('');
  readonly queryParamKey = input<string | null>(null);
  readonly valueChange = output<string>();

  protected onSelect(nextValue: string): void {
    this.valueChange.emit(nextValue);
    const paramKey: string | null = this.queryParamKey();
    if (!paramKey) {
      return;
    }
    const queryParamValue: string | null =
      nextValue === this.defaultValue() || nextValue === '' ? null : nextValue;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { [paramKey]: queryParamValue },
      queryParamsHandling: 'merge',
    });
  }
}

