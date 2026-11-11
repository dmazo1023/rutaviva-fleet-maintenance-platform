## Why

El reto exige una interfaz de usuario con su backend. Los servicios de RutaViva exponen reglas ricas (RN-1 a RN-6, 9 estados de la orden, alertas, parámetros editables y carga inicial), pero sin el CRM el jefe de flota, el aprobador y el taller no pueden operarlas ni ver su efecto en tiempo real. El mockup aprobado v3 (`docs/ux/crm-flota.handoff.md`) fija el diseño; este change lo convierte en una SPA mantenible que **no duplica reglas de negocio**: las acciones de la orden llegan del backend (`availableActions`, ADR-0003 IMP-005) y los cambios llegan por SignalR (ADR-0016).

Corresponde al corte **S5** del análisis (§10).

## What Changes

- SPA **Angular 22** `src/Web/rutaviva-fleet-maintenance-ui` con componentes standalone, *signals* y detección de cambios *zoneless* (ADR-0008), organizada por *features* alineadas con los bounded contexts.
- `core/`: cliente de API tipado contra el Gateway, `api-routes.ts` y `hub-methods.ts` (sin cadenas mágicas, ADR-0008 IMP-001), `problemDetailsInterceptor` (ADR-0008 IMP-002), `NotificationsService` con reconexión automática y recarga por REST (ADR-0016 IMP-003), selector de cliente y perfil (Operador, Aprobador, Taller), espejos de enums como tipos literales y mapa único de etiquetas en español (data-model E-05).
- Shell: rail de navegación con contadores, barra superior con indicador «En vivo» y botón «Actualizar» (ADR-0019, `POST /api/v1/maintenance/reevaluations`), salida de avisos.
- `shared/`: componentes presentacionales del handoff §3, incluida la **firma** `routine-track`.
- *Features*: `fleet` (lista, detalle, desactivar vehículo), `maintenance` (rutinas, ajustar parámetros con vista previa, activar/desactivar, lecturas de odómetro de ADR-0025, alertas), `service-orders` (bandeja priorizada, detalle, líneas, acciones desde `availableActions`, diálogos de datos y de confirmación con impacto), `administration` (matriz de tipos de rutina y política de aprobación automática), `data-import`, `reports` y `simulators` (lectura de odómetro y revisión por fecha contra los endpoints reales de Maintenance, ADR-0014 y ADR-0017).
- Tokens de diseño en `src/styles/_tokens.scss` a partir de `docs/ux/design-system/MASTER.md`, tema claro y oscuro.
- Pruebas unitarias con el *runner* por defecto de Angular 22 (Vitest) y E2E con Playwright (ADR-0008 IMP-003) **(Recortable)**.

## Capabilities

### New Capabilities

- `crm-shell`: navegación, contexto de cliente y perfil, conexión en vivo, actualización manual, avisos y manejo uniforme de errores.
- `crm-fleet-maintenance`: lista y detalle de vehículos, rutinas con su pista de km y tiempo, ajuste de parámetros, activación, desactivación del vehículo, creación manual de orden y lecturas de odómetro.
- `crm-alerts`: bandeja de alertas por estado y acción «Atender».
- `crm-service-orders`: bandeja priorizada, detalle con fases, líneas e historial, y acciones por perfil guiadas por el backend.
- `crm-administration`: edición de la matriz de tipos de rutina y de la política de aprobación automática con su historial.
- `crm-data-import`: asistente de carga inicial con vista previa por fila, confirmación y CSV de errores (versión básica).
- `crm-reports`: catálogo de reportes, filtros, vista previa paginada y descarga (versión básica).
- `crm-simulators`: emisión de lecturas de odómetro y revisión por fecha para la demo.

### Modified Capabilities

(ninguna)

## Impact

- Código nuevo: `src/Web/rutaviva-fleet-maintenance-ui/**`.
- Consume las APIs públicas del registro de contratos de `foundation-platform` (`design.md#endpoints-públicos`) a través del Gateway y el hub `/hubs/notifications` de `edge-gateway-notifications`.
- Depende de: `foundation-platform`, `maintenance-core`, `service-orders-core`, `supporting-contexts`, `edge-gateway-notifications`; y, para sus vistas, de `data-import` y `reporting`.
- Dependencias npm: `@angular/*` 22, `@microsoft/signalr`, `@playwright/test` (dev). Fuentes Manrope, JetBrains Mono y Material Symbols Rounded.

## Fuera de alcance

- Autenticación (fuera del reto): el perfil y el cliente se eligen en la barra superior.
- Micro fronts (ADR-0008 ALT-001): una sola SPA.
- Mapa con la última posición GPS (handoff §10).
- Reglas de negocio en TypeScript: la tabla de transiciones, el umbral de aprobación y la evaluación de vencimientos viven en el backend.

**Recortable** (orden del análisis §10): estilos finos de la UI (animaciones, tema oscuro, *container queries* más allá de 760 px), diseño detallado de `crm-data-import` y `crm-reports` (quedan básicos, como en el mockup), formulario de cita del taller (si se recorta A-20 en el backend), acciones de servicios adicionales (si se recorta A-10) y E2E con Playwright.
