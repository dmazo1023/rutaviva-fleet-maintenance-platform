import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { AlertsQuery } from '../../../../core/api/api-routes';
import { SessionStore } from '../../../../core/context/session.store';
import { NotificationsService } from '../../../../core/realtime/notifications.service';
import { AlertChangedEvent } from '../../../../core/realtime/realtime-events';
import { MaintenanceApi } from '../../data/maintenance.api';
import { AlertVm } from '../../models/maintenance.vm';
import { AlertsInboxPageComponent } from './alerts-inbox.page';

describe('AlertsInboxPageComponent', () => {
  let alertChangedSubject: Subject<AlertChangedEvent>;
  let currentAlerts: AlertVm[];
  let acknowledgeSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    alertChangedSubject = new Subject<AlertChangedEvent>();
    currentAlerts = [
      {
        alertId: 'ALT-01',
        vehicleId: 'RVA101',
        plate: 'RVA101',
        customerId: 'CUST-1',
        routineId: 'RT-1',
        routineName: 'Cambio de aceite',
        type: 'DueSoon',
        triggeredBy: 'Distance',
        status: 'Active',
        raisedAt: '12/11/2026 08:40',
        acknowledgedBy: null,
        resolutionReason: null,
      },
      {
        alertId: 'ALT-02',
        vehicleId: 'RVA102',
        plate: 'RVA102',
        customerId: 'CUST-1',
        routineId: 'RT-2',
        routineName: 'Frenos',
        type: 'Overdue',
        triggeredBy: 'Time',
        status: 'Resolved',
        raisedAt: '10/11/2026 09:00',
        acknowledgedBy: 'Operador',
        resolutionReason: 'RoutineRestarted',
      },
    ];

    acknowledgeSpy = vi.fn((alertId: string) => {
      currentAlerts = currentAlerts.map((a: AlertVm) =>
        a.alertId === alertId ? { ...a, status: 'Acknowledged', acknowledgedBy: 'Operador' } : a
      );
      return of(undefined);
    });

    await TestBed.configureTestingModule({
      imports: [AlertsInboxPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        {
          provide: MaintenanceApi,
          useValue: {
            getAlerts: (query: AlertsQuery) => {
              const items: AlertVm[] = currentAlerts.filter(
                (a: AlertVm) => a.status === query.status
              );
              return of({ items, totalCount: items.length });
            },
            acknowledgeAlert: acknowledgeSpy,
          },
        },
        {
          provide: NotificationsService,
          useValue: {
            alertChanged$: alertChangedSubject.asObservable(),
            syncSession: () => Promise.resolve(),
          },
        },
      ],
    }).compileComponents();
  });

  it('should filter by Active/Resolved, call acknowledge on button click, display resolution reason, and update rail counter on AlertChanged', () => {
    const sessionStore: SessionStore = TestBed.inject(SessionStore);
    const fixture: ComponentFixture<AlertsInboxPageComponent> =
      TestBed.createComponent(AlertsInboxPageComponent);
    fixture.detectChanges();

    expect(sessionStore.activeAlertsCount()).toBe(1);

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const ackBtn: HTMLButtonElement | null = host.querySelector('.alert-row__ack-btn');
    expect(ackBtn).not.toBeNull();
    ackBtn?.click();
    fixture.detectChanges();

    expect(acknowledgeSpy).toHaveBeenCalledWith('ALT-01');
    expect(sessionStore.activeAlertsCount()).toBe(0);

    fixture.componentInstance.onFilterChange('Resolved');
    fixture.detectChanges();
    expect(host.textContent).toContain('Resuelta: rutina reiniciada');

    currentAlerts = [
      ...currentAlerts,
      {
        alertId: 'ALT-03',
        vehicleId: 'RVA104',
        plate: 'RVA104',
        customerId: 'CUST-1',
        routineId: 'RT-3',
        routineName: 'Llantas',
        type: 'Overdue',
        triggeredBy: 'DistanceAndTime',
        status: 'Active',
        raisedAt: '12/11/2026 09:05',
        acknowledgedBy: null,
        resolutionReason: null,
      },
    ];
    alertChangedSubject.next({
      alertId: 'ALT-03',
      vehicleId: 'RVA104',
      plate: 'RVA104',
      customerId: 'CUST-1',
      routineId: 'RT-3',
      routineName: 'Llantas',
      type: 'Overdue',
      triggeredBy: 'DistanceAndTime',
      status: 'Active',
      raisedAt: '12/11/2026 09:05',
      acknowledgedBy: null,
      resolutionReason: null,
      occurredAt: '2026-11-12T09:05:00Z',
    });
    fixture.detectChanges();

    expect(sessionStore.activeAlertsCount()).toBe(1);
  });
});
