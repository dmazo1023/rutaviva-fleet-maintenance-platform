import { Routes } from '@angular/router';
import { SERVICE_ORDERS_FEATURE_ENABLED } from './service-orders.feature';

const COMING_SOON_ROUTES: Routes = [
  {
    path: '**',
    loadComponent: () =>
      import('./pages/coming-soon/coming-soon.page').then(
        (m) => m.ServiceOrdersComingSoonPageComponent
      ),
  },
];

const ENABLED_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/orders-inbox/orders-inbox.page').then((m) => m.OrdersInboxPageComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/order-detail/order-detail.page').then((m) => m.OrderDetailPageComponent),
  },
];

export const SERVICE_ORDERS_ROUTES: Routes = SERVICE_ORDERS_FEATURE_ENABLED
  ? ENABLED_ROUTES
  : COMING_SOON_ROUTES;
