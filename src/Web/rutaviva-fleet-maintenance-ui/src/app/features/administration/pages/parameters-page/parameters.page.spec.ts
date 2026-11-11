import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { SessionStore } from '../../../../core/context/session.store';
import { AdministrationApi } from '../../data/administration.api';
import { ParametersPageComponent } from './parameters.page';

describe('ParametersPageComponent', () => {
  let updateRoutineSpy: ReturnType<typeof vi.fn>;
  let updatePolicySpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    updateRoutineSpy = vi.fn((id: string) =>
      of({
        id,
        code: 'OIL_CHANGE',
        name: 'Cambio de aceite',
        icon: 'oil_barrel',
        defaultIntervalKm: 12000,
        defaultIntervalMonths: 6,
        dueSoonThresholdPercent: 10,
        status: 'Active' as const,
      })
    );
    updatePolicySpy = vi.fn((amount: number, createdBy: string) =>
      of({
        currentAmount: amount,
        currency: 'COP',
        history: [
          {
            id: 'POL-2',
            currency: 'COP',
            autoApprovalMaxAmount: amount,
            effectiveFrom: '12/11/2026 09:00',
            createdBy,
          },
        ],
      })
    );

    await TestBed.configureTestingModule({
      imports: [ParametersPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: AdministrationApi,
          useValue: {
            getRoutineTypes: () =>
              of([
                {
                  id: 'RT-OIL',
                  code: 'OIL_CHANGE',
                  name: 'Cambio de aceite',
                  icon: 'oil_barrel',
                  defaultIntervalKm: 10000,
                  defaultIntervalMonths: 6,
                  dueSoonThresholdPercent: 10,
                  status: 'Active',
                },
              ]),
            updateRoutineType: updateRoutineSpy,
            getApprovalPolicy: () =>
              of({
                currentAmount: 1500000,
                currency: 'COP',
                history: [],
              }),
            updateApprovalPolicy: updatePolicySpy,
          },
        },
      ],
    }).compileComponents();
  });

  it('should allow Operator to save routine type and approval policy and switch to read-only for Approver', () => {
    const sessionStore: SessionStore = TestBed.inject(SessionStore);
    sessionStore.setProfile('Operator');

    const fixture: ComponentFixture<ParametersPageComponent> =
      TestBed.createComponent(ParametersPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const saveTypeBtn: HTMLButtonElement | null = host.querySelector('.parameters-table__save-btn');
    saveTypeBtn?.click();
    expect(updateRoutineSpy).toHaveBeenCalled();

    const savePolicyBtn: HTMLButtonElement | null = host.querySelector(
      '.parameters-policy__save-btn'
    );
    savePolicyBtn?.click();
    expect(updatePolicySpy).toHaveBeenCalled();

    sessionStore.setProfile('Approver');
    fixture.detectChanges();
    expect(host.querySelector('.parameters-page__readonly')).not.toBeNull();
    expect(host.querySelector('.parameters-table__save-btn')).toBeNull();
  });
});

