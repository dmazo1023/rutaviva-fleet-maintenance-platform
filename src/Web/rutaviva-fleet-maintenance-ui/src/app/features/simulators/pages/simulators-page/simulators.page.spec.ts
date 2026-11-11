import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { SessionStore } from '../../../../core/context/session.store';
import { SimulatorsApi } from '../../data/simulators.api';
import { SimulatorsPageComponent } from './simulators.page';

describe('SimulatorsPageComponent', () => {
  let recordOdoSpy: ReturnType<typeof vi.fn>;
  let runTimeSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    recordOdoSpy = vi.fn(() =>
      of({
        plate: 'RVA101',
        kilometers: 45000,
        readAt: '12/11/2026 09:00',
        outcome: 'Ignored' as const,
        ignoredReason: 'NotGreaterThanCurrent' as const,
      })
    );
    runTimeSpy = vi.fn(() =>
      of({
        asOfDate: '2026-11-12',
        evaluatedPlans: 9,
        dueSoonRoutines: 3,
        overdueRoutines: 4,
        requestedOrders: 3,
      })
    );

    await TestBed.configureTestingModule({
      imports: [SimulatorsPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: SimulatorsApi,
          useValue: {
            recordOdometerReading: recordOdoSpy,
            runTimeEvaluation: runTimeSpy,
          },
        },
      ],
    }).compileComponents();
  });

  it('should submit odometer reading and time evaluation and display outcomes', () => {
    const sessionStore: SessionStore = TestBed.inject(SessionStore);
    sessionStore.setProfile('Operator');

    const fixture: ComponentFixture<SimulatorsPageComponent> =
      TestBed.createComponent(SimulatorsPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const forms: NodeListOf<HTMLFormElement> = host.querySelectorAll('form');
    forms[0]?.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(recordOdoSpy).toHaveBeenCalled();
    expect(host.textContent).toContain('Ignorada');
    expect(host.textContent).toContain('No es mayor que el odómetro actual');

    forms[1]?.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(runTimeSpy).toHaveBeenCalled();
    expect(host.textContent).toContain('9');
    expect(host.textContent).toContain('Planes evaluados');
  });
});

