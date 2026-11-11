## Purpose

Permitir registrar las rutinas de cada vehículo desde la carga inicial, ajustar sus parámetros, activarlas o desactivarlas desde el panel y consultar el plan con su próximo mantenimiento (A-01, ADR-0019, ADR-0020, ADR-0022).

## ADDED Requirements

### Requirement: Alta masiva de rutinas con simulación
`POST /api/v1/maintenance/routines/bulk?dryRun=` SHALL recibir hasta 500 filas `RoutineBulkRow` (`rowNumber`, `plate`, `routineTypeCode`, `lastServiceKm?`, `lastServiceDate?`, `intervalKm?`, `intervalMonths?`, `dueSoonThresholdPercent?`) y SHALL responder un resultado por fila (`Valid`, `Invalid` o `Skipped`) con sus errores. Con `dryRun=true` MUST NOT persistir nada ni publicar eventos. Con `dryRun=false` MUST persistir solo las filas válidas, evaluarlas con la fecha efectiva del plan y publicar `MaintenanceRoutineRegistered` por cada una.

#### Scenario: Simulación sin efectos
- **WHEN** se envían las 15 filas de `samples/import/rutinas.csv` con `dryRun=true`
- **THEN** las 15 filas son `Valid` y no existe ninguna rutina nueva ni evento publicado

#### Scenario: Más de 500 filas
- **WHEN** se envían 501 filas
- **THEN** la respuesta es `400`

### Requirement: Reglas de negocio de la fila
Una fila MUST ser `Invalid` con un código de error estable: `MaintenancePlan.NotFound` si la placa no corresponde a un plan; `RoutineType.NotFound` si el código de rutina no existe; `Routine.LastServiceKmAboveOdometer` si los km del último servicio superan el odómetro del vehículo; `Bulk.DuplicateInBatch` si otra fila anterior del lote tiene la misma placa y el mismo código; y el código de validación del parámetro si los intervalos o el porcentaje efectivos no son válidos. Los campos de intervalo y porcentaje vacíos toman los valores del tipo. MUST ser `Skipped` si ya existe una rutina con la clave natural `plate + routineTypeCode`.

#### Scenario: Filas con errores de muestra
- **WHEN** se simula `samples/import/con-errores/rutinas.csv` después de la carga válida
- **THEN** RVA102 · BRAKES es `Valid`, RVA102 · ENGINE_X es `Invalid` con `RoutineType.NotFound`, RVA103 · BRAKES es `Invalid` con `Routine.LastServiceKmAboveOdometer` (35.000 km > 30.200 km), ZZZ999 es `Invalid` con `MaintenancePlan.NotFound` y RVA101 · OIL_CHANGE es `Skipped`

#### Scenario: Duplicado en el lote
- **WHEN** un lote trae dos filas RVA105 · OIL_CHANGE
- **THEN** la primera se evalúa normalmente y la segunda es `Invalid` con `Bulk.DuplicateInBatch`

### Requirement: Línea base del primer ciclo
El ciclo inicial de una rutina SHALL empezar en los km y la fecha del último servicio informados. Si ambos vienen vacíos, SHALL empezar en 0 km y en la fecha de la carga en `America/Bogota` (A-01). Si solo uno viene vacío, la fila MUST ser `Invalid` con `Routine.IncompleteLastService`.

#### Scenario: Vehículo nuevo
- **WHEN** se registra la rutina de aceite de EVA301 sin km ni fecha del último servicio el 2026-11-12
- **THEN** el ciclo 1 inicia en 0 km el 2026-11-12 y la rutina queda `UpToDate`

### Requirement: Ajuste de parámetros de una rutina
`PUT /api/v1/maintenance/plans/{vehicleId}/routines/{routineId}` SHALL fijar o quitar (valor nulo) los ajustes de intervalo en km, intervalo en meses y porcentaje «por vencer» de la rutina, registrar quién lo hizo y reevaluar la rutina de inmediato. MUST publicar `MaintenanceRoutineParametersChanged` y los eventos de vencimiento que resulten. Los valores efectivos MUST cumplir las mismas validaciones que un tipo de rutina.

#### Scenario: Ajuste del 20 % en una moto
- **WHEN** la rutina de aceite de EVA30D (3.000 km, ciclo en 10.000 km, odómetro 12.500 km) recibe un ajuste de 20 %
- **THEN** el umbral es 600 km, faltan 500 km y la rutina pasa a `DueSoon`

#### Scenario: Quitar el ajuste
- **WHEN** se envía `dueSoonThresholdPercent = null` para EVA30D aceite
- **THEN** el porcentaje efectivo vuelve al del tipo (10 %), la rutina vuelve a `UpToDate` y su alerta `DueSoon` se resuelve con `ParametersChanged`

### Requirement: Activación y desactivación de rutinas
`PATCH /api/v1/maintenance/plans/{vehicleId}/routines/{routineId}/state` SHALL cambiar el estado de activación de la rutina sin alterar su estado de vencimiento. Una rutina `Inactive` MUST NOT evaluarse y sus alertas abiertas MUST resolverse con `RoutineDeactivated`; su orden abierta no se ve afectada (A-17). Al activarse, la rutina MUST reevaluarse de inmediato. Repetir el mismo estado MUST NOT publicar eventos. Cada cambio efectivo publica `MaintenanceRoutineStateChanged`.

#### Scenario: Desactivar una rutina vencida
- **WHEN** se desactiva la rutina de aceite de TAN201, que está `Overdue` con una alerta activa
- **THEN** la rutina queda `Inactive` y `Overdue`, su alerta queda `Resolved` con `RoutineDeactivated` y no se publica ningún evento de la orden

#### Scenario: Desactivar dos veces
- **WHEN** se desactiva una rutina ya inactiva
- **THEN** la respuesta es exitosa y no se publica ningún evento

### Requirement: Consulta del plan con pistas de km y tiempo
`GET /api/v1/maintenance/plans/{vehicleId}` SHALL devolver el estado del plan, el odómetro, la fecha de la última revisión por tiempo y, por rutina: tipo, nombre, estado de activación, estado de vencimiento, disparador, ciclo, intervalos y porcentaje efectivos con su origen (`RoutineType` u `Override`), próximo mantenimiento y, por cada dimensión presente, el porcentaje consumido, el porcentaje de la franja «por vencer», lo que falta (negativo si se excedió) y el estado de la dimensión. Un vehículo sin plan MUST responder `404`.

#### Scenario: Rutina vencida por km
- **WHEN** se consulta el plan de TAN201 después de la carga
- **THEN** la pista de km del aceite tiene `consumedPercent = 108`, `remaining = -800` y estado `Overdue`

### Requirement: Resumen de planes para la lista de la flota
`GET /api/v1/maintenance/plans?customerId=` SHALL devolver en una sola respuesta, por vehículo, el `VehicleId`, la placa, el estado del plan, el odómetro, el peor estado de vencimiento de sus rutinas activas, el mayor porcentaje consumido y el número de rutinas activas por estado de vencimiento (`UpToDate`, `DueSoon`, `Overdue`) más el número de rutinas inactivas. Sin `customerId` devuelve todos los planes.

#### Scenario: Peor estado del vehículo
- **WHEN** RVA104 tiene aceite `UpToDate` y frenos `Overdue`
- **THEN** su resumen indica `Overdue`, el porcentaje consumido de frenos (105) y los conteos `UpToDate = 1`, `DueSoon = 0`, `Overdue = 1`, `Inactive = 0`
