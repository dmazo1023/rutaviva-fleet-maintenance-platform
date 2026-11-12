import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { SessionStore } from '../context/session.store';
import { ShellCountersService } from './shell-counters.service';
import { SHELL_TEXTS } from './shell.texts';

export interface ShellNavItem {
  readonly route: string;
  readonly icon: string;
  readonly label: string;
  readonly badgeCount: number;
  readonly badgeTone: 'danger' | 'warn';
}

@Component({
  selector: 'app-shell-rail',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './shell-rail.component.html',
  styleUrl: './shell-rail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellRailComponent implements OnInit {
  protected readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly counters: ShellCountersService = inject(ShellCountersService);
  protected readonly texts = SHELL_TEXTS;

  ngOnInit(): void {
    this.counters.loadActiveAlerts();
  }

  protected readonly navItems = computed<readonly ShellNavItem[]>(() => {
    const alertsCount: number = this.sessionStore.activeAlertsCount();
    const isApprover: boolean = this.sessionStore.profile() === 'Approver';
    const isOperator: boolean = this.sessionStore.profile() === 'Operator';
    const ordersCount: number = isApprover ? this.sessionStore.pendingApprovalsCount() : 0;

    const baseItems: ShellNavItem[] = [
      {
        route: '/flota',
        icon: 'local_shipping',
        label: this.texts.sections.fleet,
        badgeCount: 0,
        badgeTone: 'danger',
      },
      {
        route: '/alertas',
        icon: 'notifications',
        label: this.texts.sections.alerts,
        badgeCount: alertsCount,
        badgeTone: 'danger',
      },
      {
        route: '/ordenes',
        icon: 'assignment',
        label: this.texts.sections.orders,
        badgeCount: ordersCount,
        badgeTone: 'warn',
      },
      {
        route: '/parametros',
        icon: 'tune',
        label: this.texts.sections.parameters,
        badgeCount: 0,
        badgeTone: 'danger',
      },
      {
        route: '/carga',
        icon: 'upload_file',
        label: this.texts.sections.import,
        badgeCount: 0,
        badgeTone: 'danger',
      },
      {
        route: '/reportes',
        icon: 'bar_chart',
        label: this.texts.sections.reports,
        badgeCount: 0,
        badgeTone: 'danger',
      },
    ];

    if (isOperator) {
      baseItems.push({
        route: '/simulador',
        icon: 'science',
        label: this.texts.sections.simulators,
        badgeCount: 0,
        badgeTone: 'danger',
      });
    }

    return baseItems;
  });
}

