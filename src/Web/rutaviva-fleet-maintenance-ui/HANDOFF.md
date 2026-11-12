# Handoff — `fleet-maintenance-ui`

## 1. Tareas hechas y pendientes

### Completadas y verificadas (`npm run build`, `npm test`, `npm run lint` en verde)

- **Bloque 1 — Proyecto y reglas**: `1.1`, `1.2`, `1.3`
- **Bloque 2 — Contratos de la SPA**: `2.1`, `2.2`, `2.3`, `2.4`, `2.5`
- **Bloque 3 — Core + Mock HTTP**: `3.1`, `3.2`, `3.3`, `3.4`, más el interceptor y estado semilla en `src/app/core/mocks/` con los datos de `samples/import/*.csv`
- **Bloque 4 — Shell**: `4.1`, `4.2`, `4.3`
- **Bloque 5 — Componentes compartidos**: `5.1`, `5.2`, `5.3`, `5.4`
- **Bloque 6 — Flota y mantenimiento**: `6.1`, `6.2`, `6.3`, `6.4`, `6.5`, `6.6`, `6.7`, `6.8`
- **Bloque 7 — Alertas**: `7.1`
- **Bloque 8 — Órdenes de servicio**: `8.1`, `8.2`, `8.3`, `8.4`, `8.5`
- **Bloque 9 — Parámetros y simuladores**: `9.1`, `9.2`

### Fuera de alcance (según `GEMINI.md` §7)

- `10.1`: prueba manual guiada con el sistema completo levantado (la realiza el equipo al integrar los servicios).
- `11.1` a `11.4`: tareas recortables (las rutas `/carga` y `/reportes` cuentan con vistas base cargadas perezosamente para que la navegación del rail esté completa).

---

## 2. Cómo ejecutar

1. Asegúrate de usar **Node.js 22+** (en este entorno está instalado Node `v24.15.0` en `C:\Users\Daniel_Mazo\AppData\Roaming\nvm\v24.15.0`).
2. Instala dependencias y levanta el servidor de desarrollo:
   ```bash
   cd src/Web/rutaviva-fleet-maintenance-ui
   npm install
   npm start
   ```
3. Para **apagar el mock HTTP** y consumir el Gateway real (`http://localhost:5000`), edita `src/environments/environment.ts` y cambia:
   ```ts
   useMockBackend: false
   ```
   Con `npm start`, `proxy.conf.json` redirige `/api` y `/hubs` hacia `http://localhost:5000`.

---

## 3. Decisiones tomadas que se apartan de `design.md`

- **Regla ESLint entre `fleet` y `maintenance`**: `design.md` D-04 y `tasks.md` 6.2/6.4 especifican que `FleetFacade` y `VehicleDetailPage` (en `features/fleet`) componen datos y componentes de `features/maintenance` (`MaintenanceApi`, `PlanStore`, `RoutineRowComponent`, `OdometerReadingsComponent`, `RoutineParamsDialogComponent`). Se configuró `eslint.config.mjs` para permitir explícitamente la importación unidireccional `features/fleet -> features/maintenance`, manteniendo bloqueada cualquier otra importación cruzada entre features.
- **Reglas cosméticas de Stylelint**: Se mantuvieron activas y estrictas las reglas obligatorias (`color-no-hex: true` salvo `src/styles/_tokens.scss`, prohibición de `ng-deep` y prohibición de `transition: all`), desactivando únicamente reglas cosméticas de formato de `stylelint-config-standard-scss` (`custom-property-empty-line-before`, `color-hex-length` en `_tokens.scss` y `property-no-deprecated` para `.sr-only`).

---

## 4. Pedidos al backend

Todos los contratos solicitados en `design.md` (CR-01 a CR-09 y registro de `foundation-platform`) están reflejados en `src/app/core/api/api-routes.ts` y simulados en `src/app/core/mocks/mock-backend.interceptor.ts`:

1. **CR-01 (`GET /api/v1/maintenance/plans?customerId=`)**: Resumen de planes por cliente con `worstDueStatus`, `worstConsumedPercent`, `worstTriggeredBy`, `nextRoutineName`, `nextRemainingKind` y `nextRemainingValue` para componer el listado de flota sin N+1 peticiones.
2. **CR-02 (`GET /api/v1/maintenance/plans/{vehicleId}` y `PUT /api/v1/maintenance/plans/{vehicleId}/routines/{routineId}`)**: Incluir `tracks`, `effectiveDueSoonPercent`, `thresholdSource` (`'Type' | 'Routine'`), `openServiceOrderId`, `openServiceOrderNumber`, `openServiceOrderStatus` y `canCreateOrder`.
3. **CR-03 (`POST /api/v1/maintenance/reevaluations`)**: Aceptar `{ vehicleId?: string }` opcional para reevaluar un vehículo puntual o toda la flota desde el botón «Actualizar» de la barra superior.
4. **CR-04 a CR-06 (`/api/v1/service-orders/{id}` y sub-recursos)**: Exponer `availableActions: [{ action, enabled, blockedReason }]` calculadas según estado y perfil/actor, e historial de auditoría en el detalle de la orden.
5. **CR-08 (`GET /api/v1/fleet/vehicles/by-plate/{plate}`)**: Búsqueda directa por placa para soportar la ruta `/flota/:plate`.
6. **CR-09 (`GET /api/v1/customers` y `GET /api/v1/workshops?status=Active`)**: Catálogos para poblar el selector de sesión (Cliente, Perfil y Actor) en la barra superior.

