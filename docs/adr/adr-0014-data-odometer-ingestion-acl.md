---
title: "[ADR-0014] Ingerir lecturas de odómetro por un endpoint HTTP de Maintenance con una ACL, resolución por placa y regla de monotonía en el dominio"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "data"
  - "http"
  - "anti-corruption-layer"
  - "idempotency"
related_specs: []
related_plans: []
supersedes: "adr-0004"
superseded_by: ""
---

> **Revisión 2026-11-10 (en estado Proposed):** al eliminar Kafka (ADR-0024), la lectura deja de llegar por el topic `telemetry.odometer-readings.v1` y llega por HTTP. La ACL, la resolución por placa, la monotonía y el tratamiento del vehículo desconocido se mantienen.

## Contexto

Este ADR reemplaza al ADR-0004, que recibía las lecturas por HTTP en un monolito con persistencia en memoria.

- La plataforma de telemetría reporta lecturas de odómetro identificando el vehículo **por su placa**. No conoce el `VehicleId` interno.
- La plataforma repite lecturas o las envía fuera de orden. RN-6 establece que una lectura menor o igual a la registrada **se ignora sin error**.
- El formato de la telemetría es externo y no está bajo control de RutaViva.
- La plataforma también reporta vehículos que no están registrados en RutaViva.
- Ya no hay broker de mensajes (ADR-0024). La lectura debe llegar directamente al servicio dueño del odómetro, Maintenance.
- Dos lecturas del mismo vehículo pueden llegar en paralelo.

## Decisión

Ingeriremos la telemetría en **Maintenance** con el endpoint `POST /api/v1/maintenance/odometer-readings`, expuesto por el Gateway. Aplica estas reglas:

1. **Anti-Corruption Layer**: el endpoint recibe el contrato externo (`TelemetryOdometerReadingRequest { plate, odometerKm, readAt }`), y `TelemetryReadingTranslator` lo convierte en `RecordOdometerReadingCommand(VehicleId, Kilometers, ReadAt)`. Solo el traductor conoce el formato externo.
2. **Resolución por placa**: el traductor resuelve `Plate → VehicleId` con `maintenance.MaintenancePlans.Plate` (índice único).
3. **Monotonía en el dominio**: `MaintenancePlan.RecordOdometerReading(...)` devuelve `Accepted` o `Ignored`. `Ignored` no muta el estado ni emite eventos y deja un log estructurado `OdometerReadingIgnored` con `VehicleId`, `ReceivedKm` y `CurrentKm`. Como la regla es idempotente por naturaleza, una lectura reenviada no necesita clave de idempotencia.
4. **Respuesta siempre `202 Accepted`**, con el resultado en el cuerpo: `{ "outcome": "Accepted" | "Ignored", "reason": null | "NotGreaterThanCurrent" | "UnknownVehicle" }`. La plataforma no debe reintentar una lectura que el negocio decidió ignorar. Un formato inválido responde `400` con `ProblemDetails`.
5. **Vehículo desconocido** (A-13, acordado con el equipo): la lectura se ignora con `reason = UnknownVehicle` y se registra `OdometerReadingForUnknownVehicle`. No se reintenta en el servidor: la siguiente lectura del vehículo, una vez su réplica exista, corrige el odómetro.
6. **Concurrencia**: dos lecturas paralelas del mismo plan chocan en `rowversion`. El command se reintenta una vez releyendo el plan, y la monotonía decide el resultado.
7. **Plan suspendido** (vehículo inactivo, ADR-0020): la lectura actualiza el odómetro si es mayor, pero **no evalúa** las rutinas ni emite alertas.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Las lecturas duplicadas o fuera de orden no producen errores ni reintentos: se ignoran en O(1) | Positive |
| POS-002 | La plataforma recibe el resultado de cada lectura en la misma respuesta, sin esperar a un consumidor | Positive |
| POS-003 | Un cambio de formato en la telemetría afecta 2 archivos (el request y el traductor) | Positive |
| POS-004 | En la demo, las lecturas duplicadas y fuera de orden se envían desde la UI, Postman o un archivo `.http`, y se ve el `outcome` en la respuesta | Positive |
| NEG-001 | La ingesta es síncrona: si Maintenance está caído, la plataforma recibe un error de conexión y debe reintentar. No hay broker que amortigüe picos | Negative |
| NEG-002 | Una lectura que llega antes de que Maintenance tenga la réplica del vehículo (milisegundos a segundos tras registrarlo) se ignora. La siguiente lectura corrige el odómetro | Negative |
| NEG-003 | Con muchas lecturas paralelas del mismo vehículo, el reintento por `rowversion` puede fallar dos veces y responder `409`. Es improbable: un vehículo emite del orden de una lectura por minuto | Negative |

## Alternativas consideradas

### ALT-001 — Topic de Kafka particionado por placa (versión previa de este ADR)

**Descripción**: La telemetría se publica en `telemetry.odometer-readings.v1` y Maintenance la consume.

**Rejection rationale**: Kafka se eliminó de la solución (ADR-0024).

### ALT-002 — Publicar la lectura en el EventBus y consumirla en Maintenance

**Descripción**: Un endpoint receptor publica en un stream `telemetry.odometer-reading` del EventBus.

**Rejection rationale**: La telemetría es el flujo de mayor volumen, y el único escritor del EventBus limita el rendimiento (ADR-0024, NEG-002). Además, agrega un salto sin aportar desacoplamiento: Maintenance es el único interesado.

### ALT-003 — Reordenar las lecturas por `ReadAt` antes de procesarlas (ventana de tiempo)

**Descripción**: Acumular las lecturas en una ventana y procesarlas ordenadas por fecha.

**Rejection rationale**: RN-6 se define por km, no por fecha. Una ventana introduce latencia y estado adicional sin cambiar el resultado de negocio (YAGNI).

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | `OdometerReading` es un VO con `Kilometers` (`long` ≥ 0) y `ReadAt` (`DateTimeOffset`) |
| IMP-002 | Cada lectura aceptada reevalúa por km todas las rutinas del plan y puede emitir `MaintenanceRoutineDueSoon` o `MaintenanceRoutineOverdue`, que salen al EventBus por el Outbox |
| IMP-003 | La lectura aceptada actualiza `maintenance.MaintenancePlans.OdometerKm` y `OdometerReadAt`. No se guarda el historial de lecturas (YAGNI) |
| IMP-004 | Enums: `OdometerReadingOutcome` (`Accepted`, `Ignored`) y `OdometerReadingIgnoredReason` (`NotGreaterThanCurrent`, `UnknownVehicle`) |
| IMP-005 | `samples/http/odometer-readings.http` reproduce los 7 casos de la demo con sus resultados esperados |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Evans, E. (2003). *Domain-Driven Design*, cap. 14: Anti-Corruption Layer |
| REF-002 | adr-0004-data-idempotent-odometer-ingestion: decisión reemplazada |
| REF-003 | adr-0024-int-eventbus-signalr-sql-log: eliminación de Kafka |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0014` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
