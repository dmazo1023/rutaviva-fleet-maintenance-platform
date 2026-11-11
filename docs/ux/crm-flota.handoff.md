# Handoff de UI — RutaViva CRM · Flota, rutinas y órdenes

| Campo | Valor |
|---|---|
| Mockup aprobado | `docs/ux/crm-flota.mockup.html` · **aprobado v3** · 2026-11-10 |
| Sistema de diseño | Externo RutaViva con toques Doggie (`docs/ux/design-system/MASTER.md`) |
| Stack | Angular 22 standalone, signals, zoneless ([ADR-0008](../adr/adr-0008-tech-angular-22-frontend.md)) |
| Micro fronts | **1 SPA** (`src/Web/rutaviva-fleet-maintenance-ui`), con features por bounded context. ADR-0008 ALT-001 descarta los micro fronts |
| Historial | `docs/ux/crm-flota.changelog.md` (v1 → aprobado v3) |

## 1. Resumen de diseño

- **Trabajo principal**: que el jefe de flota sepa en segundos qué vehículo necesita mantenimiento y por qué, y que el aprobador o el taller muevan la orden sin salir de la pantalla.
- **Dirección**: técnica, precisa y en calma. La **firma** es la *ruta de la rutina*: dos pistas (km y tiempo) del último servicio al próximo, con la franja «por vencer» rayada y un punto en la posición actual.
- **Decisiones clave**:
  - D1. Estados con **semáforo** (relleno brillante + texto oscuro del mismo tono), separado del verde de marca. Descartado: usar el verde de marca para «al día» (v2), porque confundía estado y acción.
  - D2. **Lista + detalle en dos columnas** desde 1100 px; por debajo, el detalle reemplaza a la lista y tiene botón de volver. Descartado: tarjetas en cuadrícula, porque se escanean peor con 10–5.000 vehículos.
  - D3. **Microcopy mínimo**: íconos con tooltip y nombre accesible, interruptores sin etiqueta dentro de la rutina y el disparador (km o tiempo) como ícono dentro de la pastilla.
  - D4. **Acciones de la orden según `availableActions`** del backend (ADR-0003 IMP-005). La UI no duplica la tabla de transiciones; la función `actions()` del mockup solo la simula.
  - D5. **Las acciones destructivas confirman con impacto** (rechazar, cancelar, desactivar vehículo). Desactivar una rutina no confirma: se puede deshacer.
  - D6. **Placa como chip amarillo** (estilo colombiano) en JetBrains Mono, para identificar el vehículo de un vistazo.

## 2. Mapa de features (una SPA)

| Feature (`src/app/features/…`) | Ruta | Bloques del mockup | APIs (base = Gateway `#{API_BASE_URL}#`) |
|---|---|---|---|
| `shell` (core) | — | N · Navegación, T · Barra superior, E · Avisos | Hub SignalR `/hubs/notifications` (`JoinCustomer`) |
| `fleet` | `/flota`, `/flota/:plate` | A · Lista de vehículos, A0, A!, B · Detalle, B1 · Encabezado, D · Desactivar vehículo | `GET /api/v1/fleet/vehicles`, `GET /api/v1/fleet/vehicles/{id}`, `PATCH /api/v1/fleet/vehicles/{id}/status` |
| `maintenance` | (dentro de `/flota/:plate`) | B2 · Rutinas, B2 · Lecturas, C · Ajustar rutina | `GET /api/v1/maintenance/plans/{vehicleId}`, `PUT /api/v1/maintenance/plans/{vehicleId}/routines/{id}`, `PATCH …/routines/{routineId}/state`, reevaluación del botón «Actualizar» (ADR-0019) |
| `maintenance/alerts` | `/alertas` | F · Bandeja de alertas | `GET /api/v1/alerts?status=`, `POST /api/v1/alerts/{id}/acknowledge` |
| `service-orders` | `/ordenes`, `/ordenes/:id` | G · Bandeja, K · Detalle, K1 · Líneas, K2 · Acciones, K3 · Historial, L · Acción con datos, M · Confirmar | Ver §2.1 |
| `administration` | `/parametros` | H1, H2 (básico en el mockup) | `POST /api/v1/approval-policies`, `PUT /api/v1/routine-types/{id}` |
| `data-import` | `/carga` | I (básico en el mockup) | ADR-0022 IMP-003 |
| `reports` | `/reportes` | J (básico en el mockup) | ADR-0023 IMP-002 |

