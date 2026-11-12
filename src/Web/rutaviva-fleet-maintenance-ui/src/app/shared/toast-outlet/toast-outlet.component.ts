import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ToastItem, ToastStore } from '../../core/feedback/toast.store';

export const TOAST_OUTLET_TEXTS = {
  dismissAriaLabel: 'Cerrar aviso',
} as const;

@Component({
  selector: 'app-toast-outlet',
  imports: [RouterLink],
  templateUrl: './toast-outlet.component.html',
  styleUrl: './toast-outlet.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastOutletComponent {
  protected readonly toastStore: ToastStore = inject(ToastStore);
  protected readonly texts = TOAST_OUTLET_TEXTS;

  protected toneClass(toast: ToastItem): string {
    if (toast.tone === 'danger') {
      return 'toast is-danger';
    }
    if (toast.tone === 'warn') {
      return 'toast is-warn';
    }
    return 'toast';
  }

  protected onActionClick(toast: ToastItem): void {
    this.toastStore.triggerAction(toast.id);
  }

  protected onDismiss(id: string): void {
    this.toastStore.dismiss(id);
  }
}

