## Purpose

Mantener en el schema `reporting` copias de lectura desnormalizadas de clientes, talleres, vehículos, rutinas, órdenes y alertas, alimentadas por los eventos de integración, para que los reportes se resuelvan sin consultar a los servicios de negocio (ADR-0023).

## ADDED Requirements

### Requirement: Suscripción a todos los streams de negocio desde el checkpoint
Reporting SHALL suscribirse con el nombre `reporting` a los streams `customers.customer`, `workshops.workshop`, `fleet.vehicle`, `maintenance.routine`, `maintenance.alert` y `serviceorders.service-order`, comenzando desde su checkpoint, y SHALL aplicar cada evento exactamente una vez.

#### Scenario: Primer arranque
- **WHEN** Reporting arranca por primera vez con 200 eventos ya en el log
- **THEN** procesa los 200 eventos en orden de offset y sus proyecciones reflejan el estado actual del sistema

#### Scenario: Evento repetido
- **WHEN** el mismo `ServiceOrderStatusChanged` llega 3 veces
- **THEN** la fila de la orden en los hechos de órdenes se actualiza una sola vez

### Requirement: Dimensiones de clientes, talleres y vehículos
Reporting SHALL mantener una fila por cliente (nombre, NIT y estado), por taller (nombre, ciudad y estado) y por vehículo (placa, dueño, taller asignado, estado y odómetro actual) a partir de `CustomerRegistered`, `CustomerStatusChanged`, `WorkshopRegistered`, `WorkshopStatusChanged`, `VehicleRegistered`, `VehicleStatusChanged` y `OdometerReadingRecorded`.

#### Scenario: Vehículo desactivado
- **WHEN** llega `VehicleStatusChanged` con `Status = Inactive` para RVA101
- **THEN** la dimensión de vehículos muestra RVA101 como `Inactive`

#### Scenario: Lectura aceptada
- **WHEN** llega `OdometerReadingRecorded` de RVA101 con `Outcome = Accepted` y `CurrentOdometerKm = 53000`
- **THEN** el odómetro de RVA101 en la dimensión es 53.000

#### Scenario: Lectura ignorada
- **WHEN** llega `OdometerReadingRecorded` de RVA101 con `Outcome = Ignored` y `CurrentOdometerKm = 53000`
- **THEN** el odómetro de RVA101 en la dimensión sigue en 53.000

### Requirement: Estado de mantenimiento por rutina
Reporting SHALL mantener una fila por rutina con placa, cliente, nombre, estado de activación, estado de vencimiento, ciclo, inicio del ciclo, próximo mantenimiento en km y fecha, odómetro y porcentaje consumido, reemplazándola con el `RoutineSnapshot` de cada evento del stream `maintenance.routine` que lo trae.

#### Scenario: Rutina vencida
- **WHEN** llega `MaintenanceRoutineOverdue` de la rutina de aceite de TAN201 con `DueStatus = Overdue` y `ConsumedPercent = 108`
- **THEN** la fila de esa rutina tiene `DueStatus = Overdue` y consumo 108

#### Scenario: Rutina reiniciada
- **WHEN** llega `MaintenanceRoutineRestarted` con un ciclo nuevo y `DueStatus = UpToDate`
- **THEN** la fila muestra el ciclo nuevo, el próximo mantenimiento recalculado y `UpToDate`

### Requirement: Hechos de órdenes de servicio con fechas por fase
Reporting SHALL mantener una fila por orden con número, placa, rutina, cliente, taller, origen, estado, montos de rutina, adicionales y total, moneda, aprobación automática, cita, motivo de cancelación y la fecha en que la orden alcanzó por primera vez cada fase: creación, cotización, aprobación, en sitio, en proceso, finalizada y cancelada. Las fechas de fase MUST tomarse del momento en que ocurrió el evento, no del momento en que se procesó.

#### Scenario: Aprobación automática
- **WHEN** llegan `ServiceOrderStatusChanged` de `Created` a `Quoted` y de `Quoted` a `Approved` con el mismo `OccurredAt`
- **THEN** la fila tiene fecha de cotización y fecha de aprobación iguales, `AutoApproved = true` y estado `Approved`

#### Scenario: Vuelta a En sitio tras servicios adicionales
- **WHEN** una orden pasa de `OnSite` a `PendingApproval` y luego otra vez a `OnSite`
- **THEN** la fecha de llegada a sitio conserva la primera llegada

#### Scenario: Orden finalizada
- **WHEN** llega `ServiceOrderCompleted`
- **THEN** la fila tiene estado `Completed` y fecha de finalización igual al `OccurredAt` del evento

### Requirement: Hechos de alertas
Reporting SHALL mantener una fila por alerta con placa, cliente, rutina, tipo, origen (km, tiempo o ambos), estado, fecha de generación, de atención y de resolución, y motivo de resolución, a partir de `AlertRaised` y `AlertStatusChanged`.

#### Scenario: Alerta atendida y resuelta
- **WHEN** llegan `AlertRaised`, luego `AlertStatusChanged` a `Acknowledged` y luego `AlertStatusChanged` a `Resolved` con `ResolutionReason = RoutineRestarted`
- **THEN** la fila tiene las tres fechas y el motivo `RoutineRestarted`

### Requirement: Descarte de eventos obsoletos
Una fila de proyección SHALL actualizarse solo con un evento posterior al último que la modificó. Un evento anterior o igual MUST ignorarse sin error.

#### Scenario: Evento obsoleto tras reconstrucción parcial
- **WHEN** la fila de una orden ya refleja el evento del offset 120 y se le aplica el evento del offset 95
- **THEN** la fila no cambia

### Requirement: Lector tolerante de valores desconocidos
Las proyecciones MUST guardar los valores de enumeración tal como llegan, sin restringirlos a una lista cerrada, y MUST NOT fallar ante un valor nuevo agregado por el servicio dueño.

#### Scenario: Estado nuevo de orden
- **WHEN** llega `ServiceOrderStatusChanged` con un `ToStatus` que Reporting no conoce
- **THEN** la fila guarda ese valor y el procesamiento continúa con el siguiente evento

### Requirement: Reconstrucción completa de las proyecciones
Reporting SHALL permitir reconstruir todas las proyecciones al arrancar, vaciándolas y reiniciando su checkpoint a 0 en una sola operación, antes de procesar eventos.

#### Scenario: Reconstrucción
- **WHEN** Reporting arranca con la reconstrucción activada
- **THEN** las proyecciones resultantes son idénticas a las que existían antes de vaciarlas
