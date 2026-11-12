import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { NotificationsService } from '../../core/realtime/notifications.service';
import {
  OdometerReadingRecordedEvent,
  RoutineChangedEvent,
  ServiceOrderChangedEvent,
  VehicleChangedEvent,
} from '../../core/realtime/realtime-events';
import { MaintenanceApi } from '../maintenance/data/maintenance.api';
import { MaintenancePlanSummaryVm } from '../maintenance/models/maintenance.vm';
import { FleetApi } from './data/fleet.api';
import { composeVehicleListItems, FleetFacade } from './fleet.facade';
import { VehicleListItemVm, VehicleSummaryVm } from './models/fleet.vm';
import { sortVehiclesByUrgency } from './state/fleet.store';

function buildVehicle(plate: string, status: 'Active' | 'Inactive' = 'Active'): VehicleSummaryVm {
  return {
    vehicleId: plate,
    plate,
    vin: null,
    brand: 'Chevrolet',
    model: 'NHR',
    modelYear: 2022,
    status,
    customerId: 'CUST-1',
    customerName: 'Logística Andina',
    workshopId: 'WS-1',
    workshopName: 'Taller Norte',
    responsibleApproverId: null,
    responsibleApproverName: null,
  };
}

function buildPlan(
  plate: string,
  worstDueStatus: MaintenancePlanSummaryVm['worstDueStatus'],
  maxConsumedPercent: number,
  odometerKm: number
): MaintenancePlanSummaryVm {
  return {
    vehicleId: plate,
    plate,
    status: 'Active',
    odometerKm,
    worstDueStatus,
    maxConsumedPercent,
    upToDateCount: 1,
    dueSoonCount: worstDueStatus === 'DueSoon' ? 1 : 0,
    overdueCount: worstDueStatus === 'Overdue' ? 1 : 0,
    inactiveCount: 0,
    nextMaintenance: null,
  };
}

describe('FleetFacade', () => {
  let routineChangedSubject: Subject<RoutineChangedEvent>;
  let odometerRecordedSubject: Subject<OdometerReadingRecordedEvent>;
  let vehicleChangedSubject: Subject<VehicleChangedEvent>;
  let orderChangedSubject: Subject<ServiceOrderChangedEvent>;

  const sampleVehicles: readonly VehicleSummaryVm[] = [
    buildVehicle('RVA103'),
    buildVehicle('RVA110', 'Inactive'),
    buildVehicle('RVA104'),
    buildVehicle('RVA101'),
    buildVehicle('RVA102'),
  ];

  const samplePlans: readonly MaintenancePlanSummaryVm[] = [
    buildPlan('RVA103', 'UpToDate', 21, 22_100),
    buildPlan('RVA110', null, 0, 58_900),
    buildPlan('RVA104', 'Overdue', 120, 60_800),
    buildPlan('RVA101', 'DueSoon', 92, 49_200),
    buildPlan('RVA102', 'Overdue', 114, 81_400),
  ];

  beforeEach(() => {
    routineChangedSubject = new Subject<RoutineChangedEvent>();
    odometerRecordedSubject = new Subject<OdometerReadingRecordedEvent>();
    vehicleChangedSubject = new Subject<VehicleChangedEvent>();
    orderChangedSubject = new Subject<ServiceOrderChangedEvent>();

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: FleetApi,
          useValue: {
            getVehicles: () => of(sampleVehicles),
          },
        },
        {
          provide: MaintenanceApi,
          useValue: {
            getPlanSummaries: () => of(samplePlans),
          },
        },
        {
          provide: NotificationsService,
          useValue: {
            routineChanged$: routineChangedSubject.asObservable(),
            odometerReadingRecorded$: odometerRecordedSubject.asObservable(),
            vehicleChanged$: vehicleChangedSubject.asObservable(),
            serviceOrderChanged$: orderChangedSubject.asObservable(),
          },
        },
      ],
    });
  });

  it('should sort vehicles by urgency (Overdue -> DueSoon -> UpToDate -> Inactive) and then by maxConsumedPercent descending', () => {
    const facade: FleetFacade = TestBed.inject(FleetFacade);
    facade.loadFleetList();

    const sortedPlates: readonly string[] = facade
      .sortedVehicles()
      .map((v: VehicleListItemVm) => v.plate);
    expect(sortedPlates).toEqual(['RVA104', 'RVA102', 'RVA101', 'RVA103', 'RVA110']);
  });

  it('should load the list without calling any service-orders endpoint', () => {
    const facade: FleetFacade = TestBed.inject(FleetFacade);
    facade.loadFleetList();

    expect(facade.listLoadable().status).toBe('ready');
    expect(facade.listLoadable().data.length).toBe(5);
  });

  it('should compose the maintenance summary with worst status, max percent and counts', () => {
    const items: readonly VehicleListItemVm[] = composeVehicleListItems(
      sampleVehicles,
      samplePlans
    );
    const overdue: VehicleListItemVm | undefined = items.find(
      (v: VehicleListItemVm) => v.plate === 'RVA104'
    );

    expect(overdue?.maintenance?.worstDueStatus).toBe('Overdue');
    expect(overdue?.maintenance?.maxConsumedPercent).toBe(120);
    expect(overdue?.maintenance?.overdueCount).toBe(1);
    expect(overdue?.odometerKm).toBe(60_800);
  });

  it('should leave maintenance empty for inactive vehicles and vehicles without plan', () => {
    const items: readonly VehicleListItemVm[] = composeVehicleListItems(
      [buildVehicle('RVA110', 'Inactive'), buildVehicle('RVA999')],
      samplePlans
    );

    expect(items[0]?.maintenance).toBeNull();
    expect(items[1]?.maintenance).toBeNull();
    expect(items[1]?.odometerKm).toBeNull();
  });

  it('should keep searching when a vehicle has a null customer name', () => {
    const facade: FleetFacade = TestBed.inject(FleetFacade);
    facade.loadFleetList();
    facade.setSearchQuery('rva101');

    expect(facade.filteredVehicles().map((v: VehicleListItemVm) => v.plate)).toEqual(['RVA101']);
  });

  it('should flash vehicle and refresh list when RoutineChanged arrives', () => {
    const facade: FleetFacade = TestBed.inject(FleetFacade);
    facade.loadFleetList();

    routineChangedSubject.next({
      routineId: 'RT-RVA101-OIL',
      vehicleId: 'RVA101',
      plate: 'RVA101',
      customerId: 'CUST-1',
      routineTypeCode: 'OIL_CHANGE',
      routineName: 'Cambio de aceite',
      state: 'Active',
      dueStatus: 'Overdue',
      triggeredBy: 'Distance',
      consumedPercent: 102,
      effectiveDueSoonPercent: 10,
      nextDueKm: 50000,
      nextDueDate: '2026-12-01',
      odometerKm: 50200,
      occurredAt: '2026-11-12T09:00:00Z',
    });

    expect(facade.flashedVehicleIds().has('RVA101')).toBe(true);
    expect(sortVehiclesByUrgency(facade.sortedVehicles()).length).toBe(5);
  });
});
