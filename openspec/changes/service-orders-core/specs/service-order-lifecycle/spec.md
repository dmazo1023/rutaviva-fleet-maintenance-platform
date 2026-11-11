## Purpose

Gobernar el ciclo de vida de una orden de servicio desde su creación hasta que se completa o se cancela, permitiendo a cada actor (taller, aprobador o sistema) solo las acciones válidas para el estado actual (RN-4, ADR-0003).

## ADDED Requirements

### Requirement: Estados y transiciones permitidas
La orden SHALL tener exactamente uno de los estados `Created`, `Quoted`, `PendingApproval`, `Approved`, `OnSite`, `InProgress`, `OnHold`, `Completed` o `Cancelled`, y SHALL aceptar solo las acciones de la tabla siguiente. Cualquier otra combinación MUST rechazarse con `422` y `code = "ServiceOrder.InvalidTransition"` sin cambiar la orden.

| Estado | Acciones permitidas |
|---|---|
| `Created` | `Quote`, `Cancel`, `Hold` |
| `Quoted` | `AutoApprove`, `RequestApproval` (solo el sistema, en la misma operación de `Quote`) |
| `PendingApproval` | `Approve`, `Reject`, `RequestAdditionalService`, `PriceAdditionalService`, `Cancel`, `Hold` |
| `Approved` | `ScheduleAppointment`, `RescheduleAppointment`, `MarkOnSite`, `Cancel`, `Hold` |
| `OnSite` | `AddAdditionalService`, `Start`, `Cancel`, `Hold` |
| `InProgress` | `Complete`, `Hold` |
| `OnHold` | `Resume`, `Cancel` |
| `Completed` | ninguna |
| `Cancelled` | ninguna |

#### Scenario: Todas las combinaciones de estado y acción
- **WHEN** se ejecuta cada una de las 16 acciones sobre una orden en cada uno de los 9 estados
- **THEN** de las 144 combinaciones, solo las que la tabla permite cambian la orden, y el resto devuelve `ServiceOrder.InvalidTransition`

#### Scenario: Iniciar una orden aprobada sin pasar por En sitio
- **WHEN** el taller pide `POST /api/v1/service-orders/{id}/start` sobre una orden `Approved`
- **THEN** la respuesta es `422` con `code = "ServiceOrder.InvalidTransition"` y la orden sigue `Approved`

#### Scenario: Orden finalizada es terminal
- **WHEN** cualquier actor intenta una acción sobre una orden `Completed`
- **THEN** la respuesta es `422` y la orden no cambia

### Requirement: Acciones restringidas por actor
Cada acción SHALL pertenecer a un único actor: el taller ejecuta `Quote`, `PriceAdditionalService`, `ScheduleAppointment`, `RescheduleAppointment`, `MarkOnSite`, `AddAdditionalService`, `Start` y `Complete`; el aprobador ejecuta `Approve`, `Reject`, `RequestAdditionalService` y `Cancel`; el sistema ejecuta `AutoApprove`, `RequestApproval`, `Hold` y `Resume`. Una acción del taller MUST provenir del taller asignado a la orden; si no, se rechaza con `422` y `code = "ServiceOrder.WorkshopNotAssigned"`.

#### Scenario: El taller no puede cancelar
- **WHEN** el taller asignado intenta cancelar una orden `Approved`
- **THEN** la operación se rechaza y la orden sigue `Approved` (A-21)

#### Scenario: Otro taller intenta cotizar
- **WHEN** un taller distinto del asignado cotiza una orden `Created`
- **THEN** la respuesta es `422` con `code = "ServiceOrder.WorkshopNotAssigned"`

### Requirement: Cotización con líneas y aprobación automática por umbral
El taller SHALL cotizar una orden `Created` con al menos una línea de rutina con valor mayor que 0 en COP. La orden SHALL registrar `Quoted` en su historial y, en la misma operación, pasar a `Approved` con `autoApproved = true` si el total es menor o igual al monto de la política vigente, o a `PendingApproval` con motivo `Quote` y prioridad `Normal` si es mayor. La orden MUST guardar una copia del monto de la política aplicada.

#### Scenario: Cotización igual al umbral
- **WHEN** la política vigente es 1.500.000 COP y el taller cotiza 1.500.000 COP
- **THEN** la orden queda `Approved` con `autoApproved = true` y el historial contiene `Created → Quoted` y `Quoted → Approved`