### 2.1 Órdenes: acción → endpoint (ADR-0003 IMP-002)

| Perfil | Acción (botón) | Estado requerido | Endpoint |
|---|---|---|---|
| Aprobador | Aprobar / Aprobar servicio | `PendingApproval` sin líneas `PendingPricing` | `POST /api/v1/service-orders/{id}/approve` |
| Aprobador | Rechazar | `PendingApproval` | `POST …/{id}/reject` |
| Aprobador | Pedir servicio | `PendingApproval` con `Reason = Quote` | `POST …/{id}/additional-services/requests` |
| Aprobador | Cancelar orden | `Created`, `PendingApproval`, `Approved`, `OnSite`, o `OnHold` con previo < `InProgress` | `POST …/{id}/cancel` |
| Taller | Cotizar | `Created` | `POST …/{id}/quote` |
| Taller | Guardar precio (en línea) | línea `PendingPricing` | `POST …/{id}/additional-services/{lineId}/price` |
| Taller | Agendar / Reprogramar cita | `Approved` | `PUT …/{id}/appointment` |
| Taller | En sitio (requiere cita) | `Approved` | `POST …/{id}/on-site` |
| Taller | Agregar servicio | `OnSite` | `POST …/{id}/additional-services` |
| Taller | Iniciar | `OnSite` | `POST …/{id}/start` |
| Taller | Finalizar (km y fecha) | `InProgress` | `POST …/{id}/complete` |
| Operador | Crear orden (desde la rutina) | rutina vencida, activa y sin orden abierta | `POST /api/v1/service-orders` |

Contratos de vista (UI):
```ts
export type DueStatus = 'UpToDate' | 'DueSoon' | 'Overdue';
export type TriggeredBy = 'Distance' | 'Time' | 'DistanceAndTime';

export interface RoutineTrackVm {
  readonly kind: 'km' | 'time';
  readonly consumedPercent: number;   // puede pasar de 100
  readonly dueSoonPercent: number;    // ancho de la franja rayada (umbral efectivo)
  readonly remaining: number;         // km o días; negativo = excedido
  readonly status: DueStatus;         // estado de esta pista
}

export interface ServiceOrderDetailVm {
  readonly id: string;
  readonly status: ServiceOrderStatus;
  readonly reason?: 'Quote' | 'AdditionalServices';
  readonly priority?: 'Normal' | 'High';
  readonly availableActions: readonly ServiceOrderAction[];   // fuente única de botones
  readonly lines: readonly ServiceLineVm[];
  readonly total: number;
  readonly autoApproved: boolean;
  readonly appointmentAt?: string;
}
```

## 3. Inventario de componentes

Sistema externo: no hay componentes `st-*`. Los de `shared/` son propios y presentacionales (`input()` / `output()`).

