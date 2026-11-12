import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { ToastStore } from '../feedback/toast.store';
import { BROWSER_STORAGE } from '../context/session.store';
import { HUB_CONNECTION_FACTORY, NotificationsService } from '../realtime/notifications.service';
import { TopBarComponent } from './top-bar.component';
import { ViewRefreshService } from './view-refresh.service';

describe('TopBarComponent', () => {
  let fixture: ComponentFixture<TopBarComponent>;
  let notifications: NotificationsService;
  let httpMock: HttpTestingController;
  let viewRefresh: ViewRefreshService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TopBarComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: BROWSER_STORAGE, useValue: null },
        { provide: HUB_CONNECTION_FACTORY, useValue: () => null },
      ],
    }).compileComponents();

    notifications = TestBed.inject(NotificationsService);
    httpMock = TestBed.inject(HttpTestingController);
    viewRefresh = TestBed.inject(ViewRefreshService);
    fixture = TestBed.createComponent(TopBarComponent);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should display "En vivo" when connected and "Reconectando…" when reconnecting', () => {
    notifications.setConnectionStateForTest('connected');
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const statusEl: HTMLElement | null = host.querySelector('.live');

    expect(statusEl?.textContent?.trim()).toBe('En vivo');
    expect(statusEl?.classList.contains('is-off')).toBe(false);

    notifications.setConnectionStateForTest('reconnecting');
    fixture.detectChanges();

    expect(statusEl?.textContent?.trim()).toBe('Reconectando…');
    expect(statusEl?.classList.contains('is-off')).toBe(true);
  });

  function changeProfile(profile: string): void {
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const select: HTMLSelectElement | null = host.querySelector('select[aria-label="Perfil"]');
    if (select) {
      select.value = profile;
      select.dispatchEvent(new Event('change'));
    }
  }

  it('should navigate to the open orders inbox and announce the profile when switching to Workshop', () => {
    const router: Router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const toastStore: ToastStore = TestBed.inject(ToastStore);
    fixture.detectChanges();

    changeProfile('Workshop');

    expect(navigateSpy).toHaveBeenCalledWith(['/ordenes'], {
      queryParams: { statusGroup: 'Open' },
    });
    expect(toastStore.toasts()[0]?.message).toContain('Ahora trabajas como Taller');
  });

  it('should navigate approvers to pending approvals and operators to the fleet', () => {
    const router: Router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const toastStore: ToastStore = TestBed.inject(ToastStore);
    fixture.detectChanges();

    changeProfile('Approver');
    expect(navigateSpy).toHaveBeenLastCalledWith(['/ordenes'], {
      queryParams: { statusGroup: 'PendingApproval' },
    });

    changeProfile('Operator');
    expect(navigateSpy).toHaveBeenLastCalledWith(['/flota'], { queryParams: {} });
    expect(toastStore.toasts().some((t) => t.message === 'Ahora trabajas como Operador')).toBe(true);
  });

  it('should display "Sin tiempo real" when realtime is unavailable', () => {
    notifications.setConnectionStateForTest('unavailable');
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const statusEl: HTMLElement | null = host.querySelector('.live');

    expect(statusEl?.textContent?.trim()).toBe('Sin tiempo real');
    expect(statusEl?.classList.contains('is-off')).toBe(true);
  });

  it('should disable refresh button with aria-busy="true" and ellipsis text while reevaluating', () => {
    viewRefresh.setActiveVehicleId('RVA101');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const button: HTMLButtonElement | null = host.querySelector('.refresh-btn');
    expect(button).not.toBeNull();

    button?.click();
    fixture.detectChanges();

    expect(button?.disabled).toBe(true);
    expect(button?.getAttribute('aria-busy')).toBe('true');
    expect(button?.textContent?.trim().endsWith('…')).toBe(true);

    const req = httpMock.expectOne('/api/v1/maintenance/reevaluations');
    expect(req.request.body).toEqual({ vehicleId: 'RVA101' });
    req.flush({
      evaluatedPlans: 1,
      dueSoonRoutines: 0,
      overdueRoutines: 0,
      requestedOrders: 0,
    });
    fixture.detectChanges();

    expect(button?.disabled).toBe(false);
    expect(button?.getAttribute('aria-busy')).toBeNull();
  });
});

