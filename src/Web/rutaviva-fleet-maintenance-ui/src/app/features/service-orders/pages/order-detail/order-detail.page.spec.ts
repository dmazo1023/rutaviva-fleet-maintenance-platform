import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SessionStore } from '../../../../core/context/session.store';
import { ServiceOrderDetailVm } from '../../models/service-orders.vm';
import { ServiceOrdersFacade } from '../../service-orders.facade';
import { ServiceOrdersStore } from '../../state/service-orders.store';
import { nextStepNoticeOf, OrderDetailPageComponent } from './order-detail.page';

describe('OrderDetailPageComponent', () => {
  let store: ServiceOrdersStore;
  let sessionStore: SessionStore;

  const baseOrder: ServiceOrderDetailVm = {
    id: 'OS-2026-001',
    number: 'OS-2026-001',
    origin: 'Manual',
    vehicleId: 'RVA102',
    plate: 'RVA102',
    routineId: 'RT-RVA102-OIL',
    routineName: 'Cambio de aceite',
    routineTypeCode: 'OIL_CHANGE',
    routineIcon: 'oil_barrel',
    customerId: 'CUST-1',
    workshopId: 'WS-1',
    workshopName: 'Taller Norte',
    status: 'OnHold',
    statusBeforeHold: 'InProgress',
    reason: null,
    priority: null,
    availableActions: [],
    lines: [
      {
        id: 'LINE-1',
        lineType: 'Additional',
        description: 'Filtro especial',
        amount: null,
        pricingStatus: 'PendingPricing',
        requestedBy: 'Approver',
        approvalStatus: 'PendingApproval',
      },
    ],
    total: 0,
    autoApproved: false,
    autoApprovalMaxAmountApplied: 1500000,
    appointmentAt: null,
    history: [
      {
        id: 'H-1',
        changeType: 'StatusChanged',
        detail: 'Orden creada',
        changedBy: 'Sistema',
        changedAt: '12/11/2026 08:00',
        icon: 'note_add',
      },
    ],
    updatedAt: '12/11/2026 08:00',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderDetailPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        {
          provide: ServiceOrdersFacade,
          useFactory: () => {
            const s: ServiceOrdersStore = TestBed.inject(ServiceOrdersStore);
            return {
              detailLoadable: s.detailLoadable,
              loadOrderDetail: () => undefined,
            };
          },
        },
      ],
    }).compileComponents();

    store = TestBed.inject(ServiceOrdersStore);
    sessionStore = TestBed.inject(SessionStore);
  });

  it('should render 5-step stepper with paused banner, inline price input for Workshop, waiting message for Created without lines, and collapsed history details', () => {
    sessionStore.setProfile('Workshop');
    store.setDetailReady(baseOrder);

    const fixture: ComponentFixture<OrderDetailPageComponent> =
      TestBed.createComponent(OrderDetailPageComponent);
    fixture.componentRef.setInput('id', 'OS-2026-001');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelectorAll('.order-stepper__step').length).toBe(5);
    expect(host.textContent).toContain('En pausa · En proceso');
    expect(host.querySelector('.service-lines__price-input')).not.toBeNull();

    const detailsEl: HTMLDetailsElement | null = host.querySelector('details.order-history');
    expect(detailsEl?.open).toBe(false);

    store.setDetailReady({
      ...baseOrder,
      status: 'Created',
      statusBeforeHold: null,
      lines: [],
    });
    fixture.detectChanges();
    expect(host.textContent).toContain('Esperando cotización del taller');
  });

  it('should show the cancelled banner and the danger cancel action for an Operator', () => {
    sessionStore.setProfile('Operator');
    store.setDetailReady({
      ...baseOrder,
      origin: 'Automatic',
      status: 'Cancelled',
      statusBeforeHold: null,
      availableActions: [],
    });

    const fixture: ComponentFixture<OrderDetailPageComponent> =
      TestBed.createComponent(OrderDetailPageComponent);
    fixture.componentRef.setInput('id', 'OS-2026-001');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.stepper-cancelled-banner')?.textContent).toContain('Orden cancelada');
    expect(host.querySelector('.order-detail__origin')?.textContent).toContain('Automática');

    store.setDetailReady({
      ...baseOrder,
      status: 'Approved',
      statusBeforeHold: null,
      availableActions: [{ action: 'Cancel', enabled: true, blockedReason: null }],
    });
    fixture.detectChanges();
    expect(host.querySelector('.order-actions__cancel-btn.btn--danger')).not.toBeNull();
    expect(host.querySelector('.stepper-cancelled-banner')).toBeNull();
  });

  it('should tell who goes next when the profile has no progress actions', () => {
    const order: ServiceOrderDetailVm = { ...baseOrder, statusBeforeHold: null };

    expect(nextStepNoticeOf({ ...order, status: 'Created' })).toBe(
      'Esperando cotización del taller · Taller Norte'
    );
    expect(nextStepNoticeOf({ ...order, status: 'PendingApproval' })).toBe(
      'Esperando aprobación del cliente'
    );
    expect(nextStepNoticeOf({ ...order, status: 'Approved' })).toBe(
      'Esperando que el taller inicie el servicio · Taller Norte'
    );
    expect(nextStepNoticeOf({ ...order, status: 'InProgress' })).toBe(
      'Esperando que el taller complete el servicio · Taller Norte'
    );
    expect(nextStepNoticeOf({ ...order, status: 'Completed' })).toBeNull();
    expect(nextStepNoticeOf({ ...order, status: 'Cancelled' })).toBeNull();
    expect(
      nextStepNoticeOf({
        ...order,
        status: 'Created',
        availableActions: [{ action: 'Quote', enabled: true, blockedReason: null }],
      })
    ).toBeNull();
    expect(
      nextStepNoticeOf({
        ...order,
        status: 'Approved',
        availableActions: [{ action: 'Cancel', enabled: true, blockedReason: null }],
      })
    ).toBe('Esperando que el taller inicie el servicio · Taller Norte');
  });

  it('should render the next-step notice next to the Operator cancel button', () => {
    sessionStore.setProfile('Operator');
    store.setDetailReady({
      ...baseOrder,
      status: 'PendingApproval',
      statusBeforeHold: null,
      availableActions: [{ action: 'Cancel', enabled: true, blockedReason: null }],
    });

    const fixture: ComponentFixture<OrderDetailPageComponent> =
      TestBed.createComponent(OrderDetailPageComponent);
    fixture.componentRef.setInput('id', 'OS-2026-001');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const notice: HTMLElement | null = host.querySelector('.order-detail__next-step');
    expect(notice?.getAttribute('role')).toBe('status');
    expect(notice?.textContent).toContain('Esperando aprobación del cliente');
    expect(host.querySelector('.order-actions__cancel-btn')).not.toBeNull();
  });
});
