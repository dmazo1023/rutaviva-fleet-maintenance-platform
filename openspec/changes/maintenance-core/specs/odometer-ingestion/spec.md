## Purpose

Recibir las lecturas de odómetro de la plataforma de telemetría externa, aislar su formato del modelo, aplicar la monotonía del odómetro (RN-6) y evaluar los vencimientos por km con cada lectura aceptada (ADR-0014, ADR-0025).

## ADDED Requirements

### Requirement: Endpoint de telemetría con respuesta de resultado
`POST /api/v1/maintenance/odometer-readings` SHALL recibir `{ plate, odometerKm, readAt }` (campos adicionales como `deviceId` se ignoran) y SHALL responder siempre `202 Accepted` con `{ outcome: "Accepted" | "Ignored", reason: null | "NotGreaterThanCurrent" | "UnknownVehicle" }` cuando el formato es válido. Un formato inválido (placa vacía, km negativo o no numérico, fecha ausente) MUST responder `400` con `ProblemDetails`.

#### Scenario: Formato inválido
- **WHEN** llega una lectura con `odometerKm = -5`
- **THEN** la respuesta es `400` y el plan no cambia

### Requirement: Monotonía del odómetro
Una lectura con km menores o iguales al odómetro registrado MUST ignorarse sin error, sin modificar el plan ni evaluar rutinas, con `reason = NotGreaterThanCurrent` y el log estructurado `OdometerReadingIgnored` con `VehicleId`, `ReceivedKm` y `CurrentKm` (RN-6).

#### Scenario: Casos de telemetría de la demo
- **WHEN** se envían en orden las lecturas de `samples/http/odometer-readings.http` después de la carga inicial
- **THEN** los resultados son: (1) RVA101 53.000 `Accepted`, aceite al 80 % `UpToDate`; (2) RVA101 53.000 `Ignored` `NotGreaterThanCurrent`; (3) RVA101 52.950 `Ignored` `NotGreaterThanCurrent`; (4) RVA101 54.100 `Accepted`, aceite `DueSoon` con alerta; (5) RVA101 55.050 `Accepted`, aceite `Overdue`, alerta `DueSoon` resuelta con `EscalatedToOverdue` y alerta `Overdue` creada; (6) RVA102 82.100 `Accepted`, aceite `Overdue`; (7) XYZ999 15.000 `Ignored` `UnknownVehicle`

### Requirement: Resolución por placa con anticorrupción
La lectura SHALL resolver el vehículo por su placa normalizada en mayúsculas y sin espacios. Una placa sin plan MUST responder `Ignored` con `reason = UnknownVehicle`, registrar el log `OdometerReadingForUnknownVehicle` y MUST NOT reintentarse en el servidor ni guardarse en el historial.

#### Scenario: Placa con minúsculas
- **WHEN** llega la lectura con `plate = " rva101 "`
- **THEN** se resuelve al plan de RVA101

### Requirement: Evaluación por km con la lectura aceptada
Una lectura aceptada SHALL actualizar el odómetro y su fecha y SHALL evaluar todas las rutinas activas de un plan activo con la fecha de la última revisión por tiempo, sin avanzar esa fecha. En un plan suspendido MUST actualizar el odómetro sin evaluar rutinas.

#### Scenario: La lectura no avanza el tiempo
- **WHEN** una rutina vencería por tiempo en una fecha posterior a la última revisión y llega una lectura aceptada
- **THEN** su estado por tiempo se calcula con la fecha de la última revisión y no cambia

### Requirement: Historial de lecturas
Cada lectura de un vehículo conocido SHALL guardarse en el historial con km, fecha reportada, fecha de recepción, resultado y motivo, en la misma transacción que el cambio del plan, y SHALL publicar `OdometerReadingRecorded`. `GET /api/v1/maintenance/plans/{vehicleId}/odometer-readings?limit=` SHALL devolverlas ordenadas por fecha de recepción descendente, con `limit` por defecto 50 y máximo 200 (ADR-0025).

#### Scenario: Pestaña «Lecturas»
- **WHEN** se consultan las lecturas de RVA101 después de los casos 1 a 5
- **THEN** se devuelven 5 lecturas, con la 2 y la 3 como `Ignored` y motivo `NotGreaterThanCurrent`

#### Scenario: Límite excedido
- **WHEN** se pide `limit=500`
- **THEN** la respuesta es `400`

### Requirement: Lecturas concurrentes del mismo vehículo
Dos lecturas simultáneas del mismo vehículo MUST resolverse por concurrencia optimista: la que pierde SHALL reintentarse una vez releyendo el plan y la monotonía decide su resultado. Si el reintento vuelve a chocar, MUST responder `409`.

#### Scenario: Dos lecturas paralelas
- **WHEN** llegan en paralelo 60.000 km y 60.100 km de un mismo vehículo con odómetro 59.000 km
- **THEN** el odómetro final es 60.100 km y ninguna lectura queda sin resultado
