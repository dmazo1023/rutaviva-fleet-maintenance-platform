---
title: "[ADR-0025] Registrar el historial de lecturas de odómetro (aceptadas e ignoradas) como log de solo inserción fuera del agregado MaintenancePlan"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "data"
  - "odometer"
  - "audit-log"
  - "cqrs"
related_specs:
  - "openspec/changes/maintenance-core"
related_plans: []
supersedes: ""
superseded_by: ""
---

> Enmienda la nota IMP-003 del ADR-0014 («no se guarda el historial de lecturas»). El resto del ADR-0014 sigue vigente.

## Contexto

- El mockup aprobado (v3) tiene la pestaña **«Lecturas»** en el detalle del vehículo. Muestra las lecturas recibidas y tacha las ignoradas por RN-6 con su motivo (`mockups/crm-flota.handoff.md`, §5, bloque B2).
- El ADR-0014 IMP-003 descartó guardar el historial por YAGNI, y el handoff dejó la contradicción como pregunta abierta.
- El equipo decidió conservar la pestaña: hace visible en la demo que la monotonía (RN-6) funciona, sin depender de leer logs.
- Guardar las lecturas dentro de `MaintenancePlan` haría crecer el agregado sin límite y obligaría a cargar todo el historial en cada lectura nueva.

## Decisión

Registraremos cada lectura de un vehículo conocido en **`maintenance.OdometerReadings`**, un **log de solo inserción** que **no forma parte del agregado** `MaintenancePlan`:

| Columna | Tipo | Nota |
|---|---|---|
| `Id` | `bigint identity` | PK |
| `VehicleId` | `uniqueidentifier` | FK a `MaintenancePlans` |
| `Kilometers` | `bigint` | ≥ 0 |
| `ReadAt` | `datetimeoffset(0)` | Fecha reportada por la telemetría |
| `ReceivedAt` | `datetimeoffset(0)` | Fecha de recepción |
| `Outcome` | `varchar(20)` | `OdometerReadingOutcome` (`Accepted`, `Ignored`) |
| `IgnoredReason` | `varchar(30)` nullable | `OdometerReadingIgnoredReason` (`NotGreaterThanCurrent`); nulo si `Accepted` |

- El handler de `RecordOdometerReadingCommand` inserta la fila **en la misma transacción** que el cambio del plan, con el resultado que devolvió el dominio. El dominio sigue siendo el único que decide `Accepted` o `Ignored`.
- Las lecturas de **vehículos desconocidos** (`UnknownVehicle`) **no** se guardan: no hay `VehicleId` y siguen yendo solo al log estructurado (ADR-0014, regla 5).
- Consulta: `GET /api/v1/maintenance/plans/{vehicleId}/odometer-readings?limit=50`, ordenada por `ReceivedAt DESC`, con un límite máximo de 200.
- Cada lectura aceptada o ignorada de un vehículo conocido publica `OdometerReadingRecorded` en el stream `maintenance.routine`, para que la pestaña se actualice por SignalR.
- Índice `IX_OdometerReadings_VehicleId_ReceivedAt (VehicleId, ReceivedAt DESC)`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | La demo muestra la regla RN-6 en la UI: la lectura repetida y la fuera de orden aparecen tachadas con su motivo | Positive |
| POS-002 | El agregado `MaintenancePlan` no crece: el historial es un modelo de lectura aparte (CQRS) | Positive |
| POS-003 | Queda una auditoría de la telemetría por vehículo sin depender de la retención de los logs | Positive |
| NEG-001 | Una fila por lectura. Con una lectura por minuto y vehículo, son unas 1.440 filas diarias por vehículo. Sin retención en esta versión (la evolución es purgar por fecha) | Negative |
| NEG-002 | Un evento de integración más por lectura en el EventBus, que tiene un único escritor (ADR-0024 NEG-002). Es aceptable con el volumen de la demo | Negative |

## Alternativas consideradas

### ALT-001 — Quitar la pestaña «Lecturas»

**Descripción**: Mantener el ADR-0014 tal cual; el resultado solo se ve en la respuesta HTTP y en los logs.

**Rejection rationale**: El equipo quiere que la regla RN-6 sea visible en la UI durante la sustentación.

### ALT-002 — Colección de lecturas dentro de `MaintenancePlan`

**Descripción**: El agregado guarda sus lecturas.

**Rejection rationale**: Agregado sin límite de tamaño, más memoria y más conflictos de concurrencia. El historial no protege ninguna invariante (Vernon: agregados pequeños).

### ALT-003 — Guardar solo las lecturas ignoradas

**Descripción**: Registrar solo las que RN-6 descarta.

**Rejection rationale**: La pestaña perdería el contexto (no se vería contra qué lectura se comparó la ignorada).

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Puerto `IOdometerReadingLog` en Application (`AppendAsync`) y `IOdometerReadingQueries` para la consulta. Implementaciones con EF Core en Infrastructure |
| IMP-002 | La entidad de persistencia `OdometerReadingEntry` no es una entidad de dominio: no tiene comportamiento y vive en Infrastructure |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | adr-0014-data-odometer-ingestion-acl: decisión que se enmienda (IMP-003) |
| REF-002 | `mockups/crm-flota.handoff.md` §5 y §10 |
| REF-003 | Vernon, V. (2013). *Implementing Domain-Driven Design*, cap. 10: Design small aggregates |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0025` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
