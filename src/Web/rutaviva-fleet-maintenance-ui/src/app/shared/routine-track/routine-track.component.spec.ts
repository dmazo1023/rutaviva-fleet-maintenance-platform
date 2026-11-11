import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoutineTrackVm } from '../models/routine-track.vm';
import { RoutineTrackComponent } from './routine-track.component';

describe('RoutineTrackComponent', () => {
  let fixture: ComponentFixture<RoutineTrackComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutineTrackComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(RoutineTrackComponent);
  });

  it('should render 0% track as UpToDate with scaleX(0) and positive remaining', () => {
    const track: RoutineTrackVm = {
      kind: 'km',
      consumedPercent: 0,
      dueSoonPercent: 10,
      remaining: 10_000,
      status: 'UpToDate',
    };
    fixture.componentRef.setInput('track', track);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const meter: HTMLElement | null = host.querySelector('[role="meter"]');
    const fill: HTMLElement | null = host.querySelector('.trk-fill');
    const zone: HTMLElement | null = host.querySelector('.trk-zone');
    const rem: HTMLElement | null = host.querySelector('.trk-rem');

    expect(meter?.getAttribute('aria-valuenow')).toBe('0');
    expect(fill?.style.transform).toBe('scaleX(0)');
    expect(zone?.style.left).toBe('90%');
    expect(zone?.style.width).toBe('10%');
    expect(rem?.textContent?.trim()).toContain('10.000');
    expect(rem?.classList.contains('is-neg')).toBe(false);
  });

  it('should render threshold track (20% dueSoonPercent, 83% consumed) as DueSoon', () => {
    const track: RoutineTrackVm = {
      kind: 'km',
      consumedPercent: 83,
      dueSoonPercent: 20,
      remaining: 500,
      status: 'DueSoon',
    };
    fixture.componentRef.setInput('track', track);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const root: HTMLElement | null = host.querySelector('.trk');
    const zone: HTMLElement | null = host.querySelector('.trk-zone');
    const dot: HTMLElement | null = host.querySelector('.trk-dot');

    expect(root?.classList.contains('is-warn')).toBe(true);
    expect(zone?.style.left).toBe('80%');
    expect(zone?.style.width).toBe('20%');
    expect(dot?.style.left).toBe('83%');
  });

  it('should clamp fill and dot at 100% when consumed is 108% and show negative remaining', () => {
    const track: RoutineTrackVm = {
      kind: 'km',
      consumedPercent: 108,
      dueSoonPercent: 10,
      remaining: -800,
      status: 'Overdue',
    };
    fixture.componentRef.setInput('track', track);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const meter: HTMLElement | null = host.querySelector('[role="meter"]');
    const fill: HTMLElement | null = host.querySelector('.trk-fill');
    const dot: HTMLElement | null = host.querySelector('.trk-dot');
    const rem: HTMLElement | null = host.querySelector('.trk-rem');

    expect(meter?.getAttribute('aria-valuenow')).toBe('108');
    expect(fill?.style.transform).toBe('scaleX(1)');
    expect(dot?.style.left).toBe('100%');
    expect(rem?.classList.contains('is-neg')).toBe(true);
    expect(rem?.textContent?.trim()).toContain('−800');
  });
});

