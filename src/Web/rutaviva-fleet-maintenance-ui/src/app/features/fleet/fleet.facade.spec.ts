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
import { FleetFacade } from './fleet.facade';
import { VehicleDetailVm, VehicleListItemVm } from './models/fleet.vm';
import { sortVehiclesByUrgency } from './state/fleet.store';

describe('FleetFacade', () => {
  let routineChangedSubject: Subject<RoutineChangedEvent>;
  let odometerRecordedSubject: Subject<OdometerReadingRecordedEvent>;
  let vehicleChangedSubject: Subject<VehicleChangedEvent>;
  let orderChangedSubject: Subject<ServiceOrderChangedEvent>;

  const sampleVehicles: readonly VehicleDetailVm[] = [
    {
      vehicleId: 'RVA103',
      plate: 'RVA103',
      vin: 'VIN103',
      brand: 'Chevrolet',
      model: 'NHR',
      modelYear: 2022,
      customerId: 'CUST-1',
      customerName: 'Logística Andina',
      workshopId: 'WS-1',
      workshopName: 'Taller Norte',
      responsibleApproverId: 'APP-1',
      responsibleApproverName: 'Laura Gómez',
      status: 'Active',
      odometerKm: 22100,
      lastReadingText: 'hace 1 h',
    },
    {
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
      responsibleApproverId: 'APP-1',
      responsibleApproverName: 'Laura Gómez',
      status: 'Inactive',
      odometerKm: 58900,
      lastReadingText: 'hace 90 días',
    },
    {
      vehicleId: 'RVA104',
      plate: 'RVA104',
      vin: 'VIN104',
      brand: 'Hino',
      model: 'FC9J',
      modelYear: 2021,
      customerId: 'CUST-1',
      customerName: 'Logística Andina',
      workshopId: 'WS-1',
      workshopName: 'Taller Norte',
      responsibleApproverId: 'APP-1',
      responsibleApproverName: 'Laura Gómez',
      status: 'Active',
      odometerKm: 60800,
      lastReadingText: 'hace 5 min',
    },
    {
      vehicleId: 'RVA101',
      plate: 'RVA101',
      vin: 'VIN101',
      brand: 'Chevrolet',
      model: 'NHR',
      modelYear: 2022,
      customerId: 'CUST-1',
      customerName: 'Logística Andina',
      workshopId: 'WS-1',
      workshopName: 'Taller Norte',
      responsibleApproverId: 'APP-1',
      responsibleApproverName: 'Laura Gómez',
      status: 'Active',
      odometerKm: 49200,
      lastReadingText: 'hace 3 min',
    },
    {
      vehicleId: 'RVA102',
      plate: 'RVA102',
      vin: 'VIN102',
      brand: 'Chevrolet',
      model: 'NPR',
      modelYear: 2021,
      customerId: 'CUST-1',
      customerName: 'Logística Andina',
      workshopId: 'WS-1',
      workshopName: 'Taller Norte',
      responsibleApproverId: 'APP-1',
      responsibleApproverName: 'Laura Gómez',
      status: 'Active',
      odometerKm: 81400,
      lastReadingText: 'hace 12 min',
    },
  ];

  const samplePlans: readonly MaintenancePlanSummaryVm[] = [
    {
      vehicleId: 'RVA103',
      plate: 'RVA103',
      customerId: 'CUST-1',
      status: 'Active',
      odometerKm: 22100,
      worstDueStatus: 'UpToDate',
      worstConsumedPercent: 21,
      worstTriggeredBy: null,
      nextRoutineName: 'Cambio de aceite',
      nextRemainingKind: 'km',
      nextRemainingValue: 7900,
    },
    {
      vehicleId: 'RVA110',
      plate: 'RVA110',
      customerId: 'CUST-1',
      status: 'Suspended',
      odometerKm: 58900,
      worstDueStatus: null,
      worstConsumedPercent: 0,
      worstTriggeredBy: null,
      nextRoutineName: null,
      nextRemainingKind: null,
      nextRemainingValue: null,
    },
    {
      vehicleId: 'RVA104',
      plate: 'RVA104',
      customerId: 'CUST-1',
      status: 'Active',
      odometerKm: 60800,
      worstDueStatus: 'Overdue',
      worstConsumedPercent: 120,
      worstTriggeredBy: 'DistanceAndTime',
      nextRoutineName: 'Frenos',
      nextRemainingKind: 'km',
      nextRemainingValue: -4800,
    },
    {
      vehicleId: 'RVA101',
      plate: 'RVA101',
      customerId: 'CUST-1',
      status: 'Active',
      odometerKm: 49200,
      worstDueStatus: 'DueSoon',
      worstConsumedPercent: 92,
      worstTriggeredBy: 'Distance',
      nextRoutineName: 'Cambio de aceite',
      nextRemainingKind: 'km',
      nextRemainingValue: 800,
    },
    {
      vehicleId: 'RVA102',
      plate: 'RVA102',
      customerId: 'CUST-1',
      status: 'Active',
      odometerKm: 81400,
      worstDueStatus: 'Overdue',
      worstConsumedPercent: 114,
      worstTriggeredBy: 'Distance',
      nextRoutineName: 'Cambio de aceite',
      nextRemainingKind: 'km',
      nextRemainingValue: -1400,
    },
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
            getVehicleOrders: () => of([]),
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

  it('should sort vehicles by urgency (Overdue -> DueSoon -> UpToDate -> Inactive) and then by worstConsumedPercent descending', () => {
    const facade: FleetFacade = TestBed.inject(FleetFacade);
    facade.loadFleetList();

    const sortedPlates: readonly string[] = facade
      .sortedVehicles()
      .map((v: VehicleListItemVm) => v.plate);
    expect(sortedPlates).toEqual(['RVA104', 'RVA102', 'RVA101', 'RVA103', 'RVA110']);
  });

  it('should flash vehicle and refresh list when RoutineChanged or OdometerReadingRecorded arrives', () => {
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
