## Purpose

Determinar el estado de vencimiento de cada rutina de un vehículo por kilómetros y por tiempo, avisar una sola vez por ciclo, declarar el vencimiento que origina la orden de servicio y reiniciar el ciclo cuando el servicio se completa (RN-1, RN-2, RN-3, RN-5).

## ADDED Requirements

### Requirement: Plan de mantenimiento por vehículo registrado
Al recibir `VehicleRegistered`, el servicio SHALL crear un plan de mantenimiento con la placa, el dueño, el estado del vehículo, el odómetro inicial y como fecha de última revisión por tiempo la fecha de registro en `America/Bogota`. Recibir de nuevo el mismo vehículo MUST NOT crear otro plan.

#### Scenario: Vehículo nuevo
- **WHEN** llega `VehicleRegistered` de RVA101 con `InitialOdometerKm = 52300`
- **THEN** existe un plan activo para RVA101 con odómetro 52.300 km y sin rutinas

#### Scenario: Evento repetido
- **WHEN** `VehicleRegistered` de RVA101 llega dos veces
- **THEN** existe un solo plan para RVA101

### Requirement: Próximo mantenimiento y porcentaje consumido
Para cada rutina, el próximo mantenimiento SHALL ser `inicio del ciclo en km + intervalo en km` y `fecha de inicio del ciclo + intervalo en meses de calendario`. El intervalo y el porcentaje efectivos SHALL ser el ajuste de la rutina si existe y, si no, el valor del tipo. El porcentaje consumido de una dimensión SHALL ser lo recorrido sobre el intervalo (km) o los días transcurridos sobre los días del intervalo (tiempo), y puede superar 100. El porcentaje consumido de la rutina SHALL ser el máximo entre sus dimensiones presentes.

#### Scenario: Próximo mantenimiento con ambos intervalos
- **WHEN** una rutina de 10.000 km y 6 meses inicia su ciclo en 45.000 km el 2026-08-01
- **THEN** su próximo mantenimiento es 55.000 km o 2027-02-01

#### Scenario: Consumo de la rutina con dos dimensiones
- **WHEN** una rutina tiene 73 % consumido por km y 57 % por tiempo
- **THEN** su porcentaje consumido es 73

### Requirement: Umbral «por vencer» por kilómetros
Una dimensión de km SHALL estar `UpToDate` si faltan más km que el umbral (`intervalo × % / 100`), `DueSoon` si faltan km iguales o menores que el umbral y mayores que 0, y `Overdue` si el odómetro alcanza o supera el próximo mantenimiento.

#### Scenario: Justo antes del umbral
- **WHEN** una rutina de 10.000 km con 10 % y ciclo iniciado en 0 km se evalúa con 8.999 km
- **THEN** su estado es `UpToDate`

#### Scenario: En el umbral
- **WHEN** la misma rutina se evalúa con 9.000 km
- **THEN** su estado es `DueSoon`

#### Scenario: En el intervalo
- **WHEN** la misma rutina se evalúa con 10.000 km
- **THEN** su estado es `Overdue`

### Requirement: Umbral «por vencer» por tiempo
Una dimensión de tiempo SHALL estar `DueSoon` cuando los días que faltan para la fecha de vencimiento sean menores o iguales a `piso(días del intervalo × % / 100)` y mayores que 0, y `Overdue` cuando la fecha evaluada sea igual o posterior a la fecha de vencimiento.

#### Scenario: Seis meses con 10 %
- **WHEN** una rutina de 6 meses iniciada el 2026-05-25 (vence 2026-11-25, 184 días) se evalúa el 2026-11-12
- **THEN** faltan 13 días, el umbral es 18 días y su estado es `DueSoon`

#### Scenario: Vencimiento exacto por fecha
- **WHEN** una rutina de 12 meses iniciada el 2026-02-10 se evalúa el 2027-02-10
- **THEN** su estado es `Overdue`

### Requirement: Kilómetros y tiempo con igual prioridad
El estado de una rutina con ambos intervalos SHALL ser el más severo de sus dos dimensiones, y el disparador SHALL indicar `Distance`, `Time` o `DistanceAndTime` según qué dimensiones alcanzaron ese estado (RN-1, A-04).

#### Scenario: Vencida por km y tiempo
- **WHEN** TAN203 (aceite iniciado en 60.000 km el 2026-05-01) se evalúa con 70.100 km el 2026-11-12
- **THEN** la rutina queda `Overdue` con disparador `DistanceAndTime`

#### Scenario: Una dimensión más severa
- **WHEN** una rutina está `Overdue` por km y `DueSoon` por tiempo
- **THEN** la rutina queda `Overdue` con disparador `Distance`

### Requirement: Aviso «por vencer» una sola vez por ciclo
Al pasar de `UpToDate` a `DueSoon`, el servicio SHALL publicar `MaintenanceRoutineDueSoon` con el disparador. La alerta `DueSoon` MUST crearse solo la primera vez en el ciclo, aunque la rutina vuelva a `DueSoon` después de un cambio de parámetros (RN-2, A-16).

#### Scenario: Primer aviso del ciclo
- **WHEN** RVA101 aceite pasa a `DueSoon` con 54.100 km
- **THEN** se publica `MaintenanceRoutineDueSoon` con `TriggeredBy = Distance` y existe una alerta `DueSoon` activa del ciclo

#### Scenario: Segundo paso a «por vencer» en el mismo ciclo
- **WHEN** una rutina que ya avisó en el ciclo vuelve a `UpToDate` por un cambio de parámetros y luego otra vez a `DueSoon`
- **THEN** se publica `MaintenanceRoutineDueSoon` pero no se crea una segunda alerta `DueSoon` del ciclo

