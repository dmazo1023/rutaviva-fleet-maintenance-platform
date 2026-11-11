## Purpose

Convertir las transiciones de las rutinas en alertas gestionables por el CRM, con un ciclo de vida auditable, una sola alerta por ciclo y tipo, y resolución automática determinada por el negocio (ADR-0015, A-12).

## ADDED Requirements

### Requirement: Alerta creada con la transición de la rutina
El servicio SHALL crear una alerta `Active` de tipo `DueSoon` u `Overdue` con el disparador, el vehículo, la placa, el dueño, la rutina y el ciclo, en la misma transacción que la transición de la rutina, y SHALL publicar `AlertRaised` en el stream `maintenance.alert`.

#### Scenario: Atomicidad con la rutina
- **WHEN** la transacción que pasa una rutina a `Overdue` falla
- **THEN** no existe ni la alerta ni el evento `AlertRaised`

### Requirement: Una alerta por rutina, ciclo y tipo
MUST existir como máximo una alerta por combinación de rutina, ciclo y tipo, garantizado en el dominio y en la base de datos.

#### Scenario: Inserción duplicada
- **WHEN** se intenta guardar una segunda alerta `Overdue` para la misma rutina y ciclo
- **THEN** la base de datos la rechaza por el índice único `UX_Alerts_OnePerCycleAndType`

### Requirement: Resolución automática
Las alertas abiertas (`Active` o `Acknowledged`) SHALL resolverse automáticamente: la `DueSoon` del ciclo con `EscalatedToOverdue` cuando la rutina vence; todas las de la rutina con `RoutineRestarted` cuando se reinicia el ciclo; la `DueSoon` con `ParametersChanged` cuando la rutina vuelve a `UpToDate`; y todas con `RoutineDeactivated` cuando la rutina se desactiva. Cada resolución SHALL publicar `AlertStatusChanged`. Resolver una alerta ya resuelta MUST NOT tener efecto.

#### Scenario: Escalamiento
- **WHEN** RVA101 aceite pasa de `DueSoon` a `Overdue`
- **THEN** la alerta `DueSoon` queda `Resolved` con `EscalatedToOverdue` y existe una alerta `Overdue` `Active`

#### Scenario: Alerta atendida que se resuelve
- **WHEN** una alerta `Acknowledged` pertenece a una rutina cuyo ciclo se reinicia
- **THEN** la alerta queda `Resolved` con `RoutineRestarted` y conserva quién y cuándo la atendió

### Requirement: Atender una alerta
`POST /api/v1/maintenance/alerts/{id}/acknowledge { acknowledgedBy }` SHALL pasar una alerta `Active` a `Acknowledged` con quién y cuándo, y publicar `AlertStatusChanged`. `acknowledgedBy` vacío MUST responder `400`; una alerta inexistente `404`; una alerta que no está `Active` MUST responder `422` con `code = "Alert.InvalidTransition"`. No existe resolución manual.

#### Scenario: Atender una alerta resuelta
- **WHEN** se atiende una alerta `Resolved`
- **THEN** la respuesta es `422` y la alerta no cambia

### Requirement: Bandeja de alertas
`GET /api/v1/maintenance/alerts?status=&vehicleId=&customerId=&page=&pageSize=` SHALL devolver las alertas filtradas, ordenadas por fecha de creación descendente, con placa, rutina, tipo, disparador, estado, ciclo y fechas, paginadas con `pageSize` máximo de 100.

#### Scenario: Alertas activas de un cliente
- **WHEN** se consultan las alertas `Active` de RutaViva S.A.S. después de la carga
- **THEN** solo aparecen alertas de vehículos de ese cliente, la más reciente primero
