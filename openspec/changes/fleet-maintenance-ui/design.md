## Context

Motivación en `proposal.md`. El diseño visual está aprobado y congelado (`mockups/crm-flota.mockup.html` v3, `mockups/crm-flota.handoff.md`, `mockups/design-system/MASTER.md`). Este documento define cómo se estructura la SPA para que el código refleje los mismos bounded contexts del backend y no duplique reglas de negocio.

Insumos que se consumen sin redefinir:
- Endpoints: `openspec/changes/foundation-platform/design.md#endpoints-públicos` (rutas normalizadas D-09; las rutas del handoff §2 quedan reemplazadas, p. ej. `/api/v1/alerts` → `/api/v1/maintenance/alerts`).
- Errores: `ProblemDetails` con extensión `code` (`foundation-platform`, spec `api-conventions`).
- Hub `/hubs/notifications`: **dueño `edge-gateway-notifications`**. Los nombres de métodos usados aquí vienen de ADR-0016 IMP-002 y se ajustarán a lo que ese change publique.

## Goals / Non-Goals

**Goals:**
- Cada *feature* encapsula su estado (*store* con `signal`/`computed`) y su acceso a datos; los componentes de página orquestan y los presentacionales solo usan `input()`/`output()`.
- Cero reglas de negocio en TypeScript: acciones de la orden desde `availableActions`, estados y porcentajes calculados por el backend.
- Una sola fuente para rutas, métodos del hub, textos, etiquetas de enums y colores.

**Non-Goals:**
- Librería de componentes reutilizable fuera de RutaViva.
- Estado global tipo NgRx (KISS: *signals* por *feature* bastan).
- *Server-side rendering*.

## Decisions

### D-01 Estructura de carpetas

```
src/Web/rutaviva-fleet-maintenance-ui/
  src/
    styles/_tokens.scss, _typography.scss, styles.scss
    app/
      app.config.ts            provideZonelessChangeDetection, provideRouter(withComponentInputBinding), provideHttpClient(withInterceptors)
      app.routes.ts            lazy loadChildren por feature
      core/
        api/api-routes.ts      funciones puras que construyen cada URL (ApiRoutes.fleet.vehicles(), ApiRoutes.serviceOrders.approve(id))
        api/api-client.ts      envoltorio tipado de HttpClient (get/post/put/patch/download)
        api/problem-details.interceptor.ts
        realtime/hub-methods.ts       nombres de métodos cliente→servidor y servidor→cliente
        realtime/notifications.service.ts
        realtime/realtime-events.ts   tipos de los payloads del hub
        context/session.store.ts      cliente, perfil, actor (approverId | workshopId)
        models/enums.ts               tipos literales espejo (data-model §5)
        models/enum-labels.ts         mapa único de etiquetas en español
        shell/shell-rail.component.ts, top-bar.component.ts, skip-link.component.ts
        feedback/toast.store.ts
      shared/
        plate-chip, status-pill, routine-track, mini-bar, switch, segmented-filter,
        confirm-impact.dialog, toast-outlet, empty-state, error-state, skeleton
      features/
        fleet/          data/fleet.api.ts, state/fleet.store.ts, fleet.facade.ts, pages/, components/, fleet.texts.ts, fleet.routes.ts
        maintenance/    data/maintenance.api.ts, state/plan.store.ts, state/alerts.store.ts, components/routine-row, dialogs/routine-params.dialog, pages/alerts-inbox.page, maintenance.texts.ts
        service-orders/ data/service-orders.api.ts, state/service-orders.store.ts, pages/orders-inbox.page, pages/order-detail.page, components/service-lines, order-actions, order-history, order-stepper, dialogs/order-form.dialog, service-orders.texts.ts
        administration/ data/, state/, pages/parameters.page
        data-import/    data/, state/, pages/import-wizard.page
        reports/        data/, state/, pages/reports.page
        simulators/     data/, pages/simulators.page
  e2e/                  Playwright
```

Regla de dependencia: `features/*` → `shared` y `core`; `shared` → nada de `features` ni de `core/api`; una *feature* no importa otra *feature* (excepto rutas). Se verifica con una regla de lint de imports restringidos (`no-restricted-imports` por patrón).

### D-02 Adapter de API y mapeo a VMs

