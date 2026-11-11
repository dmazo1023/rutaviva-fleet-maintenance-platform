import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DueStatus } from '../../core/models/enums';

const PERCENT_MIN = 0;
const PERCENT_MAX = 100;

@Component({
  selector: 'app-mini-bar',
  templateUrl: './mini-bar.component.html',
  styleUrl: './mini-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MiniBarComponent {
  readonly percent = input.required<number>();
  readonly status = input.required<DueStatus>();

  protected readonly toneClass = computed<string>(() => {
    const st: DueStatus = this.status();
    if (st === 'Overdue') {
      return 'mini is-danger';
    }
    if (st === 'DueSoon') {
      return 'mini is-warn';
    }
    return 'mini is-ok';
  });

  protected readonly scaleXTransform = computed<string>(() => {
    const clamped: number = Math.min(PERCENT_MAX, Math.max(PERCENT_MIN, this.percent()));
    return `scaleX(${clamped / PERCENT_MAX})`;
  });
}

