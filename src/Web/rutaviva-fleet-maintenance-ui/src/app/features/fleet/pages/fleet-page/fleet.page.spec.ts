import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FleetFacade } from '../../fleet.facade';
import { FleetStore } from '../../state/fleet.store';
import { FleetPageComponent } from './fleet.page';

describe('FleetPageComponent', () => {
  let store: FleetStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FleetPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        {
          provide: FleetFacade,
          useFactory: () => {
            const s: FleetStore = TestBed.inject(FleetStore);
            return {
              listLoadable: s.listLoadable,
              sortedVehicles: s.sortedVehicles,
              filteredVehicles: s.filteredVehicles,
              filterCounts: s.filterCounts,
              searchQuery: s.searchQuery,
              activeFilter: s.activeFilter,
              flashedVehicleIds: s.flashedVehicleIds,
              loadFleetList: () => undefined,
              setSearchQuery: (q: string) => s.setSearchQuery(q),
              setActiveFilter: (f: 'All' | 'Overdue' | 'DueSoon' | 'UpToDate' | 'Inactive') =>
                s.setActiveFilter(f),
              clearFilters: () => s.clearFilters(),
            };
          },
        },
      ],
    }).compileComponents();

    store = TestBed.inject(FleetStore);
  });

  it('should render loading skeleton, empty state, error state, and no-matches state', () => {
    const fixture: ComponentFixture<FleetPageComponent> =
      TestBed.createComponent(FleetPageComponent);

    store.setListLoading();
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('app-skeleton')).not.toBeNull();

    store.setListReady([]);
    fixture.detectChanges();
    expect(host.textContent).toContain('Aún no hay vehículos');
    expect(host.textContent).toContain('Cargar flota');

    store.setListError('Fallo');
    fixture.detectChanges();
    expect(host.textContent).toContain('No pudimos cargar la flota');
    expect(host.textContent).toContain('Reintentar');

    store.setListReady([
      {
        vehicleId: 'RVA101',
        plate: 'RVA101',
        brand: 'Chevrolet',
        model: 'NHR',
        modelYear: 2022,
        customerId: 'CUST-1',
        customerName: 'Logística Andina',
        status: 'Active',
        worstDueStatus: 'DueSoon',
        maxConsumedPercent: 92,
        odometerKm: 49200,
        maintenance: {
          worstDueStatus: 'DueSoon',
          maxConsumedPercent: 92,
          upToDateCount: 1,
          dueSoonCount: 1,
          overdueCount: 0, nextMaintenance: null,
        },
      },
    ]);
    store.setSearchQuery('ZZZ999');
    fixture.detectChanges();
    expect(host.textContent).toContain('Sin coincidencias');
    expect(host.textContent).toContain('Limpiar filtros');
  });
});