Cada *feature* tiene un `*.api.ts` (**Adapter**) que llama al `ApiClient` con `ApiRoutes` y convierte los DTOs del backend en VMs inmutables (`readonly`). Los componentes nunca ven DTOs. El mapeo es puro y se prueba aislado.

Contratos de vista principales (además de los del handoff §2):

```ts
export interface VehicleListItemVm {
  readonly vehicleId: string; readonly plate: string; readonly model: string;
  readonly status: VehicleStatus; readonly worstDueStatus: DueStatus | null;
  readonly worstConsumedPercent: number; readonly odometerKm: number;
}
export interface RoutineVm {
  readonly routineId: string; readonly name: string; readonly typeCode: string;
  readonly state: RoutineState; readonly dueStatus: DueStatus; readonly triggeredBy: TriggeredBy | null;
  readonly tracks: readonly RoutineTrackVm[];          // handoff §2
  readonly nextDueKm: number | null; readonly nextDueDate: string | null;
  readonly effectiveDueSoonPercent: number; readonly thresholdSource: 'Type' | 'Routine';
  readonly openServiceOrderId: string | null; readonly canCreateOrder: boolean;
}
export interface OdometerReadingVm {
  readonly kilometers: number; readonly readAt: string; readonly outcome: OdometerReadingOutcome;
  readonly ignoredReason: OdometerReadingIgnoredReason | null;
}
export interface AlertVm { readonly alertId: string; readonly plate: string; readonly routineName: string;
  readonly type: AlertType; readonly triggeredBy: TriggeredBy; readonly status: AlertStatus;
  readonly raisedAt: string; readonly resolutionReason: AlertResolutionReason | null; }
export interface ServiceOrderListItemVm { readonly id: string; readonly number: string; readonly plate: string;
  readonly routineName: string; readonly status: ServiceOrderStatus; readonly priority: ApprovalPriority | null;
  readonly total: number | null; readonly updatedAt: string; }
export interface ServiceOrderActionVm { readonly action: ServiceOrderAction; readonly enabled: boolean;
  readonly blockedReason: ServiceOrderBlockedReason | null; }
```

`ServiceOrderDetailVm.availableActions` del handoff se representa como `readonly ServiceOrderActionVm[]` para soportar «visible pero bloqueada con motivo» (handoff §8: Aprobar deshabilitado, En sitio sin cita).

### D-03 Stores por *feature* (signals)

Patrón: `@Injectable()` provisto en la ruta de la *feature* (se destruye al salir). Estado privado `signal<FeatureState>()`, selectores `computed()`, métodos de comando que llaman al Adapter y actualizan el estado de forma inmutable. Estado de carga explícito por recurso: `{ status: 'idle' | 'loading' | 'ready' | 'error'; data; error }` (handoff §5 exige los 4 estados).

`SessionStore` (core, raíz): cliente, perfil y actor. Persiste la última elección en `localStorage` con `try/catch` (conveniencia por visor).

### D-04 Facade por *feature*

Cada *feature* expone un **Facade** (`FleetFacade`, `ServiceOrdersFacade`…) que es la única API que ven las páginas: combina store + Adapter + `ToastStore` + navegación. La composición entre servicios ocurre aquí; por ejemplo `FleetFacade.loadList()` combina los vehículos de Fleet con el resumen de planes de Maintenance por `vehicleId` (el Gateway no agrega respuestas, ADR-0016).

### D-05 Container / Presentational

- Páginas (`*.page.ts`): leen parámetros de ruta con `withComponentInputBinding`, llaman al Facade y pasan *signals* a los hijos.
- Presentacionales (`shared/*`, `features/*/components/*`): `ChangeDetectionStrategy.OnPush`, solo `input()`/`output()`, sin inyección de servicios de datos. Se prueban con entradas.

### D-06 Tiempo real: mapa evento → store

`NotificationsService` (raíz) mantiene una conexión `HubConnectionBuilder().withUrl('/hubs/notifications').withAutomaticReconnect()`, expone `connectionState: Signal<'connected' | 'reconnecting' | 'disconnected'>` y un `Observable`/`signal` por evento tipado. Al cambiar el cliente invoca `JoinCustomer`/`LeaveCustomer`; con perfil Aprobador también `JoinApprover`. Al pasar de `reconnecting` a `connected` emite `reconnected`, y cada Facade activo recarga su vista (spec `crm-shell`).

