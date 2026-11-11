import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ServiceOrderActionVm } from '../../models/service-orders.vm';
import { OrderActionsComponent } from './order-actions.component';

describe('OrderActionsComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderActionsComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('should render Cancel on the left, primary actions on the right, and disabled button with aria-describedby reason', () => {
    const actions: readonly ServiceOrderActionVm[] = [
      { action: 'Cancel', enabled: true, blockedReason: null },
      { action: 'Approve', enabled: false, blockedReason: 'WaitingForPrice' },
      { action: 'MarkOnSite', enabled: false, blockedReason: 'AppointmentRequired' },
    ];

    const fixture: ComponentFixture<OrderActionsComponent> =
      TestBed.createComponent(OrderActionsComponent);
    fixture.componentRef.setInput('availableActions', actions);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const cancelBtn: HTMLButtonElement | null = host.querySelector(
      '.order-actions__left .order-actions__cancel-btn'
    );
    expect(cancelBtn).not.toBeNull();
    expect(cancelBtn?.textContent).toContain('Cancelar orden');

    const rightBtns: NodeListOf<HTMLButtonElement> = host.querySelectorAll(
      '.order-actions__right button'
    );
    expect(rightBtns.length).toBe(2);
    expect(rightBtns[0]?.disabled).toBe(true);
    expect(rightBtns[0]?.getAttribute('aria-describedby')).toBe('blocked-Approve');
    expect(host.querySelector('#blocked-Approve')?.textContent).toContain(
      'Esperando precio del taller'
    );
    expect(host.querySelector('#blocked-MarkOnSite')?.textContent).toContain('Sin cita');
  });
});