#### Scenario: Cotización un peso por encima del umbral
- **WHEN** la política vigente es 1.500.000 COP y el taller cotiza 1.500.001 COP
- **THEN** la orden queda `PendingApproval` con `approvalReason = Quote` y `approvalPriority = Normal`, y se publica `ServiceOrderApprovalRequested`

#### Scenario: Cotización vacía o con valor cero
- **WHEN** el taller cotiza sin líneas o con una línea de valor 0
- **THEN** la respuesta es `400` y la orden sigue `Created`

### Requirement: Inicio y finalización con el registro real del servicio
El taller SHALL iniciar una orden `OnSite` (pasa a `InProgress`) y SHALL completar una orden `InProgress` reportando el kilometraje (≥ 0) y la fecha del servicio (no futura). Al completarse, la orden MUST publicar `ServiceOrderCompleted` con `ServicedKm` y `ServicedAt` (RN-5, A-08).

#### Scenario: Finalización válida
- **WHEN** el taller completa una orden `InProgress` con 55.200 km y la fecha de hoy
- **THEN** la orden queda `Completed` y se publica `ServiceOrderCompleted` con `ServicedKm = 55200`

#### Scenario: Fecha de servicio futura
- **WHEN** el taller completa con una fecha posterior a hoy
- **THEN** la respuesta es `400` con `code = "ServiceOrder.InvalidServiceRecord"` y la orden sigue `InProgress`

### Requirement: Cancelación por el dueño o el responsable antes de En proceso
Un aprobador autorizado (ver `service-order-approval`) SHALL poder cancelar una orden en `Created`, `PendingApproval`, `Approved` u `OnSite`, o en `OnHold` si su estado anterior a la pausa era uno de esos cuatro. La orden cancelada MUST guardar `cancellationReason = CancelledByCustomer`.

#### Scenario: Cancelar en sitio
- **WHEN** el responsable del vehículo cancela una orden `OnSite`
- **THEN** la orden queda `Cancelled` con `cancellationReason = CancelledByCustomer`

#### Scenario: Cancelar en proceso
- **WHEN** un aprobador del dueño intenta cancelar una orden `InProgress`
- **THEN** la respuesta es `422` con `code = "ServiceOrder.InvalidTransition"`

#### Scenario: Cancelar en pausa con estado previo En proceso
- **WHEN** un aprobador intenta cancelar una orden `OnHold` cuyo estado previo era `InProgress`
- **THEN** la respuesta es `422` y la orden sigue `OnHold` (A-19)

#### Scenario: Cancelar en pausa con estado previo Aprobada
- **WHEN** un aprobador cancela una orden `OnHold` cuyo estado previo era `Approved`
- **THEN** la orden queda `Cancelled`

### Requirement: Pausa y reanudación por el estado del vehículo
Cuando el vehículo pasa a `Inactive`, todas sus órdenes abiertas SHALL pasar a `OnHold` guardando su estado anterior; cuando vuelve a `Active`, SHALL volver exactamente a ese estado. Pausar una orden ya pausada o reanudar una que no está pausada MUST no tener efectos ni publicar eventos. Desactivar una **rutina** MUST NOT cambiar el estado de su orden (A-17).

#### Scenario: Vehículo desactivado con dos órdenes abiertas
- **WHEN** llega `VehicleStatusChanged` con `Status = Inactive` para un vehículo con una orden `Approved` y otra `InProgress`
- **THEN** ambas quedan `OnHold`, con estado previo `Approved` e `InProgress` respectivamente, y se publican 2 `ServiceOrderStatusChanged`

#### Scenario: Vehículo reactivado
- **WHEN** llega `VehicleStatusChanged` con `Status = Active` para ese vehículo
- **THEN** las órdenes vuelven a `Approved` e `InProgress` y su estado previo queda vacío

#### Scenario: Evento de desactivación entregado dos veces
- **WHEN** el mismo cambio a `Inactive` se procesa por segunda vez
- **THEN** no se publican eventos nuevos y las órdenes siguen `OnHold` con el mismo estado previo

#### Scenario: Desactivar la rutina no afecta la orden
- **WHEN** llega `MaintenanceRoutineStateChanged` con la rutina `Inactive` para una rutina con una orden `Approved`
- **THEN** la orden sigue `Approved`

