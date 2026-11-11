import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';

export interface ImpactItemVm {
  readonly icon: string;
  readonly text: string;
}

export const CONFIRM_DIALOG_TEXTS = {
  backButton: 'Volver',
  closeAriaLabel: 'Cerrar',
} as const;

@Component({
  selector: 'app-confirm-impact-dialog',
  templateUrl: './confirm-impact.dialog.html',
  styleUrl: './confirm-impact.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmImpactDialogComponent {
  readonly open = input.required<boolean>();
  readonly title = input.required<string>();
  readonly icon = input<string>('warning');
  readonly tone = input<'is-danger' | 'is-neutral'>('is-danger');
  readonly items = input.required<readonly ImpactItemVm[]>();
  readonly confirmLabel = input.required<string>();
  readonly cancelLabel = input<string>(CONFIRM_DIALOG_TEXTS.backButton);
  readonly busy = input<boolean>(false);

  readonly confirmed = output<void>();
  readonly closed = output<void>();

  protected readonly texts = CONFIRM_DIALOG_TEXTS;
  private readonly dialogRef = viewChild<ElementRef<HTMLDialogElement>>('dialogEl');
  private readonly confirmBtnRef = viewChild<ElementRef<HTMLButtonElement>>('confirmBtn');
  private triggerElement: HTMLElement | null = null;

  constructor() {
    effect(() => {
      const isOpen: boolean = this.open();
      const dialog: HTMLDialogElement | undefined = this.dialogRef()?.nativeElement;
      if (!dialog) {
        return;
      }
      if (isOpen) {
        if (typeof document !== 'undefined' && document.activeElement instanceof HTMLElement) {
          this.triggerElement = document.activeElement;
        }
        if (typeof dialog.showModal === 'function' && !dialog.open) {
          dialog.showModal();
        } else {
          dialog.setAttribute('open', '');
        }
        this.confirmBtnRef()?.nativeElement.focus();
      } else {
        if (typeof dialog.close === 'function' && dialog.open) {
          dialog.close();
        } else {
          dialog.removeAttribute('open');
        }
      }
    });
  }

  protected onCancel(): void {
    const dialog: HTMLDialogElement | undefined = this.dialogRef()?.nativeElement;
    if (dialog) {
      if (typeof dialog.close === 'function' && dialog.open) {
        dialog.close();
      } else {
        dialog.removeAttribute('open');
      }
    }
    this.restoreFocus();
    this.closed.emit();
  }

  protected onConfirm(): void {
    if (this.busy()) {
      return;
    }
    this.confirmed.emit();
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.onCancel();
    }
  }

  private restoreFocus(): void {
    if (this.triggerElement && typeof this.triggerElement.focus === 'function') {
      this.triggerElement.focus();
      this.triggerElement = null;
    }
  }
}

