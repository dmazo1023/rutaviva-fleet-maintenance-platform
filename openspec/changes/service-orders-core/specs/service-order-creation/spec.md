## Purpose

Convertir cada vencimiento de rutina en exactamente una orden de servicio para el taller asignado, permitir la creación manual tras una cancelación y garantizar que nunca existan dos órdenes abiertas para el mismo vehículo y rutina (RN-3, A-07, ADR-0011).

## ADDED Requirements

### Requirement: Creación automática por vencimiento
Al recibir `MaintenanceRoutineOverdue`, el servicio SHALL crear una orden `Created` con origen `Automatic` para el vehículo y la rutina del evento, asignada al cliente dueño y al taller registrados para el vehículo, con el nombre de la rutina del evento, y SHALL publicar `ServiceOrderCreated`.

#### Scenario: Rutina vencida por kilómetros
- **WHEN** llega `MaintenanceRoutineOverdue` para la rutina de aceite de TAN201, cuyo taller asignado es «Autofrenos Medellín»
- **THEN** existe una orden `Created`, origen `Automatic`, de TAN201 y aceite, asignada a ese taller, y se publica `ServiceOrderCreated`

### Requirement: Una sola orden por vencimiento aunque el evento se repita
Una misma entrega repetida de `MaintenanceRoutineOverdue` MUST producir una sola orden. Si ya existe una orden abierta (cualquier estado distinto de `Completed` y `Cancelled`, incluido `OnHold`) para el mismo vehículo y rutina, el evento MUST tratarse como duplicado: se registra en el log como `DuplicateOpenServiceOrder`, no se crea otra orden y no se reintenta.

#### Scenario: Evento entregado cinco veces
- **WHEN** el mismo `MaintenanceRoutineOverdue` se publica 5 veces
- **THEN** existe exactamente 1 orden para ese vehículo y rutina

#### Scenario: Orden abierta en pausa
- **WHEN** llega un `MaintenanceRoutineOverdue` nuevo para un vehículo y rutina que ya tienen una orden `OnHold`
- **THEN** no se crea otra orden y el evento se marca como procesado

#### Scenario: Carrera entre dos creaciones
- **WHEN** una creación automática y una manual para el mismo vehículo y rutina se confirman al mismo tiempo
- **THEN** solo una se guarda; la otra termina como `DuplicateOpenServiceOrder` (sin reintento en el consumidor y `409` en la API)

### Requirement: Orden creada en pausa si el vehículo está inactivo
Si al procesar `MaintenanceRoutineOverdue` el vehículo está `Inactive` en la réplica local, la orden SHALL crearse directamente en `OnHold` con estado previo `Created` (carrera con la desactivación, ADR-0020).

#### Scenario: Vencimiento emitido justo antes de desactivar el vehículo
- **WHEN** se procesa `VehicleStatusChanged` a `Inactive` y luego un `MaintenanceRoutineOverdue` de ese vehículo emitido antes de la desactivación
- **THEN** la orden se crea en `OnHold` con estado previo `Created`, y al reactivarse el vehículo pasa a `Created`

### Requirement: Creación manual tras una cancelación
`POST /api/v1/service-orders` SHALL crear una orden con origen `Manual` solo si, según las réplicas locales, la rutina está `Overdue` y activa, el vehículo está activo y no existe otra orden abierta para ese vehículo y rutina. Cada condición incumplida MUST responder con su propio código: `ServiceOrder.RoutineNotOverdue` (`422`), `ServiceOrder.VehicleInactive` (`422`) o `ServiceOrder.DuplicateOpenServiceOrder` (`409`).

#### Scenario: Rutina vencida con su orden cancelada
- **WHEN** el operador crea una orden para una rutina `Overdue` cuya orden anterior fue cancelada por rechazo de la cotización
- **THEN** se crea una orden `Created` con origen `Manual` y se publica `ServiceOrderCreated`

#### Scenario: Rutina al día
- **WHEN** el operador crea una orden para una rutina `UpToDate`
- **THEN** la respuesta es `422` con `code = "ServiceOrder.RoutineNotOverdue"`

#### Scenario: Ya existe una orden abierta
- **WHEN** el operador crea una orden para una rutina que ya tiene una orden `Approved`
- **THEN** la respuesta es `409` con `code = "ServiceOrder.DuplicateOpenServiceOrder"`

#### Scenario: Cancelar no crea otra orden automáticamente
- **WHEN** se cancela la orden de una rutina que sigue `Overdue`
- **THEN** no se crea ninguna orden nueva hasta que el operador la cree manualmente (A-07)

### Requirement: Réplicas locales de datos ajenos
El servicio SHALL mantener réplicas locales de solo lectura de los vehículos (placa, dueño, taller, responsable y estado), de los aprobadores (cliente, nombre y estado) y de las rutinas (vehículo, nombre, estado de activación, estado de vencimiento y ciclo), actualizadas únicamente por los eventos de `fleet.vehicle`, `customers.customer` y `maintenance.routine`. Las decisiones de creación y autorización MUST basarse solo en estas réplicas, sin llamadas HTTP a otros servicios.

#### Scenario: Cambio de responsable
- **WHEN** llega `VehicleResponsibleAssigned` con un nuevo responsable para un vehículo
- **THEN** el nuevo responsable puede aprobar las órdenes pendientes de ese vehículo y el anterior, si es de otro cliente, ya no puede

#### Scenario: Rutina reiniciada
- **WHEN** llega `MaintenanceRoutineRestarted` para una rutina
- **THEN** su réplica queda `UpToDate` con el nuevo número de ciclo, y la creación manual para esa rutina responde `ServiceOrder.RoutineNotOverdue`

### Requirement: Consulta de órdenes
`GET /api/v1/service-orders` SHALL listar órdenes filtrables por `status`, `statusGroup` (`PendingApproval`, `Open`, `Closed` o `All`), `customerId`, `vehicleId`, `routineId`, `workshopId` y `origin`, paginadas y ordenadas por fecha de actualización descendente; cada fila incluye número, placa, rutina, estado, motivo y prioridad de aprobación, total y fecha de actualización. `Open` agrupa los estados distintos de `Completed` y `Cancelled` (incluido `OnHold`), `Closed` agrupa `Completed` y `Cancelled`, y `All` no filtra por estado.

#### Scenario: Órdenes de un taller
- **WHEN** se pide `GET /api/v1/service-orders?workshopId={id}&status=Approved`
- **THEN** solo se devuelven las órdenes `Approved` de ese taller

#### Scenario: Órdenes abiertas de una rutina
- **WHEN** se pide `GET /api/v1/service-orders?vehicleId={v}&routineId={r}&statusGroup=Open`
- **THEN** se devuelve como máximo una orden (RN-3), incluida si está `OnHold`

#### Scenario: Grupo de cerradas de un cliente
- **WHEN** se pide `GET /api/v1/service-orders?customerId={c}&statusGroup=Closed`
- **THEN** solo se devuelven órdenes `Completed` o `Cancelled` de ese cliente
