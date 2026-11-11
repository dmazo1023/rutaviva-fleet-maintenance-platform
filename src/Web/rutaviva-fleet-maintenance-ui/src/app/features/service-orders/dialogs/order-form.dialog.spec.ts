import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { OrderFormDialogComponent, OrderFormSubmitEvent } from './order-form.dialog';

describe('OrderFormDialogComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderFormDialogComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('should render Quote form and emit amount on valid submission', () => {
    const fixture: ComponentFixture<OrderFormDialogComponent> =
      TestBed.createComponent(OrderFormDialogComponent);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('action', 'Quote');
    fixture.detectChanges();

    let result: OrderFormSubmitEvent | undefined;
    fixture.componentInstance.submitted.subscribe((ev: OrderFormSubmitEvent) => {
      result = ev;
    });

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const amountInput: HTMLInputElement | null = host.querySelector(
      '.order-form-dialog__amount-input'
    );
    if (amountInput) {
      amountInput.value = '1450000';
      amountInput.dispatchEvent(new Event('input'));
    }
    fixture.detectChanges();

    const form: HTMLFormElement | null = host.querySelector('form');
    form?.dispatchEvent(new Event('submit'));
    expect(result).toEqual({
      action: 'Quote',
      description: undefined,
      amount: 1450000,
      appointmentAt: undefined,
    });
  });

  it('should render ConfirmImpactDialog for Reject and Cancel actions', () => {
    const fixture: ComponentFixture<OrderFormDialogComponent> =
      TestBed.createComponent(OrderFormDialogComponent);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('action', 'Reject');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Rechazar cotización');
    expect(host.textContent).toContain('cotización rechazada');
  });
});

