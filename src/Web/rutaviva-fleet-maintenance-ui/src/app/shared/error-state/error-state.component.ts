import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export const ERROR_STATE_TEXTS = {
  defaultRetry: 'Reintentar',
} as const;

@Component({
  selector: 'app-error-state',
  templateUrl: './error-state.component.html',
  styleUrl: './error-state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorStateComponent {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly retryLabel = input<string>(ERROR_STATE_TEXTS.defaultRetry);
  readonly retryClicked = output<void>();

  protected onRetry(): void {
    this.retryClicked.emit();
  }
}

