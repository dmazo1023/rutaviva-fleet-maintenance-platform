import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { BROWSER_STORAGE, SessionStore } from '../context/session.store';
import { HUB_CONNECTION_FACTORY } from '../realtime/notifications.service';
import { ShellRailComponent } from './shell-rail.component';

describe('ShellRailComponent', () => {
  let fixture: ComponentFixture<ShellRailComponent>;
  let sessionStore: SessionStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShellRailComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BROWSER_STORAGE, useValue: null },
        { provide: HUB_CONNECTION_FACTORY, useValue: () => null },
      ],
    }).compileComponents();

    sessionStore = TestBed.inject(SessionStore);
    fixture = TestBed.createComponent(ShellRailComponent);
  });

  it('should display both alerts and pending approvals counters when profile is Approver', () => {
    sessionStore.setProfile('Approver');
    sessionStore.setActiveAlertsCount(3);
    sessionStore.setPendingApprovalsCount(2);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const alertsBadge: HTMLElement | null = host.querySelector('[data-badge="/alertas"]');
    const ordersBadge: HTMLElement | null = host.querySelector('[data-badge="/ordenes"]');

    expect(alertsBadge?.textContent?.trim()).toBe('3');
    expect(ordersBadge?.textContent?.trim()).toBe('2');
  });

  it('should hide pending approvals counter when profile is Workshop', () => {
    sessionStore.setProfile('Workshop');
    sessionStore.setActiveAlertsCount(3);
    sessionStore.setPendingApprovalsCount(2);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const alertsBadge: HTMLElement | null = host.querySelector('[data-badge="/alertas"]');
    const ordersBadge: HTMLElement | null = host.querySelector('[data-badge="/ordenes"]');

    expect(alertsBadge?.textContent?.trim()).toBe('3');
    expect(ordersBadge).toBeNull();
  });
});

