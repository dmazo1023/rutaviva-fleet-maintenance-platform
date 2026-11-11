import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { SwitchComponent } from '../switch/switch.component';
import { SegmentedFilterComponent, SegmentedFilterOption } from './segmented-filter.component';

describe('SwitchComponent and SegmentedFilterComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SwitchComponent, SegmentedFilterComponent],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should toggle SwitchComponent aria-checked and emit toggled value', () => {
    const fixture: ComponentFixture<SwitchComponent> = TestBed.createComponent(SwitchComponent);
    fixture.componentRef.setInput('checked', true);
    fixture.componentRef.setInput('ariaLabel', 'Cambio de aceite activa');
    const emitted: boolean[] = [];
    fixture.componentInstance.toggled.subscribe((val: boolean) => emitted.push(val));
    fixture.detectChanges();

    const button: HTMLButtonElement | null = (
      fixture.nativeElement as HTMLElement
    ).querySelector('button[role="switch"]');
    expect(button?.getAttribute('aria-checked')).toBe('true');

    button?.click();
    expect(emitted).toEqual([false]);
  });

  it('should synchronize SegmentedFilterComponent radio change with URL query params', async () => {
    const router: Router = TestBed.inject(Router);
    const fixture: ComponentFixture<SegmentedFilterComponent> =
      TestBed.createComponent(SegmentedFilterComponent);

    const options: readonly SegmentedFilterOption[] = [
      { value: 'all', label: 'Todos', count: 10 },
      { value: 'Overdue', label: 'Vencida', count: 4, toneClass: 'is-danger' },
    ];
    fixture.componentRef.setInput('legend', 'Estado');
    fixture.componentRef.setInput('name', 'estado');
    fixture.componentRef.setInput('options', options);
    fixture.componentRef.setInput('selectedValue', 'all');
    fixture.componentRef.setInput('defaultValue', 'all');
    fixture.componentRef.setInput('queryParamKey', 'estado');
    fixture.detectChanges();

    const overdueRadio: HTMLInputElement | null = (
      fixture.nativeElement as HTMLElement
    ).querySelector('#estado-Overdue');
    expect(overdueRadio).not.toBeNull();

    overdueRadio?.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    expect(router.url).toContain('estado=Overdue');
  });
});

