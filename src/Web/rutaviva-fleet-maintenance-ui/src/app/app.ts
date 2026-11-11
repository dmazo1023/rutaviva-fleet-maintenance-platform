import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { SessionStore } from './core/context/session.store';
import { ShellRailComponent } from './core/shell/shell-rail.component';
import { SkipLinkComponent } from './core/shell/skip-link.component';
import { TopBarComponent } from './core/shell/top-bar.component';
import { ToastOutletComponent } from './shared/toast-outlet/toast-outlet.component';

@Component({
  imports: [
    RouterOutlet,
    SkipLinkComponent,
    ShellRailComponent,
    TopBarComponent,
    ToastOutletComponent,
  ],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App implements OnInit {
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly router: Router = inject(Router);
  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor() {
    this.router.events
      .pipe(
        filter((ev): ev is NavigationEnd => ev instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        setTimeout(() => {
          if (typeof document === 'undefined') {
            return;
          }
          const heading: HTMLElement | null =
            document.getElementById('h1') ?? document.getElementById('d-title');
          heading?.focus({ preventScroll: true });
        }, 0);
      });
  }

  ngOnInit(): void {
    void this.sessionStore.loadContextCatalogs();
  }
}
