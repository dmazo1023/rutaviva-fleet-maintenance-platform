import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-switch',
  templateUrl: './switch.component.html',
  styleUrl: './switch.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SwitchComponent {
  readonly checked = input.required<boolean>();
  readonly disabled = input<boolean>(false);
  readonly ariaLabel = input<string | null>(null);
  readonly label = input<string | null>(null);
  readonly toggled = output<boolean>();

  protected onToggle(): void {
    if (this.disabled()) {
      return;
    }
    this.toggled.emit(!this.checked());
  }
}

