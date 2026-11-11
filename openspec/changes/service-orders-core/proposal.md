## Why

ServiceOrders es uno de los dos *core domains* de RutaViva (ADR-0010). Concentra RN-3 (una orden por vencimiento y máximo una abierta por vehículo y rutina), RN-4 (flujo, aprobación por umbral, rechazo y cancelación) y la mitad productora de RN-5 (completar con km y fecha reales). Sin este servicio, el vencimiento detectado por Maintenance no se convierte en trabajo para el taller ni en una decisión de costos para el cliente. Es la parte del reto donde más se luce el diseño: una máquina de 9 estados con 16 acciones, tres actores y garantías de unicidad bajo entrega *at-least-once*.

Corresponde al corte **S2** del análisis (§10). Depende de `foundation-platform` y consume los eventos de `maintenance-core` y `supporting-contexts`.

## What Changes

- Servicio `RutaViva.ServiceOrders` (Domain, Application, Infrastructure, Api) con schema `serviceorders` y usuario propio (ADR-0013).
- Agregado `ServiceOrder` con máquina de estados explícita: tabla declarativa de transiciones y una única guarda (ADR-0003). Estados `Created`, `Quoted` (transitorio, A-14), `PendingApproval`, `Approved`, `OnSite`, `InProgress`, `OnHold`, `Completed`, `Cancelled`.
- Líneas de servicio (rutina y adicionales), solicitud de aprobación con motivo y prioridad, `Money`, `ServiceRecord` e historial de cambios.
- Aprobación automática por **política versionada de solo inserción** (`ApprovalPolicy`), editable desde la web y copiada en la orden al cotizar (A-06, ADR-0019).
- Autorización de aprobación y cancelación: aprobador activo del dueño **o** responsable del vehículo (A-11). El taller no cancela (A-21).
- Creación **automática** al consumir `MaintenanceRoutineOverdue` y **manual** desde el CRM tras una cancelación (A-07), con RN-3 garantizada en tres niveles (ADR-0011).
- Pausa y reanudación de órdenes abiertas por el estado del vehículo, con `StatusBeforeHold` (ADR-0020, A-18, A-19).
- Réplicas locales `VehicleReference`, `ApproverReference` y `RoutineReference` alimentadas por los streams `fleet.vehicle`, `customers.customer` y `maintenance.routine`.
- Consultas para el CRM: listado, detalle con `availableActions` por perfil (ADR-0003 IMP-005), bandeja de aprobaciones priorizada (IMP-003) e historial de políticas.
- Número legible `OS-000123` desde una `SEQUENCE` (data-model D-12).
- **(Recortable)** Cita con el taller (`WorkshopAppointment`, ADR-0021, A-20).
- **(Recortable)** Servicios adicionales: pedidos por el aprobador y cotizados por el taller (A-10), o agregados por el taller en sitio con aprobación prioritaria (A-15).

## Capabilities

### New Capabilities

- `service-order-lifecycle`: estados, transiciones permitidas por actor, cotización, inicio, finalización, cancelación, pausa y reanudación de la orden, historial y acciones disponibles.
- `service-order-approval`: aprobación y rechazo de cotizaciones y servicios adicionales, autorización del aprobador, bandeja priorizada y bloqueo por líneas sin precio.
- `service-order-creation`: creación automática por vencimiento y manual desde el CRM, unicidad de la orden abierta y réplicas de datos de otros contextos.
- `approval-policy`: monto de aprobación automática versionado, consultable y editable desde la web con efecto en la siguiente cotización.
- `workshop-appointment`: registro y reprogramación de la cita con el taller como precondición de «En sitio» (Recortable).

### Modified Capabilities

(ninguna)

## Impact

- Código nuevo: `src/Services/ServiceOrders/RutaViva.ServiceOrders.{Domain,Application,Infrastructure,Api}` y `tests/Services/ServiceOrders/RutaViva.ServiceOrders.{Domain.UnitTests,Application.UnitTests,IntegrationTests}`.
- Base de datos: tablas `serviceorders.ServiceOrders`, `ServiceOrderLines`, `ServiceOrderHistory`, `ApprovalPolicies`, `VehicleReferences`, `ApproverReferences`, `RoutineReferences`, `OutboxMessages`, `EventCheckpoints` y la `SEQUENCE` `ServiceOrderNumbers`, por migraciones EF Core.
- Eventos publicados en `serviceorders.service-order`: `ServiceOrderCreated`, `ServiceOrderStatusChanged`, `ServiceOrderApprovalRequested`, `ServiceOrderLinesChanged`, `ServiceOrderAppointmentScheduled`, `ServiceOrderCompleted` (registro de contratos de `foundation-platform`).
- Consumidores: Maintenance (`ServiceOrderCompleted` → RN-5), Notifications y Reporting.
- APIs: todos los endpoints de ServiceOrders del registro de contratos, usados por el CRM (perfiles Aprobador, Taller y Operador).

## Fuera de alcance

- Elegir o agendar el taller: la orden usa el taller asignado al vehículo (enunciado). Disponibilidad, cupos, confirmación de la cita e inasistencias (ADR-0021).
- Varias cotizaciones por orden (A-10: una sola, con líneas).
- Pagos, facturación y envío real de avisos (basta con publicar el evento).
- Lo que ocurre después de cancelar una orden (enunciado); solo se habilita la creación manual (A-07).
- Autenticación: el actor (aprobador o taller) viaja en el request y se valida contra las réplicas.

**Recortable** (orden del análisis §10, en este orden): cita del taller (A-20) y luego servicios adicionales (A-10, A-15). Sin la cita, «En sitio» no exige cita; sin servicios adicionales, la orden pasa de `OnSite` a `InProgress` directamente y la bandeja solo tiene prioridad `Normal`.
