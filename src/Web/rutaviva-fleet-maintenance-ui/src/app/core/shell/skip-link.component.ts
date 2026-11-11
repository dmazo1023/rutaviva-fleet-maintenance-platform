import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SHELL_TEXTS } from './shell.texts';

@Component({
  selector: 'app-skip-link',
  templateUrl: './skip-link.component.html',
  styleUrl: './skip-link.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkipLinkComponent {
  protected readonly texts = SHELL_TEXTS;
}

