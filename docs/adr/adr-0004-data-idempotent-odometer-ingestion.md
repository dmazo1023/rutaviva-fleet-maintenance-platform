---
title: "[ADR-0004] Ingerir lecturas de odómetro de forma idempotente y monótona mediante una ACL y concurrencia optimista por vehículo"
status: "Superseded"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "data"
  - "idempotency"
  - "anti-corruption-layer"
  - "concurrency"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: "adr-0014"
---

## Contexto

La plataforma de telemetría es un sistema externo no controlado.

- Envía lecturas de odómetro en kilómetros por vehículo, y **a veces repite lecturas o las envía fuera de orden**.
- RN-6: el odómetro nunca disminuye. Una lectura menor o igual a la registrada **se ignora, sin error**.
- RN-2: cada lectura válida dispara la evaluación por kilómetros de todas las rutinas del vehículo.
- Pueden llegar dos lecturas del mismo vehículo de forma concurrente (reintentos del emisor o varias instancias).
- Responder con un error a una lectura duplicada provocaría reintentos del emisor, que es el comportamiento contrario al deseado.
- El modelo de la telemetría (sus nombres de campo, unidades y formato) no debe filtrarse al dominio.

## Decisión

Trataremos la ingesta como una operación **idempotente y monótona** con tres piezas:

1. **Anti-Corruption Layer** en la frontera: `POST /api/v1/odometer-readings` recibe el contrato externo (`TelemetryOdometerReadingRequest`), y `TelemetryReadingTranslator` lo traduce al command `RecordOdometerReadingCommand(VehicleId, Kilometers, ReadAt)`. Solo el traductor conoce el formato externo.
2. **Monotonía como regla de dominio**: `Vehicle.RecordOdometerReading(OdometerReading reading)` compara con `Odometer` y devuelve un resultado `OdometerReadingOutcome.Accepted | Ignored`. Si la lectura es ≤, **no muta el estado ni emite eventos**. Aceptar la misma lectura dos veces equivale a aceptarla una (idempotencia natural, sin tabla de deduplicación).
3. **Concurrencia optimista por agregado**: el repositorio compara `Vehicle.Version` al guardar. Ante un conflicto, el handler **reintenta hasta 3 veces** recargando el agregado. Tras el reintento, la lectura concurrente menor se resuelve como `Ignored` por la regla de dominio.

La respuesta HTTP es **`202 Accepted`** tanto para las lecturas aceptadas como para las ignoradas, con el cuerpo `{ "outcome": "Accepted" | "Ignored" }`. Las ignoradas quedan en un log estructurado (`OdometerReadingIgnored` con `VehicleId`, `ReceivedKm` y `CurrentKm`).

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Las lecturas duplicadas o fuera de orden no generan errores 4xx ni 5xx, así que el emisor no tiene motivo para reintentar | Positive |
| POS-002 | No se necesita un almacén de claves de idempotencia: la deduplicación cuesta O(1) (una comparación de enteros) | Positive |
| POS-003 | Dos lecturas concurrentes de un mismo vehículo producen exactamente una evaluación por cada valor de km que sea efectivamente mayor. Se verifica con una prueba que lanza 50 lecturas en paralelo | Positive |
| POS-004 | Un cambio en el formato de la telemetría impacta un solo archivo (`TelemetryReadingTranslator`) | Positive |
| NEG-001 | No se distingue una lectura duplicada de una fuera de orden: ambas son `Ignored`. Si el negocio necesitara auditar el desorden, haría falta conservar el historial de lecturas | Negative |
| NEG-002 | Con alta contención sobre un mismo vehículo, los reintentos (como máximo 3) agregan latencia. Es aceptable porque un vehículo emite del orden de una lectura por minuto | Negative |

## Alternativas consideradas

### ALT-001 — Clave de idempotencia por lectura (tabla de lecturas procesadas)

**Descripción**: Guardar un hash o identificador de cada lectura y descartar las repetidas.

**Rejection rationale**: No resuelve las lecturas fuera de orden con valores distintos (km 9.950 después de km 10.020), que RN-6 exige ignorar. La regla de monotonía ya cubre los duplicados exactos. Sería almacenamiento adicional sin un driver (YAGNI).

### ALT-002 — Responder `409 Conflict` o `400` a las lecturas menores o iguales

**Descripción**: Señalar al emisor que la lectura fue rechazada.

**Rejection rationale**: Contradice RN-6 («se ignora, sin error») y fomenta los reintentos del sistema externo.

### ALT-003 — Bloqueo pesimista (`SemaphoreSlim` por vehículo o `SELECT ... FOR UPDATE`)

**Descripción**: Serializar el procesamiento por vehículo con un bloqueo.

**Rejection rationale**: El bloqueo en memoria no sobrevive a varias instancias, y el bloqueo de base de datos acopla el dominio a la infraestructura. La concurrencia optimista funciona igual con el adaptador en memoria y con PostgreSQL (`xmin` o columna `version`).

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | `OdometerReading` es un VO: `Kilometers` (`long` ≥ 0) y `ReadAt` (`DateTimeOffset`). Un valor negativo no se puede construir |
| IMP-002 | El repositorio en memoria usa `ConcurrentDictionary<VehicleId, VehicleSnapshot>` y `TryUpdate(key, newValue, comparisonValue)` sobre la versión, sin bloqueos globales |
| IMP-003 | La política de reintento es un decorator del handler (`OptimisticConcurrencyRetryDecorator`), no un bucle dentro del caso de uso. El número máximo de reintentos es una constante con nombre |
| IMP-004 | Un vehículo inexistente responde `404 Not Found`. Esto no es parte de RN-6: es un error de contrato del emisor |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Evans, E. (2003). *Domain-Driven Design*, cap. 14: Anti-Corruption Layer |
| REF-002 | adr-0002-arch-aggregate-boundaries: `Vehicle` como dueño del odómetro y de las rutinas |
| REF-003 | adr-0005-data-persistence-ports-in-memory: adaptador de persistencia |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0004` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
