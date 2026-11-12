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
        odometerKm: 45000,
        readAt: '2026-11-12T09:00:00-05:00',
        outcome: 'Ignored' as const,
        ignoredReason: 'NotGreaterThanCurrent' as const,
      })
    );
    runTimeSpy = vi.fn(() =>
      of({
        asOfDate: '2026-11-12',
        evaluatedPlans: 9,
        dueSoon: 3,
        overdue: 4,
        ordersRequested: 3,
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

    expect(recordOdoSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        plate: 'RVA101',
        odometerKm: 50200,
        readAt: expect.stringMatching(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:00-05:00$/),
      })
    );
    expect(host.textContent).toContain('Ignorada');
    expect(host.textContent).toContain('No es mayor que el odómetro actual');

    forms[1]?.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(runTimeSpy).toHaveBeenCalled();
    expect(host.textContent).toContain('9');
    expect(host.textContent).toContain('Planes evaluados');
    expect(host.textContent).toContain('2026-11-12');
  });
});

