---
title: "[ADR-0021] Registrar la cita del taller como dato de la orden de servicio, sin módulo de agendamiento"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "architecture"
  - "ddd"
  - "scope"
  - "service-orders"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: ""
---

## Contexto

- El enunciado del reto declara **fuera de alcance** «elegir o agendar el taller». La orden se crea con el taller ya asignado al vehículo.
- El equipo considera necesario que la orden tenga **una cita con el taller**, para que el vehículo llegue a una hora específica antes de pasar a «En sitio» (ADR-0003).
- Un módulo de agendamiento completo (disponibilidad del taller, cupos, confirmación del cliente, inasistencias y recordatorios) es un subdominio en sí mismo y no cabe en el plazo de unas 28 horas.
- El dueño y el responsable deben enterarse de la cita y de sus cambios.

## Decisión

Registraremos la cita como un **objeto de valor `WorkshopAppointment`** dentro del agregado `ServiceOrder`, **sin un estado nuevo y sin módulo de agendamiento**. Lo presentaremos explícitamente como una **extensión del alcance** del reto:

- `WorkshopAppointment` (VO): `ScheduledAt` (`DateTimeOffset`, posterior al momento de registro según `IClock`) y `Notes` (opcional, hasta 250 caracteres).
- **Solo el taller** la registra (`ScheduleAppointment`) o la reprograma (`RescheduleAppointment`), y **solo en `Approved`**. La cita queda en firme, sin confirmación del cliente.
- **No se puede marcar `OnSite` sin una cita registrada.** La guarda vive en la tabla de transiciones del agregado.
- Cada registro o reprogramación queda en el **historial de la orden** (`ChangeType = AppointmentScheduled | AppointmentRescheduled`) y emite `ServiceOrderAppointmentScheduled`. Notifications lo envía al dueño y al responsable.
- Queda excluido: disponibilidad del taller, cupos por franja, confirmación del cliente, manejo de inasistencias y recordatorios automáticos.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | El flujo «Aprobada → cita → En sitio» queda trazable, con unos 3 archivos nuevos de dominio y 2 columnas en la orden | Positive |
| POS-002 | La máquina de estados no crece: sigue con 9 estados y la cita es una precondición de `MarkOnSite` | Positive |
| POS-003 | Si en el futuro se necesita agendamiento real, `WorkshopAppointment` es el punto de integración: un servicio de agenda (otro bounded context) podría proponer la cita por evento | Positive |
| NEG-001 | Se agrega alcance que el reto excluye. Se mitiga documentándolo como extensión y priorizándolo en el plan de recorte | Negative |
| NEG-002 | Sin control de cupos, el taller puede citar varios vehículos a la misma hora | Negative |
| NEG-003 | Sin confirmación del cliente, una cita que no le sirve se resuelve fuera del sistema (el taller la reprograma) | Negative |

## Alternativas consideradas

### ALT-001 — Estado «Agendada» con confirmación del dueño o del responsable

**Descripción**: El taller propone la cita y el cliente la confirma antes de que quede en firme.

**Rejection rationale**: Agrega un estado, dos transiciones y una pantalla de confirmación por una necesidad que el reto excluye. El equipo eligió la versión mínima.

### ALT-002 — Módulo o microservicio de agendamiento

**Descripción**: Disponibilidad, cupos, calendario del taller y recordatorios.

**Rejection rationale**: Es un subdominio completo, fuera de alcance y del plazo (YAGNI).

### ALT-003 — No registrar la cita

**Descripción**: Seguir el enunciado literalmente.

**Rejection rationale**: El equipo la considera necesaria para la operación real, y la versión mínima cuesta poco y no altera la máquina de estados.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Endpoint del taller: `PUT /api/v1/service-orders/{id}/appointment { scheduledAt, notes }`. El primer registro es `Schedule` y los siguientes son `Reschedule` |
| IMP-002 | Columnas `AppointmentAt` y `AppointmentNotes` en `serviceorders.ServiceOrders`, con un `CHECK` que exige `AppointmentAt` en `OnSite`, `InProgress` y `Completed` |
| IMP-003 | La vista del taller muestra un selector de fecha y hora en `Approved`. El botón «En sitio» se habilita solo con cita registrada (`availableActions`) |
| IMP-004 | En el plan de recorte, la cita va **después** de los servicios adicionales |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Enunciado del reto, sección «Fuera de alcance» |
| REF-002 | adr-0003-arch-service-order-state-machine · adr-0016-int-gateway-notifications-signalr |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0021` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