| Bloque (`data-card`) | Componente | Base | Notas |
|---|---|---|---|
| N · Navegación | `core/shell-rail.component` | `<nav>` + `routerLinkActive` | Rail de 88 px; barra inferior ≤ 760 px. Contadores: alertas activas (rojo) y órdenes por aprobar (ámbar, solo perfil Aprobador) |
| T · Barra superior | `core/top-bar.component` | `<select>` nativo | Cliente, perfil, indicador «En vivo» (`role="status"`), botón Actualizar |
| — | `shared/plate-chip.component` | `<span>` / `<a>` | Variantes `md` (24 px) y `lg` (38 px) |
| — | `shared/status-pill.component` | — | `status` + `trigger?`; ícono relleno + texto + íconos del disparador + `sr-only` |
| — | `shared/routine-track.component` | `role="meter"` | **Firma**. Riel, franja rayada, relleno (`transform: scaleX`), punto con borde en el tono oscuro y valor restante coloreado |
| — | `shared/mini-bar.component` | — | 6 px, decorativa (`aria-hidden`) |
| — | `shared/switch.component` | `<button role="switch">` | `aria-checked`, deshabilitado según perfil o vehículo |
| — | `shared/segmented-filter.component` | `<fieldset>` + radios | Sincronizado con query params |
| A · Lista de vehículos | `fleet/vehicle-list.component` | lista de `<a>` | Orden: vencida → por vencer → al día → inactivo, y dentro de cada grupo por % consumido. Destello al recibir un evento |
| B · Detalle | `fleet/vehicle-detail.page` | tabs ARIA | Pestañas Rutinas · Órdenes · Lecturas; `?tab=` en la URL |
| B2 · Rutinas | `maintenance/routine-row.component` | — | Ajustar (C) + interruptor de activación + enlace a la orden o «Crear orden» |
| C · Ajustar rutina | `maintenance/routine-params.dialog` | `<dialog>` | Los placeholders muestran los valores del tipo; **vista previa en vivo** con `routine-track` |
| D · Desactivar vehículo | `fleet/deactivate-vehicle.dialog` | `<dialog>` | Impacto: órdenes que pasan a pausa y rutinas que dejan de evaluarse |
| F · Alertas | `maintenance/alerts-inbox.page` | — | Activas · Atendidas · Resueltas; acción Atender |
| G · Bandeja de órdenes | `service-orders/orders-inbox.component` | — | Por aprobar · Abiertas · Cerradas · Todas. Orden: prioritaria → por aprobar → abiertas → cerradas |
| K · Detalle de orden | `service-orders/order-detail.page` | — | Stepper de 6 fases (`aria-current="step"`), aviso de prioritaria, pausa o cancelada |
| K1 · Líneas | `service-orders/service-lines.component` | — | Rutina o adicional, quién la pidió, estado y valor; precio en línea para el taller |
| K2 · Acciones | `service-orders/order-actions.component` | — | Barra fija abajo. Cancelar a la izquierda, primaria a la derecha. Motivo de bloqueo visible (`aria-describedby`) |
| K3 · Historial | `service-orders/order-history.component` | `<details>` | Plegado por defecto |
| L · Acción con datos | `service-orders/order-form.dialog` | `<dialog>` | Cotizar, Pedir/Agregar servicio, Cita, Finalizar |
| M · Confirmar | `shared/confirm-impact.dialog` | `<dialog>` | Título, lista de impacto, botón destructivo; el secundario es «Volver» |
| E · Avisos | `shared/toast-outlet.component` | `aria-live="polite"` | 6 s; acción opcional (Ver / Deshacer) |

## 4. Tokens y estilos

- Fuente única: `src/styles/_tokens.scss` con los tokens de `MASTER.md` en `:root` y `[data-theme="dark"]`. **Ningún hex fuera de ese archivo.**
- Estados (semáforo): `--c-ok/-bar/-soft`, `--c-warn/-bar/-soft`, `--c-danger/-bar/-soft`, `--c-info`, `--c-neutral`. Los componentes reciben `--fg`, `--bg` y `--bar` mediante las clases `is-ok | is-warn | is-danger | is-info | is-brand | is-neutral`.
- Regla de accesibilidad del semáforo: el relleno verde o amarillo **nunca va solo**. Siempre lo acompañan el valor en `--c-*` (texto oscuro), la pastilla con ícono y el punto con borde `--fg`.
- Tipografía: Manrope (400–800) para la interfaz y JetBrains Mono (500–600) solo para placa, VIN y n.º de orden. Números con `tabular-nums`.
- Radios: chip 5 · control 10 · panel 14 · modal 20 px. Sombra solo en modal y toast.
- Íconos: Material Symbols Rounded (fuente variable, `FILL` 0/1).
- Responsive con **container queries** sobre el contenedor de la app: 1180 / 1100 / 900 / 760 px. Revisado en 390 / 768 / 1024 / 1440.
- Movimiento: relleno 700 ms, `transform` y `opacity` únicamente, odómetro que cuenta al recibir una lectura y `prefers-reduced-motion` respetado.
- Prohibido: hex sueltos, `::ng-deep`, alturas fijas para alinear, `transition: all`.

