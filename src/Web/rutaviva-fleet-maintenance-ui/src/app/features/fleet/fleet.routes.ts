import { Routes } from '@angular/router';

export const FLEET_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/fleet-page/fleet.page').then((m) => m.FleetPageComponent),
  },
  {
    path: ':plate',
    loadComponent: () =>
      import('./pages/vehicle-detail/vehicle-detail.page').then(
        (m) => m.VehicleDetailPageComponent
      ),
  },
];

