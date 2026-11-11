# RutaViva — Fleet Maintenance CRM UI

SPA en **Angular 22** (*standalone*, *zoneless*, *signals*) para la gestión de mantenimiento preventivo de flotas de RutaViva.

## Requisitos previos

- **Node.js 22 LTS o superior** (p. ej. Node 24 LTS).
- **npm 10+**.

## Comandos principales

Desde la carpeta `src/Web/rutaviva-fleet-maintenance-ui`:

```bash
npm install
npm start
```

La aplicación queda disponible en `http://localhost:4200`.

### Compilación, pruebas y linters

```bash
npm run build
npm test
npm run lint
```

## Conexión con el backend real vs. Mock HTTP

Por defecto en desarrollo (`src/environments/environment.ts`), el flag `useMockBackend` está en `true`:

```ts
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000',
  useMockBackend: true,
} as const;
```

- Con `useMockBackend: true`, `mockBackendInterceptor` (`src/app/core/mocks/`) responde con los datos de `samples/import/*.csv` (10 vehículos, 15 rutinas, 3 clientes, 5 aprobadores, 3 talleres) y mantiene SignalR desconectado sin mostrar errores.
- Para conectarse al Gateway real (`http://localhost:5000`), basta con cambiar `useMockBackend` a `false` en `src/environments/environment.ts`.

## Arquitectura

- `src/app/core/`: cliente HTTP tipado (`ApiClient`), rutas puras (`ApiRoutes`), interceptor RFC 9457 (`problemDetailsInterceptor`), sesión (`SessionStore`), notificaciones en tiempo real (`NotificationsService`), shell (`ShellRailComponent`, `TopBarComponent`, `SkipLinkComponent`), y mocks de desarrollo (`core/mocks/`).
- `src/app/shared/`: componentes presentacionales reutilizables (`PlateChipComponent`, `StatusPillComponent`, `RoutineTrackComponent`, `MiniBarComponent`, `SwitchComponent`, `SegmentedFilterComponent`, `ConfirmImpactDialogComponent`, `EmptyStateComponent`, `ErrorStateComponent`, `SkeletonComponent`, `ToastOutletComponent`) y estado `Loadable<T>`.
- `src/app/features/`: módulos de negocio cargados perezosamente (`fleet`, `maintenance`, `service-orders`, `administration`, `simulators`, `data-import`, `reports`).