## 5. Estados por bloque

| Bloque | Cargando | Vacío | Error | Otros |
|---|---|---|---|---|
| A · Lista | skeleton de 4 filas | `local_shipping` + «Aún no hay vehículos» + **Cargar flota** → `/carga` | `cloud_off` + causa + **Reintentar** | Sin coincidencias → **Limpiar filtros**; vehículo inactivo atenuado |
| B · Detalle | skeleton del encabezado y 2 rutinas | rutina sin intervalo: se omite su pista | toast con el `ProblemDetails` | Vehículo inactivo: aviso «Evaluaciones y órdenes en pausa» y controles deshabilitados |
| B2 · Lecturas | — | «Sin lecturas» | — | Lectura ignorada (RN-6): tachada + motivo |
| F · Alertas | skeleton | `notifications_off` + «Todo en orden» | Reintentar | — |
| G · Bandeja | skeleton | `task_alt` + «Nada por aprobar» / «Sin órdenes» | Reintentar | Prioritaria con `priority_high` |
| K · Detalle | skeleton | «Esperando cotización del taller» | toast `ProblemDetails` (409 → recargar la orden) | Pausa, cancelada, aprobación bloqueada por precio, «En sitio» sin cita |
| T · En vivo | — | — | «Reconectando…» (punto gris) | — |

## 6. Textos (constantes `*_TEXTS`)

```ts
export const FLEET_TEXTS = {
  title: 'Flota',
  searchPlaceholder: 'Placa o modelo…',
  emptyTitle: 'Aún no hay vehículos',
  emptyMessage: 'Carga tu flota desde los archivos de Excel para empezar a ver sus mantenimientos.',
  emptyAction: 'Cargar flota',
  errorTitle: 'No pudimos cargar la flota',
  errorMessage: 'El servicio de flota no respondió. Tus datos están a salvo.',
  retry: 'Reintentar',
  noMatches: 'Sin coincidencias',
  clearFilters: 'Limpiar filtros',
  inactiveBanner: 'Evaluaciones y órdenes en pausa',
  createOrder: 'Crear orden',
} as const;

export const SERVICE_ORDER_TEXTS = {
  approve: 'Aprobar', approveAdditional: 'Aprobar servicio', reject: 'Rechazar', request: 'Pedir servicio',
  cancel: 'Cancelar orden', back: 'Volver', quote: 'Cotizar', schedule: 'Agendar cita', reschedule: 'Reprogramar cita',
  onSite: 'En sitio', addService: 'Agregar servicio', start: 'Iniciar', complete: 'Finalizar',
  waitingPrice: 'Esperando precio del taller', noAppointment: 'Sin cita', vehicleStopped: 'Vehículo detenido en el taller',
  rejectQuoteImpact: ['La orden se cancela', 'Podrás crear una orden manual'],
  rejectAdditionalImpact: ['{n} servicio(s) se descartan', 'La orden sigue en sitio con lo aprobado'],
  autoApproval: 'Aprobación automática',
} as const;
```

## 7. Interacción y accesibilidad

- Enlace «Saltar al contenido». Al navegar, el foco va al `h1` o al título del detalle.
- Orden de tabulación: rail → barra superior → filtros → lista → detalle → barra de acciones.
- Pestañas con ← / →, diálogos nativos (`<dialog>`: Esc cierra y el foco queda atrapado) y foco inicial en el primer campo o en la acción de confirmar.
- Validación al enviar, no en cada tecla: foco al primer error y mensaje con causa y solución (`aria-describedby`).
- Botón ocupado (`aria-busy`) con un texto que termina en «…» durante la petición.
- `aria-live="polite"` en los avisos; `role="alert"` solo en errores.
- Botones solo con ícono: siempre `aria-label` + tooltip `data-tip`.

