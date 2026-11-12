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
    expect(result).toEqual({ action: 'Quote', amount: 1450000 });
  });

  it('should prefill the odometer and emit the serviced km for Complete', () => {
    const fixture: ComponentFixture<OrderFormDialogComponent> =
      TestBed.createComponent(OrderFormDialogComponent);
    fixture.componentRef.setInput('initialAmount', 48250);
    fixture.componentRef.setInput('action', 'Complete');
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();

    let result: OrderFormSubmitEvent | undefined;
    fixture.componentInstance.submitted.subscribe((ev: OrderFormSubmitEvent) => {
      result = ev;
    });

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const kmInput: HTMLInputElement | null = host.querySelector(
      '.order-form-dialog__amount-input'
    );
    expect(kmInput?.value).toBe('48250');
    host.querySelector('form')?.dispatchEvent(new Event('submit'));
    expect(result).toEqual({ action: 'Complete', amount: 48250 });
  });

  it('should render ConfirmImpactDialog for the Reject action', () => {
    const fixture: ComponentFixture<OrderFormDialogComponent> =
      TestBed.createComponent(OrderFormDialogComponent);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('action', 'Reject');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Rechazar cotización');
    expect(host.textContent).toContain('cotización rechazada');
  });

  it('should render ConfirmImpactDialog for the Cancel action and emit on confirm', () => {
    const fixture: ComponentFixture<OrderFormDialogComponent> =
      TestBed.createComponent(OrderFormDialogComponent);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('action', 'Cancel');
    fixture.detectChanges();

    let result: OrderFormSubmitEvent | undefined;
    fixture.componentInstance.submitted.subscribe((ev: OrderFormSubmitEvent) => {
      result = ev;
    });

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Cancelar orden');
    fixture.componentInstance.onImpactConfirmed();
    expect(result).toEqual({ action: 'Cancel' });
  });
});

