import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RegisterServiceRequest, RoutineVm } from '../../models/maintenance.vm';
import {
  ServiceRegistrationDialogComponent,
  SERVICE_TIMEZONE_OFFSET,
  todayIsoDate,
} from './service-registration.dialog';

const PLAN_ODOMETER_KM = 49200;

describe('ServiceRegistrationDialogComponent', () => {
  const routine: RoutineVm = {
    routineId: 'RT-1',
    vehicleId: 'VEH-1',
    plate: 'RVA101',
    name: 'Cambio de aceite',
    typeCode: 'OIL_CHANGE',
    icon: 'oil_barrel',
    state: 'Active',
    dueStatus: 'Overdue',
    triggeredBy: 'Distance',
    tracks: [],
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
    canCreateOrder: false,
  };

  let fixture: ComponentFixture<ServiceRegistrationDialogComponent>;
  let host: HTMLElement;

  function setKm(value: string): void {
    const input: HTMLInputElement = host.querySelector('.service-dialog__input-km') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  function submit(): void {
    host.querySelector('form')?.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceRegistrationDialogComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(ServiceRegistrationDialogComponent);
    fixture.componentRef.setInput('routine', routine);
    fixture.componentRef.setInput('planOdometerKm', PLAN_ODOMETER_KM);
    fixture.detectChanges();
    host = fixture.nativeElement as HTMLElement;
  });

  it('should default km to the plan odometer and the date to today', () => {
    const km: HTMLInputElement = host.querySelector('.service-dialog__input-km') as HTMLInputElement;
    const date: HTMLInputElement = host.querySelector('.service-dialog__input-date') as HTMLInputElement;
    expect(km.value).toBe(String(PLAN_ODOMETER_KM));
    expect(date.value).toBe(todayIsoDate());
  });

  it('should emit servicedKm and servicedAt with the -05:00 offset on submit', () => {
    let emitted: RegisterServiceRequest | undefined;
    fixture.componentInstance.confirmed.subscribe((value: RegisterServiceRequest) => {
      emitted = value;
    });
    submit();
    expect(emitted?.servicedKm).toBe(PLAN_ODOMETER_KM);
    expect(emitted?.servicedAt).toBe(`${todayIsoDate()}T00:00:00${SERVICE_TIMEZONE_OFFSET}`);
  });

  it('should show a validation error and not emit when km is negative', () => {
    let emitted = false;
    fixture.componentInstance.confirmed.subscribe(() => {
      emitted = true;
    });
    setKm('-5');
    submit();
    expect(emitted).toBe(false);
    expect(host.querySelector('.service-dialog__error')?.textContent).toContain('0 o más');
  });
});
