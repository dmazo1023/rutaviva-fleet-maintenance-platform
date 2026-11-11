---
title: "[ADR-0008] Construir el CRM web con Angular 22 standalone, signals y organización por features detrás del Gateway"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "technology"
  - "angular"
  - "signals"
  - "frontend"
  - "crm"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: ""
---

> **Revisión 2026-11-10 (en estado Proposed):** se ajusta al alcance CRM, al Gateway y a Notifications (ADR-0010 y ADR-0016).

## Contexto

- La entrega debe incluir una interfaz de usuario. El equipo definió que la aplicación funcione como un **CRM**: gestión de clientes y aprobadores, talleres, vehículos y rutinas, bandeja de alertas y órdenes de servicio.
- La autenticación está fuera de alcance. Sin embargo, hay tres perfiles de uso: jefe de flota y aprobador (cliente), taller y operador del CRM.
- Todas las APIs se exponen detrás de un único Gateway, y los cambios en tiempo real llegan por un único hub SignalR (ADR-0016).
- El stack de la compañía es Angular. La versión estable vigente es Angular 22 (standalone, *signals* y *zoneless* por defecto).
- El tiempo disponible para la UI es de unas 6 a 8 horas.

## Decisión

Construiremos una SPA en **Angular 22** con **componentes standalone, *signals* y detección de cambios *zoneless***, organizada **por features alineadas con los bounded contexts**:

```
src/Web/rutaviva-fleet-maintenance-ui/src/app/
├── core/       # ApiClient tipado (base URL = Gateway), NotificationsService (SignalR), interceptor ProblemDetails, selector de perfil/cliente
├── shared/     # presentación: status-badge, money, data-table, toast, confirm-dialog
└── features/
    ├── customers/        # clientes y aprobadores
    ├── workshops/        # talleres
    ├── fleet/            # vehículos y taller asignado
    ├── maintenance/      # panel de rutinas (activar/desactivar, intervalos, % efectivo, próximo mantenimiento) + bandeja de alertas (Acknowledge) + botón «Actualizar»
    ├── administration/   # matriz de tipos de rutina (% «por vencer», intervalos por defecto) y política de aprobación automática (historial)
    ├── data-import/      # asistente de carga inicial: plantillas, subir los 5 CSV, vista previa por fila, confirmar, descargar CSV de errores (ADR-0022)
    ├── reports/          # catálogo R-01..R-04, filtros, vista previa paginada, descarga .xlsx / .csv (ADR-0023)
    ├── service-orders/   # órdenes y bandeja de aprobaciones por prioridad (aprobar/rechazar/cancelar) · vista taller: cotizar, cita (fecha y hora), En sitio, servicios adicionales, En proceso, Finalizada
    └── simulators/       # emitir lectura de odómetro y revisión por fecha (llama a los endpoints reales de Maintenance, ADR-0014 y ADR-0017)
```

- Cada feature tiene un *store* con `signal()` y `computed()` y un servicio de datos. Los componentes de página orquestan y los de presentación usan `input()` y `output()`.
- El **selector de perfil y cliente** (sin autenticación) define el grupo SignalR (`JoinCustomer`) y las acciones visibles.
- Las acciones permitidas de una orden vienen del backend (`availableActions`). Las reglas no se duplican en TypeScript.
- Los eventos de SignalR actualizan los *stores* afectados: la fila de la orden, el estado de la rutina y el contador de alertas activas.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | La demo recorre RN-1 a RN-6 y la gestión de alertas desde el navegador, sin herramientas externas (los archivos `.http` quedan como apoyo) | Positive |
| POS-002 | Hay una sola URL base y una sola conexión WebSocket (Gateway) | Positive |
| POS-003 | Las features reflejan los bounded contexts, así que un revisor encuentra la misma estructura en el front y en el back | Positive |
| NEG-001 | 6 features en unas 7 horas obligan a usar componentes de tabla y formulario genéricos y estilos mínimos | Negative |
| NEG-002 | Angular 22 exige Node.js ≥ 22 en el portátil de la demo | Negative |
| NEG-003 | Las pruebas de la UI se limitan a los *stores* y a un E2E del flujo principal | Negative |

## Alternativas consideradas

### ALT-001 — Un micro front por servicio (single-spa o Module Federation)

**Descripción**: Un front independiente por bounded context.

**Rejection rationale**: No hay equipos separados que justifiquen despliegues independientes del front (Ley de Conway). Multiplica la configuración en un plazo de unas 30 horas.

### ALT-002 — NgRx como estado global

**Descripción**: *Store* global con acciones, reducers y efectos.

**Rejection rationale**: El estado es local a cada feature. Los *signals* cubren el caso con unos 4 archivos menos por flujo. Sería un martillo dorado.

### ALT-003 — Blazor WebAssembly

**Descripción**: UI en C#.

**Rejection rationale**: No es el stack de front de la compañía, y tienta a compartir los tipos de dominio con la presentación.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Las rutas de la API y los nombres de los métodos del hub viven en `core/api-routes.ts` y `core/hub-methods.ts`, sin cadenas mágicas en los componentes |
| IMP-002 | `problemDetailsInterceptor` muestra el `title` y el `detail` de los `400`, `409` y `422` en *toasts* |
| IMP-003 | E2E con Playwright: «lectura → alerta y orden → el taller cotiza 2.000.000 → el aprobador aprueba → el taller completa → la rutina vuelve a estar al día y la alerta se resuelve», verificando las actualizaciones sin recargar |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | angular.dev: *Signals*, *Zoneless* y *Standalone components* |
| REF-002 | adr-0016-int-gateway-notifications-signalr |
| REF-003 | adr-0003-arch-service-order-state-machine: origen de `availableActions` |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0008` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
