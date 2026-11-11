## Purpose

Evaluar los vencimientos por tiempo una vez al día, permitir disparar esa revisión para una fecha concreta en la demo y reevaluar a demanda con los parámetros vigentes en la base de datos (RN-2, ADR-0017, ADR-0019).

## ADDED Requirements

### Requirement: Revisión por tiempo monótona
La revisión SHALL evaluar los planes activos con la fecha efectiva `max(fecha pedida, última revisión del plan)` y el odómetro actual, y SHALL actualizar la última revisión del plan a esa fecha. Una fecha anterior a la última revisión MUST NOT retroceder el tiempo. Repetir una fecha ya evaluada MUST NOT producir eventos nuevos.

#### Scenario: Fechas de la demo
- **WHEN** después de los casos de telemetría se dispara la revisión del 2027-01-20 y luego la del 2027-02-10
- **THEN** el 2027-01-20 vencen por tiempo RVA103 aceite y EVA30D aceite, y el 2027-02-10 pasan RVA101 frenos a `DueSoon` (quedan 33 de 365 días) y EVA302 frenos a `Overdue`

#### Scenario: Fecha anterior después de una posterior
- **WHEN** después de evaluar el 2027-02-10 se pide la revisión del 2027-01-20
- **THEN** los planes se evalúan con 2027-02-10 y no se publica ningún evento nuevo

### Requirement: Disparo manual con resumen
`POST /api/v1/maintenance/time-evaluations { asOfDate }` SHALL ejecutar la revisión y responder `200` con `{ evaluatedPlans, dueSoon, overdue, ordersRequested }`, donde `ordersRequested` es el número de rutinas que pasaron a `Overdue`. `asOfDate` ausente o con formato inválido MUST responder `400`.

#### Scenario: Resumen de la revisión
- **WHEN** se dispara la revisión del 2027-01-20 con los 10 vehículos de la demo
- **THEN** la respuesta indica `evaluatedPlans = 10`, `overdue = 2` y `ordersRequested = 2`

### Requirement: Disparo automático diario
El servicio SHALL disparar la revisión con la fecha de hoy a las 00:05 en `America/Bogota` y, al arrancar, SHALL dispararla con la fecha de hoy para recuperar una revisión perdida.

#### Scenario: Arranque después de una caída nocturna
- **WHEN** el servicio arranca a las 08:00 sin haber ejecutado la revisión de las 00:05
- **THEN** ejecuta la revisión con la fecha de hoy

### Requirement: Planes suspendidos, rutinas inactivas y aislamiento de fallos
La revisión MUST omitir los planes suspendidos y las rutinas inactivas, SHALL procesar los planes en lotes de 100 y un fallo en un plan MUST registrarse (`TimeEvaluationFailed` con `VehicleId`) sin abortar el resto.

#### Scenario: Un plan falla
- **WHEN** la evaluación de un plan lanza un error en medio de un lote
- **THEN** los demás planes del lote se evalúan y el log contiene `TimeEvaluationFailed` con ese vehículo

### Requirement: Reevaluación a demanda (botón «Actualizar»)
`POST /api/v1/maintenance/reevaluations { vehicleId? }` SHALL reevaluar con los parámetros vigentes en la base de datos, el odómetro actual y la fecha efectiva `max(hoy, última revisión)`, para un vehículo o para todos los planes activos, y SHALL responder con el mismo resumen. Un `vehicleId` sin plan MUST responder `404`.

#### Scenario: Edición directa en SQL
- **WHEN** se cambia en SQL el `DueSoonThresholdPercent` de `BRAKES` a 25 % y se pulsa «Actualizar»
- **THEN** las rutinas de frenos que quedan dentro del nuevo umbral pasan a `DueSoon` y se publican sus eventos
