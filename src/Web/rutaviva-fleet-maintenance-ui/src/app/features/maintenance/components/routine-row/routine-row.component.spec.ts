import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoutineVm } from '../../models/maintenance.vm';
import { RoutineRowComponent } from './routine-row.component';

describe('RoutineRowComponent', () => {
  const baseRoutine: RoutineVm = {
    routineId: 'RT-RVA101-OIL',
    vehicleId: 'RVA101',
    plate: 'RVA101',
    name: 'Cambio de aceite',
    typeCode: 'OIL_CHANGE',
    icon: 'oil_barrel',
    state: 'Active',
    dueStatus: 'DueSoon',
    triggeredBy: 'Distance',
    tracks: [
      {
        kind: 'km',
        consumedPercent: 92,
        dueSoonPercent: 10,
        remaining: 800,
        status: 'DueSoon',
      },
      {
        kind: 'time',
        consumedPercent: 45,
        dueSoonPercent: 10,
        remaining: 95,
        status: 'UpToDate',
      },
    ],
    nextDueKm: 50000,
    nextDueDate: '2027-02-15',
    cycleStartKm: 40000,
    cycleStartDate: '2026-08-15',
    intervalKm: 10000,
    intervalMonths: 6,
    defaultIntervalKm: 10000,
    defaultIntervalMonths: 6,
    defaultDueSoonPercent: 10,
    intervalKmOverride: null,
    intervalMonthsOverride: null,
    dueSoonPercentOverride: null,
    effectiveDueSoonPercent: 10,
    thresholdSource: 'Type',
    openServiceOrderId: null,
    openServiceOrderNumber: null,
    openServiceOrderStatus: null,
    canCreateOrder: true,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutineRowComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('should render tracks, create order button when canCreateOrder is true, and emit toggleState on switch click', () => {
    const fixture: ComponentFixture<RoutineRowComponent> =
      TestBed.createComponent(RoutineRowComponent);
    fixture.componentRef.setInput('routine', baseRoutine);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelectorAll('app-routine-track').length).toBe(2);
    expect(host.querySelector('.routine-row__create-btn')?.textContent).toContain('Crear orden');

    let toggled: RoutineVm | undefined;
    fixture.componentInstance.toggleState.subscribe((val: RoutineVm) => {
      toggled = val;
    });

    const switchBtn: HTMLButtonElement | null = host.querySelector('button[role="switch"]');
    switchBtn?.click();
    expect(toggled?.routineId).toBe('RT-RVA101-OIL');
  });

  it('should render open order link instead of create button when openServiceOrderId is present', () => {
    const fixture: ComponentFixture<RoutineRowComponent> =
      TestBed.createComponent(RoutineRowComponent);
    fixture.componentRef.setInput('routine', {
      ...baseRoutine,
      openServiceOrderId: 'OS-2026-001',
      openServiceOrderNumber: 'OS-2026-001',
      openServiceOrderStatus: 'PendingApproval',
      canCreateOrder: false,
    });
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.routine-row__create-btn')).toBeNull();
    expect(host.querySelector('.routine-row__order-link')?.textContent).toContain('OS-2026-001');
  });
});
