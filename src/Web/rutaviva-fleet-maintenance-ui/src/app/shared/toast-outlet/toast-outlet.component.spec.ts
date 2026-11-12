import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { TOAST_DURATION_MS, ToastStore } from '../../core/feedback/toast.store';
import { ToastOutletComponent } from './toast-outlet.component';

describe('ToastStore and ToastOutletComponent', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should auto-dismiss non-persistent toast after 6 seconds and keep persistent toast', () => {
    const store: ToastStore = new ToastStore();
    store.show({ message: 'Alerta atendida' });
    store.show({
      message: 'Aprobación prioritaria',
      tone: 'warn',
      persistent: true,
    });

    expect(store.toasts().length).toBe(2);

    vi.advanceTimersByTime(TOAST_DURATION_MS);
    expect(store.toasts().length).toBe(1);
    expect(store.toasts()[0]?.message).toBe('Aprobación prioritaria');
  });

  it('should render aria-live="polite" and role="alert" for error toasts', async () => {
    await TestBed.configureTestingModule({
      imports: [ToastOutletComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    const store: ToastStore = TestBed.inject(ToastStore);
    store.show({
      message: 'Transición no permitida',
      detail: 'La orden ya fue aprobada.',
      tone: 'danger',
      isError: true,
    });

    const fixture: ComponentFixture<ToastOutletComponent> =
      TestBed.createComponent(ToastOutletComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const container: HTMLElement | null = host.querySelector('.toasts');
    expect(container?.getAttribute('aria-live')).toBe('polite');

    const alertEl: HTMLElement | null = host.querySelector('[role="alert"]');
    expect(alertEl).not.toBeNull();
    expect(alertEl?.textContent).toContain('Transición no permitida');
    expect(alertEl?.textContent).toContain('La orden ya fue aprobada.');
  });
});