### Requirement: Historial de cambios y evento por transición
Cada transición, registro o reprogramación de cita y servicio adicional agregado SHALL añadir una entrada al historial de la orden con tipo (`StatusChanged`, `AppointmentScheduled`, `AppointmentRescheduled` o `AdditionalServiceAdded`), estados de origen y destino cuando aplique, actor y fecha. Cada transición de estado, incluida la transitoria `Created → Quoted`, MUST publicar su propio `ServiceOrderStatusChanged` con el `OccurredAt` del momento en que ocurrió, en el mismo orden del historial.

#### Scenario: Historial de una cotización automática
- **WHEN** una orden se cotiza por debajo del umbral
- **THEN** el detalle de la orden muestra, en orden, `Created → Quoted` y `Quoted → Approved`, y se publican dos `ServiceOrderStatusChanged` en ese orden, con offsets consecutivos y el `OccurredAt` de la cotización

### Requirement: Acciones disponibles por actor
El detalle de la orden (`GET /api/v1/service-orders/{id}?actor=Approver|Workshop|Operator&actorId=`) SHALL devolver la orden con sus líneas, su historial y `availableActions` como una lista de `{ action, enabled, blockedReason }`. La lista contiene las acciones que la tabla de transiciones permite en el estado actual y que pertenecen a ese actor; `enabled = false` con `blockedReason` igual al código de error de la guarda cuando falla una precondición contextual (`ServiceOrder.PendingPricingLines`, `ServiceOrder.AppointmentRequired`). De `ScheduleAppointment` y `RescheduleAppointment` solo aparece la que corresponde a si ya hay cita. La lista MUST estar vacía para `Operator`, para un aprobador no autorizado y para un taller que no es el asignado. Las acciones del sistema (`AutoApprove`, `RequestApproval`, `Hold`, `Resume`) nunca aparecen.

#### Scenario: Taller con orden aprobada sin cita
- **WHEN** el taller asignado consulta una orden `Approved` sin cita
- **THEN** `availableActions` es `[{ ScheduleAppointment, enabled: true }, { MarkOnSite, enabled: false, blockedReason: "ServiceOrder.AppointmentRequired" }]` y no incluye `Cancel` (no es una acción del taller)

#### Scenario: Aprobador con una línea sin precio
- **WHEN** un aprobador autorizado consulta una orden `PendingApproval` con motivo `Quote` y una línea `PendingPricing`
- **THEN** `Approve` aparece con `enabled: false` y `blockedReason: "ServiceOrder.PendingPricingLines"`, y `Reject`, `RequestAdditionalService` y `Cancel` aparecen habilitadas

#### Scenario: Aprobador de otro cliente
- **WHEN** un aprobador de otro cliente que no es el responsable consulta una orden `PendingApproval`
- **THEN** `availableActions` está vacía

### Requirement: Identificación del actor en cada acción
Sin autenticación, cada acción SHALL identificar a su actor en el body: `approverId` en `approve`, `reject`, `cancel` y `additional-services/requests`; `workshopId` en `quote`, `additional-services/{lineId}/price`, `additional-services`, `appointment`, `on-site`, `start` y `complete`. La autorización (A-11) y la verificación del taller asignado MUST hacerse con ese identificador contra las réplicas locales. Un identificador ausente MUST responder `400`.

#### Scenario: Aprobación sin aprobador
- **WHEN** se envía `POST /api/v1/service-orders/{id}/approve` sin `approverId`
- **THEN** la respuesta es `400` y la orden no cambia

### Requirement: Número legible y consistencia de concurrencia
Cada orden SHALL recibir un número secuencial único mostrado como `OS-` seguido de 6 dígitos (por ejemplo `OS-000123`). Dos modificaciones concurrentes de la misma orden MUST resolverse con concurrencia optimista: la segunda recibe `409` con `code = "Concurrency.Conflict"`.

#### Scenario: Número de la primera orden
- **WHEN** se crea la primera orden del sistema
- **THEN** su número visible es `OS-000001`

#### Scenario: Aprobación concurrente
- **WHEN** dos aprobadores aprueban la misma orden al mismo tiempo
- **THEN** una aprobación se aplica y la otra recibe `409`
