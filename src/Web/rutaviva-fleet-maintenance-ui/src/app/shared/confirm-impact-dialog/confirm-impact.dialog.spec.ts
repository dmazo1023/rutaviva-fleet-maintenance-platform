import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmImpactDialogComponent } from './confirm-impact.dialog';

describe('ConfirmImpactDialogComponent', () => {
  it('should close on Escape and return focus to the trigger element', () => {
    TestBed.configureTestingModule({
      imports: [ConfirmImpactDialogComponent],
    });

    const triggerButton: HTMLButtonElement = document.createElement('button');
    document.body.appendChild(triggerButton);
    triggerButton.focus();

    const fixture: ComponentFixture<ConfirmImpactDialogComponent> = TestBed.createComponent(
      ConfirmImpactDialogComponent
    );
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('title', 'Rechazar cotización');
    fixture.componentRef.setInput('confirmLabel', 'Rechazar');
    fixture.componentRef.setInput('items', [
      { icon: 'block', text: 'La orden se cancela' },
      { icon: 'add_task', text: 'Podrás crear una orden manual' },
    ]);

    let closedCount = 0;
    fixture.componentInstance.closed.subscribe(() => {
      closedCount += 1;
    });
    fixture.detectChanges();

    const dialogEl: HTMLDialogElement | null = (
      fixture.nativeElement as HTMLElement
    ).querySelector('dialog');
    expect(dialogEl).not.toBeNull();
    expect(dialogEl?.textContent).toContain('La orden se cancela');

    dialogEl?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(closedCount).toBe(1);
    expect(document.activeElement).toBe(triggerButton);

    document.body.removeChild(triggerButton);
  });
});