| Método del hub (servidor → cliente) | Stores que reaccionan | Efecto |
|---|---|---|
| `RoutineChanged` | `FleetStore`, `PlanStore` | Actualiza la rutina y el peor estado del vehículo; destello y aviso si cambió `dueStatus` |
| `OdometerReadingRecorded` *(requerido, ver abajo)* | `FleetStore`, `PlanStore` | Odómetro con conteo animado; agrega la fila a Lecturas |
| `VehicleChanged` *(requerido)* | `FleetStore`, `PlanStore` | Estado del vehículo / plan suspendido |
| `AlertChanged` | `AlertsStore`, `ShellCounters` | Alta, atención o resolución; contador del rail |
| `ServiceOrderChanged` | `ServiceOrdersStore`, `PlanStore` | Fila y detalle abiertos; enlace de la rutina a su orden |
| `ApprovalRequested` | `ServiceOrdersStore`, `ToastStore`, `ShellCounters` | Contador por aprobar; `High` → aviso persistente con «Ver» |

Política de aplicación: el evento trae los datos de la vista (DTOs propios de Notifications, ADR-0016 IMP-002) y se aplica con *upsert* por ID; si el evento no basta para la vista abierta (por ejemplo, historial de la orden), el store recarga ese recurso por REST. Esto evita duplicar lógica de cálculo en el cliente.

### D-07 Acciones de la orden

`order-actions.component` recibe `ServiceOrderActionVm[]` y el perfil, y solo resuelve presentación: etiqueta (`SERVICE_ORDER_TEXTS`), posición (Cancelar a la izquierda), si abre diálogo de datos (`ACTION_FORMS: Record<ServiceOrderAction, OrderFormKind | null>`) o de confirmación (`ACTION_IMPACT`), y el endpoint (`ApiRoutes.serviceOrders[action]`). Son tablas de presentación, no de transiciones: el backend decide qué acción existe y si está habilitada.

### D-08 Diálogos y formularios

`<dialog>` nativo con `showModal()` (Escape y foco atrapado nativos). Formularios con *Reactive Forms* tipados, validación al enviar, foco al primer error y `aria-describedby`. Las validaciones del cliente son de forma (requeridos, rangos); las de negocio vienen del `422`.

### D-09 Tokens y estilos

`_tokens.scss` es el único archivo con colores (claro y `[data-theme="dark"]`). Los componentes usan clases de tono `is-ok | is-warn | is-danger | is-info | is-brand | is-neutral` que fijan `--fg`, `--bg`, `--bar`. *Container queries* en el contenedor de la app (1180/1100/900/760). Una regla de Stylelint (`color-no-hex` con excepción del archivo de tokens) y la prohibición de `::ng-deep` y `transition: all` se verifican en `npm run lint`.

### D-10 Pruebas

- Unitarias con el *runner* por defecto de Angular 22 (Vitest): mapeos de Adapter, stores (comandos y reacción a eventos con un `NotificationsService` falso), `routine-track` (0 %, umbral, > 100 %), `order-actions` (deshabilitado con motivo) y `problemDetailsInterceptor`.
- E2E con Playwright contra el sistema levantado (ADR-0008 IMP-003). **(Recortable)**

## Contratos nuevos requeridos

Faltan en el registro de `foundation-platform` o en ADRs y los necesita esta UI. Se piden a los changes dueños:

