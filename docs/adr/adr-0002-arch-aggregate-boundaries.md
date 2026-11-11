---
title: "[ADR-0002] Modelar Vehicle (con sus rutinas) y ServiceOrder como agregados separados con consistencia eventual"
status: "Superseded"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "architecture"
  - "ddd"
  - "aggregate"
  - "domain-events"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: "adr-0011"
---

## Contexto

Las reglas de negocio involucran tres conceptos: el vehículo con su odómetro, las rutinas de mantenimiento y las órdenes de servicio.

- RN-1 y RN-2: cada lectura de odómetro reevalúa **todas** las rutinas del vehículo, y la revisión diaria reevalúa las rutinas por tiempo.
- RN-2: el aviso «por vencer» se emite **una sola vez por ciclo** de la rutina.
- RN-3: al vencer una rutina se crea una orden. **Para el mismo vehículo y la misma rutina no puede haber más de una orden abierta.**
- RN-4: la orden tiene su propio ciclo de vida, con acciones del taller y del cliente.
- RN-5: al completar la orden, la rutina se reinicia con el km y la fecha reales.
- RN-6: el odómetro nunca disminuye.
- La plataforma de telemetría envía lecturas repetidas o fuera de orden, y pueden llegar lecturas concurrentes del mismo vehículo.
- Un vehículo tiene pocas rutinas (del orden de 3 a 10). Las órdenes de un vehículo crecen sin límite a lo largo de los años.

Un agregado es una unidad de consistencia transaccional: se modifica solo a través de su raíz y cada transacción modifica un único agregado.

## Decisión

Modelaremos **dos agregados**:

1. **`Vehicle`** (raíz), que contiene el `Odometer` (VO), el `WorkshopId` asignado y la colección de `MaintenanceRoutine` (entidades internas). Es dueño de RN-1, RN-2 y RN-6, y del **estado del ciclo** de cada rutina.
2. **`ServiceOrder`** (raíz), que referencia `VehicleId` y `RoutineId` **por ID**. Es dueño de RN-4 y del registro del servicio real (RN-5).

La invariante «máximo una orden abierta» (RN-3) se garantiza en dos niveles:

- **En el dominio**: la rutina solo transita a `Overdue` **una vez por ciclo** y emite `MaintenanceRoutineOverdue` en esa transición. Las lecturas repetidas o la coincidencia de la evaluación por km y por tiempo no vuelven a emitir el evento, porque la rutina ya está `Overdue`.
- **Como red de seguridad en la persistencia**: el repositorio de órdenes rechaza una segunda orden abierta para el mismo par `(VehicleId, RoutineId)`. En el esquema relacional esto es un índice único filtrado (ver ADR-0005).

La integración entre agregados es **eventualmente consistente dentro del proceso**, mediante eventos de dominio despachados después de confirmar la unidad de trabajo:

- `MaintenanceRoutineOverdue` → `CreateServiceOrderOnRoutineOverdueHandler` → `ServiceOrder.Create(...)`.
- `ServiceOrderCompleted` → `RestartRoutineOnServiceOrderCompletedHandler` → `Vehicle.RestartRoutine(routineId, servicedAt, servicedOdometer)`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Una lectura de odómetro modifica exactamente 1 agregado (`Vehicle`) en 1 transacción, sin bloquear las órdenes | Positive |
| POS-002 | La deduplicación de órdenes no depende de consultas previas del tipo «¿existe una orden abierta?», que son propensas a carreras. Depende de una transición de estado única protegida por la concurrencia optimista del agregado | Positive |
| POS-003 | `Vehicle` no carga su historial de órdenes: el tamaño del agregado queda acotado al número de rutinas | Positive |
| POS-004 | Cada regla tiene un solo dueño, verificable con pruebas unitarias de dominio sin mocks | Positive |
| NEG-001 | Entre la confirmación del `Vehicle` y la creación de la orden hay una ventana de inconsistencia (milisegundos en el proceso). Si el proceso cae en esa ventana, la orden se pierde. Se mitiga con Outbox cuando haya una base de datos real (ADR-0007) | Negative |
| NEG-002 | El estado de la rutina (`Overdue`) y el de la orden se mantienen en dos agregados. Hay que mostrar ambos en la UI mediante una query de lectura que los combine | Negative |

## Alternativas consideradas

### ALT-001 — Un agregado único `Vehicle` que contiene rutinas y órdenes

**Descripción**: `Vehicle` es la raíz de todo y la invariante RN-3 se valida en memoria sobre la colección de órdenes.

**Rejection rationale**: El agregado crece sin límite con el historial de órdenes. Las acciones del taller (cotizar, iniciar, completar) competirían por el mismo bloqueo que la ingesta de telemetría, que es de alta frecuencia. Viola la regla de mantener los agregados pequeños: el límite se define por lo que debe ser consistente al mismo tiempo, no por lo que está relacionado.

### ALT-002 — `MaintenanceRoutine` como agregado independiente

**Descripción**: Cada rutina es una raíz y el vehículo solo guarda el odómetro.

**Rejection rationale**: Cada lectura tendría que modificar N agregados (una rutina por agregado) en una transacción, o aceptar que el odómetro y las rutinas no sean consistentes entre sí. La regla RN-6 (descartar lecturas ≤) y la reevaluación de las rutinas deben ser atómicas.

### ALT-003 — Garantizar RN-3 solo con una consulta previa en el handler

**Descripción**: Antes de crear la orden, el handler consulta si ya existe una abierta.

**Rejection rationale**: Es una condición de carrera clásica (*check-then-act*): dos lecturas concurrentes pueden pasar la verificación a la vez. Además, saca una regla de negocio del dominio. Se conserva solo como red de seguridad, no como mecanismo principal.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | `AggregateRoot` expone `IReadOnlyCollection<IDomainEvent> DomainEvents` y `ClearDomainEvents()`. La `IUnitOfWork` despacha los eventos después de `SaveChangesAsync` |
| IMP-002 | `Vehicle` expone un `Version` (entero) para la concurrencia optimista. El repositorio en memoria lo compara al guardar (ver ADR-0004) |
| IMP-003 | `MaintenanceRoutine` conserva `CurrentCycle` (`RoutineCycle`: `StartOdometer`, `StartDate`) y `DueSoonNotified` (booleano por ciclo). `RestartRoutine` crea un ciclo nuevo y reinicia la marca |
| IMP-004 | Las colecciones internas se exponen como `IReadOnlyCollection<T>`. No existe `vehicle.Routines.Add(...)` desde fuera: se usa `vehicle.AddRoutine(...)` |
| IMP-005 | Supuesto A-07: si una orden se cancela, la rutina permanece `Overdue` y no se genera otra orden automáticamente (comportamiento posterior a la cancelación fuera de alcance) |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Vernon, V. (2011). *Effective Aggregate Design*, partes I a III |
| REF-002 | adr-0003-arch-service-order-state-machine: ciclo de vida de `ServiceOrder` |
| REF-003 | adr-0004-data-idempotent-odometer-ingestion: concurrencia y monotonía del odómetro |
| REF-004 | adr-0005-data-persistence-ports-in-memory: índice único filtrado como red de seguridad |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0002` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
