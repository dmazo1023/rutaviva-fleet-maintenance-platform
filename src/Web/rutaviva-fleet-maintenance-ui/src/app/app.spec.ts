import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { BROWSER_STORAGE } from './core/context/session.store';
import { mockBackendInterceptor } from './core/mocks/mock-backend.interceptor';
import { HUB_CONNECTION_FACTORY } from './core/realtime/notifications.service';

describe('App and Router', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter(routes, withComponentInputBinding()),
        provideHttpClient(withInterceptors([mockBackendInterceptor])),
        { provide: BROWSER_STORAGE, useValue: null },
        { provide: HUB_CONNECTION_FACTORY, useValue: () => null },
      ],
    }).compileComponents();
  });

  it('should create the app shell with rail, top bar, and main landmark', () => {
    const fixture: ComponentFixture<App> = TestBed.createComponent(App);
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('app-shell-rail')).not.toBeNull();
    expect(host.querySelector('app-top-bar')).not.toBeNull();
    expect(host.querySelector('main#main')).not.toBeNull();
  });

  it('should activate Lecturas tab when navigating to /flota/RVA101?tab=lecturas', async () => {
    const fixture: ComponentFixture<App> = TestBed.createComponent(App);
    const router: Router = TestBed.inject(Router);

    await router.navigateByUrl('/flota/RVA101?tab=lecturas');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const lecturasTab: HTMLElement | null = host.querySelector('#tab-lecturas');
    expect(lecturasTab?.getAttribute('aria-selected')).toBe('true');
    expect(host.querySelector('#panel-lecturas')).not.toBeNull();
  });
});
