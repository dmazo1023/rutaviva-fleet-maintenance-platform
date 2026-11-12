import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { OdometerReadingVm } from '../../models/maintenance.vm';
import { OdometerReadingsComponent } from './odometer-readings.component';

describe('OdometerReadingsComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OdometerReadingsComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('should render applied reading and struck-through ignored reading with reason', () => {
    const sampleReadings: readonly OdometerReadingVm[] = [
      {
        kilometers: 49200,
        readAt: '12/11/2026 08:40',
        outcome: 'Accepted',
        ignoredReason: null,
      },
      {
        kilometers: 47000,
        readAt: '11/11/2026 14:20',
        outcome: 'Ignored',
        ignoredReason: 'NotGreaterThanCurrent',
      },
    ];

    const fixture: ComponentFixture<OdometerReadingsComponent> =
      TestBed.createComponent(OdometerReadingsComponent);
    fixture.componentRef.setInput('readings', sampleReadings);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const rows: NodeListOf<HTMLLIElement> = host.querySelectorAll('li.row');
    expect(rows.length).toBe(2);
    expect(rows[0]?.classList.contains('rd-ignored')).toBe(false);
    expect(rows[1]?.classList.contains('rd-ignored')).toBe(true);
    expect(rows[1]?.querySelector('.t')).not.toBeNull();
    expect(rows[1]?.textContent).toContain('Ignorada');
    expect(rows[1]?.textContent).toContain('No es mayor que el odómetro actual');
  });

  it('should render empty state when readings list is empty', () => {
    const fixture: ComponentFixture<OdometerReadingsComponent> =
      TestBed.createComponent(OdometerReadingsComponent);
    fixture.componentRef.setInput('readings', []);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Sin lecturas');
  });
});
