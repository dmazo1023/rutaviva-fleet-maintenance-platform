import { Directive, effect, ElementRef, inject, input, InputSignal, output, OutputEmitterRef } from '@angular/core';

@Directive({
  selector: 'dialog[appModalDialog]',
  host: { '(cancel)': 'onCancel($event)' },
})
export class ModalDialogDirective {
  readonly open: InputSignal<boolean> = input.required<boolean>({ alias: 'appModalDialog' });
  readonly dismissed: OutputEmitterRef<void> = output<void>();

  private readonly host: ElementRef<HTMLDialogElement> = inject(ElementRef);

  constructor() {
    effect(() => this.sync(this.open()));
  }

  protected onCancel(event: Event): void {
    event.preventDefault();
    this.dismissed.emit();
  }

  private sync(isOpen: boolean): void {
    const dialog: HTMLDialogElement = this.host.nativeElement;
    if (isOpen) {
      this.show(dialog);
    } else {
      this.hide(dialog);
    }
  }

  private show(dialog: HTMLDialogElement): void {
    if (dialog.open) {
      return;
    }
    if (typeof dialog.showModal === 'function') {
      dialog.showModal();
      return;
    }
    dialog.setAttribute('open', '');
  }

  private hide(dialog: HTMLDialogElement): void {
    if (!dialog.open) {
      return;
    }
    if (typeof dialog.close === 'function') {
      dialog.close();
      return;
    }
    dialog.removeAttribute('open');
  }
}
