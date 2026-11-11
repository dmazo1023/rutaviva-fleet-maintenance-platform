import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ServiceOrderListItemVm } from '../../models/service-orders.vm';
import { ServiceOrdersFacade } from '../../service-orders.facade';
import { OrdersFilterKey, ServiceOrdersStore } from '../../state/service-orders.store';
import { OrdersInboxPageComponent } from './orders-inbox.page';

describe('OrdersInboxPageComponent', () => {
  let store: ServiceOrdersStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrdersInboxPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        {
          provide: ServiceOrdersFacade,
          useFactory: () => {
            const s: ServiceOrdersStore = TestBed.inject(ServiceOrdersStore);
            return {
              listLoadable: s.listLoadable,
              sortedOrders: s.sortedOrders,
              filteredOrders: s.filteredOrders,
              filterCounts: s.filterCounts,
              activeFilter: s.activeFilter,
              flashedOrderIds: s.flashedOrderIds,
              loadOrders: () => undefined,
              setActiveFilter: (f: OrdersFilterKey) => s.setActiveFilter(f),
            };
          },
        },
      ],
    }).compileComponents();

    store = TestBed.inject(ServiceOrdersStore);
  });

  it('should sort orders by High priority PendingApproval -> PendingApproval -> Open -> Closed and render empty states', () => {
    const fixture: ComponentFixture<OrdersInboxPageComponent> =
      TestBed.createComponent(OrdersInboxPageComponent);

    store.setListReady([]);
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Sin órdenes por aprobar');

    const sampleOrders: readonly ServiceOrderListItemVm[] = [
      {
        id: 'OS-3',
        number: 'OS-2026-003',
        vehicleId: 'RVA103',
        plate: 'RVA103',
        routineId: 'RT-3',
        routineName: 'Llantas',
        routineTypeCode: 'TIRES',
        customerId: 'CUST-1',
        status: 'Completed',
        statusBeforeHold: null,
        reason: null,
        priority: null,
        total: 900000,
        appointmentAt: '10/11/2026 10:00',
        updatedAt: '10/11/2026 15:00',
      },
      {
        id: 'OS-2',
        number: 'OS-2026-002',
        vehicleId: 'RVA102',
        plate: 'RVA102',
        routineId: 'RT-2',
        routineName: 'Frenos',
        routineTypeCode: 'BRAKES',
        customerId: 'CUST-1',
        status: 'Approved',
        statusBeforeHold: null,
        reason: null,
        priority: null,
        total: 1200000,
        appointmentAt: '13/11/2026 09:00',
        updatedAt: '12/11/2026 08:00',
      },
      {
        id: 'OS-1',
        number: 'OS-2026-001',
        vehicleId: 'RVA101',
        plate: 'RVA101',
        routineId: 'RT-1',
        routineName: 'Aceite',
        routineTypeCode: 'OIL_CHANGE',
        customerId: 'CUST-1',
        status: 'PendingApproval',
        statusBeforeHold: null,
        reason: 'Quote',
        priority: 'Normal',
        total: 1800000,
        appointmentAt: null,
        updatedAt: '12/11/2026 08:30',
      },
      {
        id: 'OS-4',
        number: 'OS-2026-004',
        vehicleId: 'RVA104',
        plate: 'RVA104',
        routineId: 'RT-4',
        routineName: 'Frenos',
        routineTypeCode: 'BRAKES',
        customerId: 'CUST-1',
        status: 'PendingApproval',
        statusBeforeHold: null,
        reason: 'AdditionalServices',
        priority: 'High',
        total: 2400000,
        appointmentAt: '12/11/2026 07:30',
        updatedAt: '12/11/2026 09:00',
      },
    ];

    store.setListReady(sampleOrders);
    store.setActiveFilter('All');
    fixture.detectChanges();

    const orderedIds: readonly string[] = store.sortedOrders().map((o) => o.id);
    expect(orderedIds).toEqual(['OS-4', 'OS-1', 'OS-2', 'OS-3']);
  });
});

