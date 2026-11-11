import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { ToastStore } from '../../core/feedback/toast.store';
import { NotificationsService } from '../../core/realtime/notifications.service';
import {
  ApprovalRequestedEvent,
  ServiceOrderChangedEvent,
} from '../../core/realtime/realtime-events';
import { ServiceOrdersApi } from './data/service-orders.api';
import { ServiceOrderDetailVm } from './models/service-orders.vm';
import { ServiceOrdersFacade } from './service-orders.facade';

describe('ServiceOrdersFacade', () => {
  let orderChangedSubject: Subject<ServiceOrderChangedEvent>;
  let approvalRequestedSubject: Subject<ApprovalRequestedEvent>;
  let getOrderByIdSpy: ReturnType<typeof vi.fn>;
  let approveSpy: ReturnType<typeof vi.fn>;

  const sampleDetail: ServiceOrderDetailVm = {
    id: 'OS-2026-001',
    number: 'OS-2026-001',
    vehicleId: 'RVA102',
    plate: 'RVA102',
    routineId: 'RT-RVA102-OIL',
    routineName: 'Cambio de aceite',
    routineTypeCode: 'OIL_CHANGE',
    routineIcon: 'oil_barrel',
    customerId: 'CUST-1',
    workshopId: 'WS-1',
    workshopName: 'Taller Norte',
    status: 'PendingApproval',
    statusBeforeHold: null,
    reason: 'Quote',
    priority: 'Normal',
    availableActions: [
      { action: 'Approve', enabled: true, blockedReason: null },
      { action: 'Reject', enabled: true, blockedReason: null },
    ],
    lines: [],
    total: 1850000,
    autoApproved: false,
    autoApprovalMaxAmountApplied: 1500000,
    appointmentAt: null,
    history: [],
    updatedAt: '12/11/2026 08:50',
  };

  beforeEach(() => {
    orderChangedSubject = new Subject<ServiceOrderChangedEvent>();
    approvalRequestedSubject = new Subject<ApprovalRequestedEvent>();
    getOrderByIdSpy = vi.fn(() => of(sampleDetail));
    approveSpy = vi.fn(() =>
      throwError(() => new HttpErrorResponse({ status: 409, statusText: 'Conflict' }))
    );

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: ServiceOrdersApi,
          useValue: {
            getOrders: () => of([]),
            getOrderById: getOrderByIdSpy,
            approve: approveSpy,
          },
        },
        {
          provide: NotificationsService,
          useValue: {
            serviceOrderChanged$: orderChangedSubject.asObservable(),
            approvalRequested$: approvalRequestedSubject.asObservable(),
            syncSession: () => Promise.resolve(),
          },
        },
      ],
    });
  });

  it('should expose availableActions, reload order on 409 conflict, and show persistent toast on High priority ApprovalRequested', () => {
    const facade: ServiceOrdersFacade = TestBed.inject(ServiceOrdersFacade);
    const toastStore: ToastStore = TestBed.inject(ToastStore);

    facade.loadOrderDetail('OS-2026-001');
    expect(facade.detailLoadable().data?.availableActions.length).toBe(2);

    facade.approve('OS-2026-001').subscribe({
      error: () => undefined,
    });
    expect(getOrderByIdSpy).toHaveBeenCalledTimes(2);

    approvalRequestedSubject.next({
      serviceOrderId: 'OS-2026-002',
      number: 'OS-2026-002',
      plate: 'RVA104',
      routineName: 'Frenos',
      customerId: 'CUST-1',
      reason: 'AdditionalServices',
      priority: 'High',
      totalAmount: 2100000,
      occurredAt: '2026-11-12T09:10:00Z',
    });

    const toasts = toastStore.toasts();
    expect(toasts.length).toBe(1);
    expect(toasts[0]?.persistent).toBe(true);
    expect(toasts[0]?.message).toContain('OS-2026-002');
  });
});
