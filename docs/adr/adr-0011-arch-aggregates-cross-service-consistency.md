---
title: "[ADR-0011] Definir agregados por servicio y garantizar RN-3 con transición única, consumidor idempotente e índice único filtrado"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "architecture"
  - "ddd"
  - "aggregate"
  - "eventual-consistency"
related_specs: []
related_plans: []
supersedes: "adr-0002"
superseded_by: ""
---

## Contexto

Este ADR reemplaza al ADR-0002, cuyos agregados vivían en un solo proceso. Con la descomposición en microservicios (ADR-0010), el vehículo, las rutinas, las alertas y las órdenes quedan en servicios distintos, y los eventos viajan por el EventBus con entrega *at-least-once* (ADR-0024; antes Kafka, ADR-0012).

- RN-2: el aviso «por vencer» se emite **una sola vez por ciclo**.
- RN-3: **máximo una orden abierta** por vehículo y rutina.
- RN-5: al completar una orden se reinicia la rutina.
- El EventBus puede entregar el mismo evento más de una vez (reintentos del Outbox, recuperación tras un reinicio), y un suscriptor puede reprocesar tras un reinicio.
- El registro del vehículo (placa, cliente y taller) pertenece a Fleet, pero Maintenance y ServiceOrders necesitan esos datos para operar.

## Decisión

Definiremos los agregados así:

| Servicio | Agregado (raíz) | Contenido | Referencias por ID |
|---|---|---|---|
| Maintenance | `MaintenancePlan` | `VehicleId` (identidad), `Odometer`, `MaintenanceRoutine[]`, `LastTimeEvaluationDate` | `CustomerId` |
| Maintenance | `Alert` | Tipo, origen (km o tiempo), ciclo y estado (ADR-0015) | `VehicleId`, `RoutineId` |
| Maintenance | `RoutineType` | Matriz de parámetros: intervalos por defecto y % «por vencer» (ADR-0019) | — |
| ServiceOrders | `ServiceOrder` | Estado, `ServiceLine[]`, copia de la política, `ServiceRecord` e historial de estados (ADR-0003) | `VehicleId`, `RoutineId`, `CustomerId`, `WorkshopId` |
| ServiceOrders | `ApprovalPolicy` | Monto de aprobación automática versionado (ADR-0019) | — |
| Fleet | `Vehicle` | Placa, VIN, datos, taller asignado, **estado** y **responsable** (ADR-0020) | `CustomerId` (dueño), `WorkshopId`, `ResponsibleApproverId` |
| Customers | `Customer` | Datos y `Approver[]` | — |

`MaintenancePlan` se crea en Maintenance al consumir `VehicleRegistered` de Fleet. Fleet es dueño del registro y Maintenance es dueño del plan.

**Dentro de un servicio**, los eventos de dominio se despachan **antes de confirmar** en la misma transacción de base de datos. Por ejemplo, `MaintenanceRoutineDueSoon` → crear `Alert` → escribir en el Outbox ocurren de forma atómica en Maintenance. **Entre servicios**, la integración es eventual vía Outbox → EventBus → checkpoint del suscriptor.

La invariante RN-3 se garantiza en **tres niveles** independientes:

1. **Dominio (Maintenance)**: la rutina transita a `Overdue` una sola vez por ciclo, y `MaintenanceRoutineOverdue` lleva `CycleNumber`.
2. **Suscriptor idempotente (ServiceOrders)**: el checkpoint `serviceorders.EventCheckpoints`, confirmado en la misma transacción que la orden, descarta los eventos con `Offset` ya procesado. El log del EventBus no admite `MessageId` repetidos (ADR-0024).
3. **Base de datos (ServiceOrders)**: el índice único filtrado `UX_ServiceOrders_OneOpenPerRoutine` sobre `(VehicleId, RoutineId)`, con `WHERE Status IN ('Created','Quoted','PendingApproval','Approved','OnSite','InProgress','OnHold')`. Cubre también las órdenes creadas manualmente (A-07). Si se viola, el consumidor lo trata como un duplicado: lo registra y no reintenta.

La garantía equivalente para RN-2 es el índice único `UX_Alerts_OnePerCycleAndType` sobre `(RoutineId, CycleNumber, Type)`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Un mismo `MaintenanceRoutineOverdue` entregado N veces produce exactamente 1 orden. Se verifica con una prueba de integración que publica el evento 5 veces | Positive |
| POS-002 | La alerta y el evento de integración se confirman en la misma transacción que el cambio de la rutina: no hay alertas sin evento ni eventos sin alerta | Positive |
| POS-003 | Cada servicio opera con sus réplicas locales aunque los demás estén caídos | Positive |
| NEG-001 | El despacho antes de confirmar crea dos agregados (`MaintenancePlan` y `Alert`) en una transacción. Es una excepción consciente a «una transacción por agregado», limitada a un mismo servicio | Negative |
| NEG-002 | Una orden aparece en ServiceOrders entre unos milisegundos y 1 s después del vencimiento (intervalo del relay del Outbox más la latencia del consumidor) | Negative |
| NEG-003 | Las réplicas pueden estar temporalmente desactualizadas. Por ejemplo, un aprobador revocado puede aprobar durante la ventana de propagación | Negative |

## Alternativas consideradas

### ALT-001 — Saga orquestada para el flujo «vencimiento → orden»

**Descripción**: Un orquestador coordina Maintenance y ServiceOrders con pasos y compensaciones.

**Rejection rationale**: El flujo no necesita compensaciones: si la orden no se crea, el evento se reintenta. Una coreografía de dos pasos con consumidores idempotentes cubre el requisito con menos piezas (KISS).

### ALT-002 — Consultar a ServiceOrders por HTTP antes de emitir el vencimiento

**Descripción**: Maintenance pregunta si existe una orden abierta antes de emitir el evento.

**Rejection rationale**: Es un *check-then-act* distribuido con condiciones de carrera, y además acopla temporalmente a Maintenance con ServiceOrders. Tampoco es necesario: Maintenance ya sabe que la rutina está `Overdue`.

### ALT-003 — Despachar los eventos de dominio después de confirmar (como en ADR-0002)

**Descripción**: Primero se guarda el plan y luego, en otra transacción, se crea la alerta.

**Rejection rationale**: Si el proceso cae entre las dos transacciones, la alerta se pierde. Con una base de datos real y Outbox disponible, la atomicidad dentro del servicio cuesta lo mismo y elimina esa ventana.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Concurrencia optimista con una columna `rowversion` en cada raíz de agregado (`IsRowVersion()` en EF Core 10) |
| IMP-002 | El `SaveChangesInterceptor` de BuildingBlocks recoge los eventos de dominio, ejecuta los handlers internos y escribe el Outbox antes de `base.SavingChangesAsync` |
| IMP-003 | `DbUpdateException` con el número de error 2601/2627 (clave duplicada) en `UX_ServiceOrders_OneOpenPerRoutine` se traduce a `DuplicateOpenServiceOrder` (resultado, no excepción de negocio) |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Vernon, V. (2013). *Implementing Domain-Driven Design*, cap. 10 (Aggregates) y cap. 13 (Integrating Bounded Contexts) |
| REF-002 | adr-0002-arch-aggregate-boundaries: decisión reemplazada |
| REF-003 | adr-0003-arch-service-order-state-machine: sigue vigente dentro de ServiceOrders |
| REF-004 | [Modelo de datos](../architecture/data-model.md): índices únicos filtrados |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0011` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
