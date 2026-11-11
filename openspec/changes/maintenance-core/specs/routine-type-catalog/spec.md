## Purpose

Mantener la matriz de tipos de rutina (intervalos por defecto y porcentaje «por vencer») como datos editables desde la web, con efecto inmediato sobre las rutinas de cada tipo y sin despliegues (ADR-0019, A-02).

## ADDED Requirements

### Requirement: Catálogo inicial de tipos de rutina
El servicio SHALL iniciar con los tipos `OIL_CHANGE` (10.000 km, 6 meses, 10 %), `BRAKES` (20.000 km, 12 meses, 10 %) y `TIRES` (40.000 km, 24 meses, 10 %), todos activos, y SHALL exponerlos en `GET /api/v1/maintenance/routine-types` con código, nombre, intervalos, porcentaje y estado.

#### Scenario: Consulta del catálogo inicial
- **WHEN** se consulta `GET /api/v1/maintenance/routine-types` en una base recién creada
- **THEN** la respuesta contiene `OIL_CHANGE`, `BRAKES` y `TIRES` con los valores indicados

### Requirement: Validación de los parámetros de un tipo
Un tipo de rutina MUST tener al menos un intervalo (km o meses); cada intervalo presente MUST ser mayor que 0 y el porcentaje «por vencer» MUST cumplir `0 < % < 100`. Un cambio inválido MUST rechazarse con `400` sin modificar el tipo.

#### Scenario: Porcentaje fuera de rango
- **WHEN** se envía `PUT /api/v1/maintenance/routine-types/{id}` con `dueSoonThresholdPercent = 100`
- **THEN** la respuesta es `400` y el tipo conserva su porcentaje anterior

#### Scenario: Sin intervalos
- **WHEN** se envía un cambio con `defaultIntervalKm = null` y `defaultIntervalMonths = null`
- **THEN** la respuesta es `400`

### Requirement: Cambio de parámetros con reevaluación inmediata
Al actualizar un tipo de rutina, el servicio SHALL guardar quién y cuándo lo cambió y, en la misma operación, SHALL reevaluar todas las rutinas activas de ese tipo en planes activos que no tengan ajuste propio para el parámetro cambiado, usando la fecha efectiva `max(hoy, última revisión por tiempo del plan)`. Los eventos resultantes MUST publicarse en el stream `maintenance.routine`.

#### Scenario: Subir el porcentaje convierte una rutina en «por vencer»
- **WHEN** una rutina `OIL_CHANGE` sin ajuste propio está al 85 % de su intervalo de km y el porcentaje del tipo pasa de 10 % a 20 %
- **THEN** la rutina pasa a `DueSoon`, se publican `MaintenanceRoutineParametersChanged` y `MaintenanceRoutineDueSoon`, y se crea una alerta `DueSoon`

#### Scenario: El ajuste de la rutina prevalece sobre el tipo
- **WHEN** la rutina de aceite de EVA30D tiene ajuste de 20 % y el porcentaje del tipo `OIL_CHANGE` cambia a 15 %
- **THEN** el porcentaje efectivo de esa rutina sigue siendo 20 % y su estado no cambia

### Requirement: Concurrencia optimista en la edición del tipo
Dos ediciones simultáneas del mismo tipo de rutina MUST NOT sobrescribirse en silencio: la segunda MUST recibir `409` con `code = "Concurrency.Conflict"`.

#### Scenario: Edición concurrente
- **WHEN** dos usuarios editan `BRAKES` a partir de la misma versión
- **THEN** el primero guarda y el segundo recibe `409`
