import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'flota',
  },
  {
    path: 'flota',
    loadChildren: () =>
      import('./features/fleet/fleet.routes').then((m) => m.FLEET_ROUTES),
  },
  {
    path: 'alertas',
    loadComponent: () =>
      import('./features/maintenance/pages/alerts-inbox/alerts-inbox.page').then(
        (m) => m.AlertsInboxPageComponent
      ),
  },
  {
    path: 'ordenes',
    loadChildren: () =>
      import('./features/service-orders/service-orders.routes').then(
        (m) => m.SERVICE_ORDERS_ROUTES
      ),
  },
  {
    path: 'parametros',
    loadComponent: () =>
      import('./features/administration/pages/parameters-page/parameters.page').then(
        (m) => m.ParametersPageComponent
      ),
  },
  {
    path: 'carga',
    loadComponent: () =>
      import('./features/data-import/pages/data-import-page/data-import.page').then(
        (m) => m.DataImportPageComponent
      ),
  },
  {
    path: 'reportes',
    loadComponent: () =>
      import('./features/reports/pages/reports-page/reports.page').then(
        (m) => m.ReportsPageComponent
      ),
  },
  {
    path: 'simulador',
    loadComponent: () =>
      import('./features/simulators/pages/simulators-page/simulators.page').then(
        (m) => m.SimulatorsPageComponent
      ),
  },
  {
    path: '**',
    redirectTo: 'flota',
  },
];