### Requirement: Vencimiento único por ciclo
Al llegar a `Overdue`, el servicio SHALL publicar `MaintenanceRoutineOverdue` con el número de ciclo y el disparador, una sola vez por ciclo. Un salto directo de `UpToDate` a `Overdue` MUST producir solo el vencimiento, sin aviso «por vencer» (RN-3, A-03).

#### Scenario: Escalamiento desde «por vencer»
- **WHEN** RVA101 aceite, en `DueSoon`, recibe una lectura de 55.050 km
- **THEN** se publica una sola vez `MaintenanceRoutineOverdue` con el ciclo actual

#### Scenario: Salto directo
- **WHEN** una rutina `UpToDate` se evalúa con km por encima de su próximo mantenimiento
- **THEN** se publica `MaintenanceRoutineOverdue` y no se publica `MaintenanceRoutineDueSoon` ni se crea alerta `DueSoon`

#### Scenario: Reevaluación de una rutina ya vencida
- **WHEN** una rutina `Overdue` recibe otra lectura mayor
- **THEN** no se publica otro `MaintenanceRoutineOverdue`

### Requirement: El vencimiento no retrocede por parámetros
Una reevaluación SHALL devolver una rutina de `DueSoon` a `UpToDate` si los parámetros vigentes lo determinan, publicando `MaintenanceRoutineDueStatusReverted` con motivo `ParametersChanged`. Una rutina `Overdue` MUST NOT retroceder por ningún cambio de parámetros; solo el reinicio del ciclo la devuelve a `UpToDate` (A-16).

#### Scenario: Bajar el porcentaje
- **WHEN** una rutina `DueSoon` al 92 % pasa de un umbral de 10 % a 5 %
- **THEN** la rutina queda `UpToDate`, se publica `MaintenanceRoutineDueStatusReverted` y su alerta `DueSoon` se resuelve con `ParametersChanged`

#### Scenario: Ampliar el intervalo de una rutina vencida
- **WHEN** una rutina `Overdue` por km amplía su intervalo de km de 10.000 a 20.000
- **THEN** la rutina sigue `Overdue` y su próximo mantenimiento se recalcula

### Requirement: Reinicio del ciclo al completar la orden
Al recibir `ServiceOrderCompleted`, el servicio SHALL reiniciar la rutina de la orden: número de ciclo + 1, inicio del ciclo en los km y la fecha del servicio, estado `UpToDate` y aviso del ciclo sin emitir. Si los km del servicio superan el odómetro, el odómetro MUST actualizarse a ese valor. Las alertas abiertas de la rutina MUST resolverse con `RoutineRestarted` y se MUST publicar `MaintenanceRoutineRestarted` (RN-5, A-08).

#### Scenario: Servicio completado
- **WHEN** llega `ServiceOrderCompleted` de la rutina de frenos de RVA104 con 98.400 km y fecha 2026-11-12, y el odómetro está en 98.000 km
- **THEN** la rutina queda `UpToDate` en el ciclo 2 con próximo mantenimiento 118.400 km o 2027-11-12, el odómetro pasa a 98.400 km y su alerta `Overdue` queda `Resolved` con `RoutineRestarted`

#### Scenario: Reinicio de una rutina inactiva
- **WHEN** se completa la orden de una rutina que se desactivó mientras la orden seguía abierta
- **THEN** el ciclo se reinicia y la rutina sigue `Inactive`

### Requirement: Suspensión del plan por vehículo inactivo
Al recibir `VehicleStatusChanged` con `Inactive`, el servicio SHALL suspender el plan y publicar `MaintenancePlanStatusChanged`. Un plan suspendido MUST NOT evaluar km ni tiempo ni crear alertas. Al recibir `Active`, el servicio SHALL reanudar el plan y reevaluarlo con la fecha efectiva `max(hoy, última revisión)` y el odómetro actual. Suspender un plan suspendido o reanudar uno activo MUST NOT publicar eventos (ADR-0020, A-18).

#### Scenario: Vehículo desactivado
- **WHEN** RVA102 se inactiva y luego recibe una lectura que vencería su aceite
- **THEN** el odómetro se actualiza, pero la rutina conserva su estado y no se publica `MaintenanceRoutineOverdue`

#### Scenario: Vehículo reactivado
- **WHEN** RVA102 se reactiva con un odómetro que supera el próximo mantenimiento del aceite
- **THEN** el plan queda `Active` y se publica `MaintenanceRoutineOverdue`

#### Scenario: Reentrega del evento
- **WHEN** `VehicleStatusChanged(Inactive)` se procesa dos veces para el mismo vehículo
- **THEN** se publica un solo `MaintenancePlanStatusChanged`

### Requirement: Resultado esperado de la carga inicial de muestra
Al cargar `samples/import/` y evaluar el 2026-11-12, el servicio SHALL dejar exactamente 4 rutinas `DueSoon` (RVA102 aceite por km, RVA103 aceite por tiempo, EVA302 frenos por km y EVA30D aceite por km con ajuste de 20 %) y 4 rutinas `Overdue` (RVA104 frenos por km, TAN201 aceite por km, TAN202 aceite por tiempo y TAN203 aceite por km y tiempo), con una alerta por cada una y 4 eventos `MaintenanceRoutineOverdue`.

#### Scenario: Carga de la demo
- **WHEN** se registran los 10 vehículos y las 15 rutinas de `samples/import/` con fecha efectiva 2026-11-12
- **THEN** hay 4 alertas `DueSoon` activas, 4 alertas `Overdue` activas y 4 eventos `MaintenanceRoutineOverdue` en el stream `maintenance.routine`
