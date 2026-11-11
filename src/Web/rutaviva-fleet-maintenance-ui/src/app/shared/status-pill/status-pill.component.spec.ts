import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlateChipComponent } from '../plate-chip/plate-chip.component';
import { StatusPillComponent } from './status-pill.component';

describe('PlateChipComponent and StatusPillComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PlateChipComponent, StatusPillComponent],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should render accessible sr-only trigger label for Distance, Time, and DistanceAndTime', () => {
    const fixture: ComponentFixture<StatusPillComponent> =
      TestBed.createComponent(StatusPillComponent);
    fixture.componentRef.setInput('status', 'Overdue');
    fixture.componentRef.setInput('trigger', 'DistanceAndTime');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const srOnly: HTMLElement | null = host.querySelector('.sr-only');
    const icons: NodeListOf<HTMLElement> = host.querySelectorAll('.trig .ms');

    expect(host.textContent).toContain('Vencida');
    expect(srOnly?.textContent?.trim()).toBe('Por km y tiempo');
    expect(icons.length).toBe(2);
    expect(icons[0]?.textContent?.trim()).toBe('speed');
    expect(icons[1]?.textContent?.trim()).toBe('event');
  });

  it('should render PlateChipComponent as anchor when link is provided and span otherwise', () => {
    const fixture: ComponentFixture<PlateChipComponent> =
      TestBed.createComponent(PlateChipComponent);
    fixture.componentRef.setInput('plate', 'RVA101');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('span.plate')?.textContent?.trim()).toBe('RVA101');

    fixture.componentRef.setInput('link', '/flota/RVA101');
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();

    const anchor: HTMLAnchorElement | null = host.querySelector('a.plate.plate--lg');
    expect(anchor?.textContent?.trim()).toBe('RVA101');
  });
});

