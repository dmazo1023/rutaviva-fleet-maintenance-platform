## Purpose

Avisar en tiempo real al CRM, por audiencia (dueño, responsable y taller), cada cambio de órdenes, aprobaciones, alertas, rutinas, lecturas de odómetro y vehículos, para que la web se actualice sin recargar y la aprobación prioritaria llegue de inmediato (ADR-0016, RQ-07).

## ADDED Requirements

### Requirement: Solo eventos nuevos
Notifications SHALL comenzar a leer el EventBus desde el último offset existente al arrancar y MUST NOT retransmitir eventos publicados antes de su arranque.

#### Scenario: Arranque con eventos antiguos en el log
- **WHEN** Notifications arranca con 100 eventos ya guardados en el EventBus
- **THEN** no envía ningún mensaje por esos 100 eventos y envía el mensaje del siguiente evento publicado

### Requirement: Suscripción a audiencias por perfil
El hub `/hubs/notifications` SHALL permitir que una conexión se una y salga de los grupos de un cliente (`JoinCustomer`/`LeaveCustomer`), de un aprobador (`JoinApprover`/`LeaveApprover`) y de un taller (`JoinWorkshop`/`LeaveWorkshop`). Un identificador vacío o con formato inválido MUST rechazarse con un error del hub sin unir la conexión a ningún grupo.

#### Scenario: Cambio de cliente en el selector
- **WHEN** una conexión unida al cliente A llama `LeaveCustomer(A)` y luego `JoinCustomer(B)`
- **THEN** recibe los mensajes del cliente B y deja de recibir los del cliente A

#### Scenario: Identificador inválido
- **WHEN** una conexión llama `JoinCustomer("no-es-un-guid")`
- **THEN** recibe un error del hub y no queda unida a ningún grupo

### Requirement: Cambios de órdenes de servicio
Por cada `ServiceOrderCreated`, `ServiceOrderStatusChanged`, `ServiceOrderLinesChanged`, `ServiceOrderAppointmentScheduled` y `ServiceOrderCompleted`, Notifications SHALL enviar `ServiceOrderChanged` al grupo del cliente dueño, al grupo del taller asignado y, si el vehículo tiene responsable, al grupo de ese aprobador. El mensaje MUST indicar el tipo de cambio, el estado actual y, en un cambio de estado, el estado anterior.

#### Scenario: El taller cotiza y la orden se aprueba automáticamente
- **WHEN** se publica `ServiceOrderStatusChanged` de `Quoted` a `Approved` con `AutoApproved = true`
- **THEN** el grupo del cliente dueño, el del taller y el del responsable reciben `ServiceOrderChanged` con `kind = StatusChanged`, `fromStatus = Quoted`, `status = Approved` y `autoApproved = true`

#### Scenario: Orden finalizada
- **WHEN** se publica `ServiceOrderCompleted`
- **THEN** el grupo del cliente dueño recibe `ServiceOrderChanged` con `kind = Completed` y `status = Completed`

#### Scenario: Vehículo sin responsable
- **WHEN** se publica `ServiceOrderCreated` de un vehículo sin `ResponsibleApproverId`
- **THEN** el mensaje llega al grupo del cliente y al del taller, y a ningún grupo de aprobador

### Requirement: Solicitudes de aprobación con prioridad
Por cada `ServiceOrderApprovalRequested`, Notifications SHALL enviar `ApprovalRequested` con el motivo y la prioridad al grupo del cliente dueño y, si existe, al grupo del responsable asignado. El taller MUST NOT recibir `ApprovalRequested`.

#### Scenario: Servicio adicional con el vehículo en sitio
- **WHEN** se publica `ServiceOrderApprovalRequested` con `Reason = AdditionalServices` y `Priority = High` de un vehículo con responsable
- **THEN** el grupo del cliente dueño y el grupo del responsable reciben `ApprovalRequested` con `priority = High` y `reason = AdditionalServices`

#### Scenario: Cotización por encima del monto automático
- **WHEN** se publica `ServiceOrderApprovalRequested` con `Reason = Quote` y `Priority = Normal`
- **THEN** el grupo del cliente dueño recibe `ApprovalRequested` con `priority = Normal` y el grupo del taller no lo recibe

### Requirement: Cambios de alertas
Por cada `AlertRaised` y `AlertStatusChanged`, Notifications SHALL enviar `AlertChanged` al grupo del cliente dueño del vehículo, con el tipo, el origen (`Distance`, `Time` o `DistanceAndTime`), el estado y, si aplica, el motivo de resolución.

