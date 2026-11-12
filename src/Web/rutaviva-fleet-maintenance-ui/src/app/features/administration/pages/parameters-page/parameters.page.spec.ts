import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { SessionStore } from '../../../../core/context/session.store';
import { AdministrationApi } from '../../data/administration.api';
import { ParametersPageComponent } from './parameters.page';

describe('ParametersPageComponent', () => {
  let updateRoutineSpy: ReturnType<typeof vi.fn>;
  let getRoutineTypesSpy: ReturnType<typeof vi.fn>;
  let getPolicySpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    updateRoutineSpy = vi.fn(() => of(undefined));
    getPolicySpy = vi.fn();
    getRoutineTypesSpy = vi.fn(() =>
      of([
        {
          id: 'RT-OIL',
          code: 'OIL_CHANGE',
          name: 'Cambio de aceite',
          defaultIntervalKm: 10000,
          defaultIntervalMonths: 6,
          dueSoonThresholdPercent: 10,
          status: 'Active',
        },
      ])
    );

    await TestBed.configureTestingModule({
      imports: [ParametersPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: AdministrationApi,
          useValue: {
            getRoutineTypes: getRoutineTypesSpy,
            updateRoutineType: updateRoutineSpy,
            getApprovalPolicy: getPolicySpy,
          },
        },
      ],
    }).compileComponents();
  });

  it('should send the real contract with actor, reload after 204 and show the next phase state for the approval policy', () => {
    const sessionStore: SessionStore = TestBed.inject(SessionStore);
    sessionStore.setProfile('Operator');

    const fixture: ComponentFixture<ParametersPageComponent> =
      TestBed.createComponent(ParametersPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    host.querySelector<HTMLButtonElement>('.parameters-table__save-btn')?.click();

    expect(updateRoutineSpy).toHaveBeenCalledWith('RT-OIL', {
      intervalKm: 10000,
      intervalMonths: 6,
      dueSoonThresholdPercent: 10,
      updatedBy: sessionStore.currentActorName(),
    });
    expect(getRoutineTypesSpy).toHaveBeenCalledTimes(2);
    expect(getPolicySpy).not.toHaveBeenCalled();
    expect(host.textContent).toContain('Próxima fase');
  });

  it('should switch to read-only for Approver', () => {
    const sessionStore: SessionStore = TestBed.inject(SessionStore);
    sessionStore.setProfile('Approver');
    const fixture: ComponentFixture<ParametersPageComponent> =
      TestBed.createComponent(ParametersPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.parameters-page__readonly')).not.toBeNull();
    expect(host.querySelector('.parameters-table__save-btn')).toBeNull();
  });
});
