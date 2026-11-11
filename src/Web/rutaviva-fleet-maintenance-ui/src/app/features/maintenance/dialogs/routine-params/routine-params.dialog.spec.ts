import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoutineVm } from '../../models/maintenance.vm';
import { RoutineParamsDialogComponent } from './routine-params.dialog';

describe('RoutineParamsDialogComponent', () => {
  const sampleRoutine: RoutineVm = {
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
      imports: [RoutineParamsDialogComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('should update live preview when interval input changes and validate when both intervals are empty', () => {
    const fixture: ComponentFixture<RoutineParamsDialogComponent> =
      TestBed.createComponent(RoutineParamsDialogComponent);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('routine', sampleRoutine);
    fixture.componentRef.setInput('currentOdometerKm', 49200);
    fixture.detectChanges();

    expect(fixture.componentInstance.previewTracks().length).toBe(2);

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const kmInput: HTMLInputElement | null = host.querySelector('.params-dialog__input-km');
    const monthsInput: HTMLInputElement | null = host.querySelector('.params-dialog__input-months');

    if (kmInput && monthsInput) {
      kmInput.value = '';
      kmInput.dispatchEvent(new Event('input'));
      monthsInput.value = '';
      monthsInput.dispatchEvent(new Event('input'));
    }
    fixture.detectChanges();

    const form: HTMLFormElement | null = host.querySelector('form');
    form?.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(host.querySelector('.params-dialog__error')?.textContent).toContain(
      'Indica al menos un intervalo'
    );
  });
});

