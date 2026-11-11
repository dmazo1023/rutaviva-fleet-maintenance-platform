import { Routes } from '@angular/router';

export const SERVICE_ORDERS_ROUTES: Routes = [
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