#### Scenario: Alerta por vencer escalada a vencida
- **WHEN** en una misma lectura se publican `AlertStatusChanged` (alerta `DueSoon` → `Resolved` con `EscalatedToOverdue`) y luego `AlertRaised` (`Overdue`)
- **THEN** el grupo del cliente recibe dos `AlertChanged` en ese mismo orden

### Requirement: Cambios de rutinas
Por cada evento de rutina (`MaintenanceRoutineRegistered`, `MaintenanceRoutineDueSoon`, `MaintenanceRoutineOverdue`, `MaintenanceRoutineDueStatusReverted`, `MaintenanceRoutineRestarted`, `MaintenanceRoutineStateChanged` y `MaintenanceRoutineParametersChanged`), Notifications SHALL enviar `RoutineChanged` al grupo del cliente dueño con el tipo de cambio, el estado de activación, el estado de vencimiento, el próximo mantenimiento (km y fecha), el % consumido y el % «por vencer» efectivo.

#### Scenario: Rutina reiniciada al finalizar la orden
- **WHEN** se publica `MaintenanceRoutineRestarted` con `DueStatus = UpToDate`
- **THEN** el grupo del cliente recibe `RoutineChanged` con `kind = Restarted`, `dueStatus = UpToDate` y el nuevo `nextDueKm`

#### Scenario: Rutina vencida por kilómetros
- **WHEN** se publica `MaintenanceRoutineOverdue` con `TriggeredBy = Distance`
- **THEN** el grupo del cliente recibe `RoutineChanged` con `kind = Overdue` y `triggeredBy = Distance`

### Requirement: Lecturas de odómetro
Por cada `OdometerReadingRecorded`, Notifications SHALL enviar `OdometerReadingRecorded` al grupo del cliente dueño, tanto para las lecturas aceptadas como para las ignoradas, con el resultado, el motivo y el odómetro vigente.

#### Scenario: Lectura repetida
- **WHEN** se publica `OdometerReadingRecorded` con `Outcome = Ignored` e `IgnoredReason = NotGreaterThanCurrent`
- **THEN** el grupo del cliente recibe el mensaje con `outcome = Ignored` y `ignoredReason = NotGreaterThanCurrent`

### Requirement: Cambios de vehículos
Por cada `VehicleRegistered` y `VehicleStatusChanged`, Notifications SHALL enviar `VehicleChanged` al grupo del cliente dueño con la placa y el estado del vehículo.

#### Scenario: Vehículo desactivado
- **WHEN** se publica `VehicleStatusChanged` con `Status = Inactive`
- **THEN** el grupo del cliente dueño recibe `VehicleChanged` con `kind = StatusChanged` y `status = Inactive`

#### Scenario: Vehículos cargados por la importación
- **WHEN** se publican 10 `VehicleRegistered` de un mismo cliente
- **THEN** el grupo de ese cliente recibe 10 `VehicleChanged` con `kind = Registered`

### Requirement: Mensajes propios, ordenados e identificables
Los mensajes del hub SHALL ser contratos propios de Notifications, no los eventos de integración, serializados en JSON con nombres en *camelCase* y enums como texto. Cada mensaje MUST incluir `eventId` (el identificador del evento de origen) y `occurredAt`. Los mensajes de una audiencia MUST enviarse en el orden de los offsets del EventBus.

#### Scenario: Conexión en dos grupos
- **WHEN** una conexión unida al cliente dueño y al grupo del responsable recibe dos veces el mensaje de un mismo evento
- **THEN** ambos mensajes tienen el mismo `eventId`, lo que permite descartar el duplicado

#### Scenario: Orden de los mensajes
- **WHEN** se publican, en este orden, `ServiceOrderStatusChanged` (`OnSite` → `PendingApproval`) y `ServiceOrderApprovalRequested`
- **THEN** el grupo del cliente recibe `ServiceOrderChanged` antes que `ApprovalRequested`

### Requirement: Sin persistencia ni reenvío
Notifications MUST NOT guardar mensajes ni reenviar los que se emitieron mientras una conexión estaba desconectada.

#### Scenario: Cliente desconectado durante un evento
- **WHEN** una conexión se desconecta, se publica `AlertRaised` de su cliente y la conexión se restablece
- **THEN** la conexión no recibe el `AlertChanged` de ese evento

### Requirement: Eventos sin interés para la web
Notifications SHALL ignorar, sin error y sin bloquear el stream, los eventos de sus streams que no tienen mensaje asociado (por ejemplo `MaintenancePlanStatusChanged` y `VehicleResponsibleAssigned`).

#### Scenario: Evento sin mensaje asociado
- **WHEN** se publica `MaintenancePlanStatusChanged` y luego `AlertRaised`
- **THEN** no se envía ningún mensaje por el primero, no se registra una *dead letter* y el `AlertChanged` del segundo se envía