| # | Contrato | Change dueño | Motivo |
|---|---|---|---|
| CR-01 | `GET /api/v1/maintenance/plans?customerId=` → resumen por vehículo (`vehicleId`, `plate`, `status`, `odometerKm`, `worstDueStatus`, `worstConsumedPercent`) | `maintenance-core` | Ordenar y pintar la lista de flota sin N+1 llamadas |
| CR-02 | `GET /api/v1/maintenance/plans/{vehicleId}` incluye por rutina: `tracks` (km/tiempo con `consumedPercent`, `remaining`), `effectiveDueSoonPercent`, `thresholdSource`, `triggeredBy` | `maintenance-core` | La pista no calcula porcentajes en el cliente |
| CR-03 | `GET /api/v1/maintenance/alerts?customerId=&status=` (filtro por cliente) | `maintenance-core` | La bandeja se filtra por el cliente activo |
| CR-04 | `GET /api/v1/service-orders?customerId=&vehicleId=&routineId=&statusGroup=PendingApproval\|Open\|Closed` | `service-orders-core` | Bandeja, pestaña Órdenes del vehículo y enlace rutina → orden abierta |
| CR-05 | `GET /api/v1/service-orders/{id}?actor=Operator\|Approver\|Workshop&actorId=` con `availableActions: [{ action, enabled, blockedReason }]` y `history[]` | `service-orders-core` | Acciones por perfil con motivo de bloqueo (ADR-0003 IMP-005) |
| CR-06 | Body de `approve`/`reject`/`cancel`/`additional-services/requests` con `approverId`; body de acciones del taller con `workshopId` | `service-orders-core` | Autorización dueño o responsable sin autenticación (ADR-0003 §5) |
| CR-07 | Hub: `JoinCustomer(customerId)`, `LeaveCustomer(customerId)`, `JoinApprover(approverId)`; servidor → cliente además de ADR-0016 IMP-002: `OdometerReadingRecorded` y `VehicleChanged` (o equivalentes) | `edge-gateway-notifications` | Pestaña Lecturas y vehículo inactivo en vivo (ADR-0025, ADR-0020) |
| CR-08 | `POST /api/v1/maintenance/reevaluations { vehicleId? }` devuelve el mismo resumen que `time-evaluations` | `maintenance-core` | Aviso tras «Actualizar» |
| CR-09 | `GET /api/v1/workshops?status=Active` y `GET /api/v1/customers/{id}` con sus aprobadores | `supporting-contexts` | Selector de actor del perfil Taller y Aprobador |

## Patrones aplicados

| Patrón | Dónde | Por qué |
|---|---|---|
| Container / Presentational | D-05 | Presentacionales reutilizables y probables sin dobles de servicios |
| Facade | D-04 | Las páginas no conocen stores, Adapters ni composición entre servicios |
| Adapter | D-02 | DTOs del backend aislados; un cambio de contrato toca un archivo |
| Observer | D-06 | Eventos del hub a stores sin acoplar el servicio de tiempo real a las *features* |
| Strategy (tablas de presentación) | D-07 | Acción → diálogo/impacto/endpoint sin `switch` |

## Antipatrones evitados

| Antipatrón | Cómo se evita |
|---|---|
| Reglas de transición duplicadas en TS | `availableActions` del backend; D-07 solo mapea presentación |
| Números y cadenas mágicas | `ApiRoutes`, `HubMethods`, `*_TEXTS`, `ENUM_LABELS`, constantes de tiempos (`TOAST_DURATION_MS = 6000`) |
| Hex sueltos, `::ng-deep`, `transition: all` | Tokens únicos y reglas de Stylelint (D-09) |
| Objeto dios (store global) | Store por *feature* destruido al salir de la ruta |
| Componentes anémicos con lógica en plantillas | `computed()` en el store o en el componente, no expresiones complejas en HTML |
| Copy-paste de estados de carga | `empty-state`, `error-state`, `skeleton` y el tipo `Loadable<T>` compartidos |

## Risks / Trade-offs

- [Los contratos CR-01 a CR-09 no se implementan a tiempo en el backend] → El Adapter aísla el cambio; mientras tanto la *feature* puede usar datos simulados en pruebas unitarias. Se reporta al orquestador antes de la ola de UI.
- [Angular 22 cambia el *runner* de pruebas o la API de *zoneless* respecto a lo esperado] → La Ola 0 del harness crea un componente de humo con `templateUrl` e `input()` y su prueba antes de lanzar agentes.
- [Eventos en vivo que llegan antes de la respuesta REST] → *Upsert* por ID con `updatedAt`; se descarta el más antiguo.
- [Tiempo de entrega] → Recorte en el orden del análisis §10; data-import y reports quedan básicos.

## Open Questions

- ¿El perfil Operador puede aprobar o cancelar en nombre del cliente? (handoff §10). Hoy no: solo ve. No cambia la estructura, solo la tabla de visibilidad.