## 8. Criterios de aceptación de UI

- [ ] Dado un vehículo con una rutina vencida por km, cuando abro su detalle, entonces veo la pista km llena en rojo, el valor «−N km» en rojo y la pastilla «Vencida» con el ícono de velocímetro.
- [ ] Dado EVA30D (ajuste 20 %), cuando abro su rutina de aceite, entonces la franja rayada ocupa el 20 % final y el estado es «Por vencer».
- [ ] Dado el diálogo Ajustar rutina, cuando cambio el % de aviso, entonces la vista previa refleja el nuevo estado antes de guardar.
- [ ] Dado el perfil Aprobador y una orden por aprobar con una línea sin precio, entonces «Aprobar» está deshabilitado y se lee «Esperando precio del taller».
- [ ] Dado el perfil Aprobador, cuando rechazo un servicio adicional, entonces solo esa línea queda tachada y la orden vuelve a «En sitio».
- [ ] Dado el perfil Taller y una orden aprobada sin cita, entonces «En sitio» está deshabilitado.
- [ ] Dado el perfil Taller, cuando finalizo con km y fecha, entonces la rutina vuelve a «Al día» sin recargar (SignalR).
- [ ] Dado el perfil Taller, cuando cotizo hasta el monto vigente, entonces la orden queda «Aprobada» con la pastilla «Aprobación automática».
- [ ] Cuando llega una lectura por SignalR, entonces la fila destella, el odómetro cuenta y aparece un aviso si cambió el estado.
- [ ] Los filtros, la pestaña y la entidad abierta viven en la URL (deep link y botón atrás funcionan).
- [ ] Coincide con el mockup aprobado en 390 / 768 / 1024 / 1440 px, en tema claro y oscuro.
- [ ] Todos los estados de §5 están implementados.
- [ ] Teclado completo y foco visible; contraste AA en ambos temas.

## 9. Tareas sugeridas

1. `_tokens.scss`, fuentes, íconos y tema oscuro (`data-theme`).
2. Shell: rail, barra superior y salida de avisos; `NotificationsService` (SignalR) conectado al indicador «En vivo».
3. `shared/`: plate-chip, status-pill, routine-track, mini-bar, switch, segmented-filter, confirm-impact.dialog. Pruebas unitarias de `routine-track` (0 %, umbral y > 100 %).
4. Feature `fleet` + `maintenance`: lista, detalle, rutinas, ajustar parámetros con vista previa y desactivar vehículo.
5. Feature `service-orders`: bandeja, detalle, líneas, barra de acciones a partir de `availableActions`, diálogos de formulario y confirmación.
6. Feature `alerts`: bandeja y Atender.
7. Store por feature (`signal` / `computed`) actualizado por los eventos de SignalR.
8. E2E con Playwright (ADR-0008 IMP-003): lectura → orden → cotiza 2.000.000 → aprueba → completa → rutina al día, sin recargar.

## 10. Preguntas abiertas

- ¿El perfil **Operador** debe poder aprobar o cancelar en nombre del cliente? Hoy solo puede ver (ADR-0003: solo el dueño o el responsable).
- ¿Se incluye el **mapa** (última posición GPS) en el detalle del vehículo? Hoy está fuera de alcance.
- **Lecturas de odómetro**: ningún ADR define el endpoint de consulta del historial (pestaña B2 · Lecturas). Definirlo en Maintenance o quitar la pestaña.
- Ruta exacta del endpoint de reevaluación del botón «Actualizar» (ADR-0019).
- **Carga inicial** y **Reportes** quedaron básicos en el mockup: su diseño detallado (vista previa de errores por fila, filtros y vista previa de reportes) está pendiente.
