import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlanStore } from '../../../maintenance/state/plan.store';
import { FleetFacade } from '../../fleet.facade';
import { FleetStore } from '../../state/fleet.store';
import { VehicleDetailPageComponent } from './vehicle-detail.page';

describe('VehicleDetailPageComponent', () => {
  let fleetStore: FleetStore;
  let planStore: PlanStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VehicleDetailPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        {
          provide: FleetFacade,
          useFactory: () => {
            const fs: FleetStore = TestBed.inject(FleetStore);
            const ps: PlanStore = TestBed.inject(PlanStore);
            return {
              listLoadable: fs.listLoadable,
              filteredVehicles: fs.filteredVehicles,
              filterCounts: fs.filterCounts,
              searchQuery: fs.searchQuery,
              activeFilter: fs.activeFilter,
              flashedVehicleIds: fs.flashedVehicleIds,
              loadFleetList: () => undefined,
              setSearchQuery: () => undefined,
              setActiveFilter: () => undefined,
              detailLoadable: fs.detailLoadable,
              planLoadable: ps.planLoadable,
              readingsLoadable: ps.readingsLoadable,
              ordersLoadable: ps.ordersLoadable,
              flashedRoutineIds: ps.flashedRoutineIds,
              loadVehicleDetail: () => undefined,
            };
          },
        },
      ],
    }).compileComponents();

    fleetStore = TestBed.inject(FleetStore);
    planStore = TestBed.inject(PlanStore);
  });

  it('should support ArrowRight and ArrowLeft keyboard navigation on ARIA tabs and show inactive banner when vehicle is Inactive', () => {
    fleetStore.setDetailReady({
      vehicleId: 'RVA110',
      plate: 'RVA110',
      vin: 'VIN110',
      brand: 'Hino',
      model: 'DUTRO',
      modelYear: 2019,
      customerId: 'CUST-1',
      customerName: 'Logística Andina',
      workshopId: 'WS-1',
      workshopName: 'Taller Norte',
      responsibleApproverId: null,
      responsibleApproverName: null,
      status: 'Inactive',
      initialOdometerKm: 58900,
    });
    planStore.setPlanReady({
      vehicleId: 'RVA110',
      plate: 'RVA110',
      status: 'Suspended',
      odometerKm: 58900,
      worstDueStatus: null,
      maxConsumedPercent: 0,
      routines: [],
    });
    planStore.setReadingsReady([]);

    const fixture: ComponentFixture<VehicleDetailPageComponent> =
      TestBed.createComponent(VehicleDetailPageComponent);
    fixture.componentRef.setInput('plate', 'RVA110');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Evaluaciones y órdenes en pausa');

    const tablist: HTMLElement | null = host.querySelector('[role="tablist"]');
    expect(fixture.componentInstance.activeTab()).toBe('rutinas');

    tablist?.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    fixture.detectChanges();
    expect(fixture.componentInstance.activeTab()).toBe('ordenes');

    tablist?.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    fixture.detectChanges();
    expect(fixture.componentInstance.activeTab()).toBe('rutinas');
  });
});

