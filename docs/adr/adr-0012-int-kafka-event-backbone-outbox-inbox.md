---
title: "[ADR-0012] Usar Kafka como bus de eventos con Transactional Outbox, Inbox idempotente y envelope CloudEvents"
status: "Superseded"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "integration"
  - "kafka"
  - "outbox"
  - "inbox"
  - "cloudevents"
related_specs: []
related_plans: []
supersedes: "adr-0007"
superseded_by: "adr-0024"
---

> **Reemplazado por [ADR-0024](adr-0024-int-eventbus-signalr-sql-log.md)** (2026-11-10): se elimina Kafka y se usa un servicio EventBus propio (SignalR para avisar, SQL Server como log de eventos).

## Contexto

Este ADR reemplaza al ADR-0007, que publicaba los eventos solo en el log y en SignalR dentro de un proceso.

- El equipo usa **Kafka** en producción y dispone de un broker en Docker local.
- Con microservicios (ADR-0010), toda la integración entre contextos viaja por eventos.
- Kafka garantiza el orden **solo dentro de una partición**, y la entrega es *at-least-once* cuando el offset se confirma después de procesar.
- Escribir en la base de datos y publicar en Kafka son dos sistemas sin transacción distribuida. Si el proceso cae entre ambas operaciones, se pierden eventos (*dual-write problem*).
- La telemetría y la revisión diaria también se emulan como eventos de Kafka.
- MassTransit v9 adoptó licencia comercial. `Confluent.Kafka` es el cliente oficial con licencia Apache 2.0.

## Decisión

Adoptaremos **Kafka** (KRaft, un broker local) como bus de eventos, con estos patrones obligatorios:

**1. Topics y claves**

| Topic | Productor | Clave (orden garantizado por) | Consumidores |
|---|---|---|---|
| `rutaviva.customers.customer.v1` | Customers | `CustomerId` | Fleet, ServiceOrders |
| `rutaviva.workshops.workshop.v1` | Workshops | `WorkshopId` | Fleet |
| `rutaviva.fleet.vehicle.v1` | Fleet | `VehicleId` | Maintenance, ServiceOrders |
| `telemetry.odometer-readings.v1` | Externo / Simulators | `Plate` (la plataforma externa no conoce el `VehicleId` interno) | Maintenance, Reporting |
| `rutaviva.maintenance.time-evaluation-requested.v1` | Maintenance (scheduler) / Simulators | `asOfDate` | Maintenance |
| `rutaviva.maintenance.routine.v1` | Maintenance | `VehicleId` | ServiceOrders, Notifications |
| `rutaviva.maintenance.alert.v1` | Maintenance | `VehicleId` | Notifications |
| `rutaviva.serviceorders.service-order.v1` | ServiceOrders | `VehicleId` | Maintenance, Notifications |
| `{topic}.dlq` | Infraestructura de consumo | la original | operación manual |

Todos los topics tienen 3 particiones y factor de replicación 1 en local. Se nombran por **agregado**, no por evento, para preservar el orden de los eventos de un mismo vehículo.

**2. Transactional Outbox**: el evento de integración se escribe en `{schema}.OutboxMessages` **en la misma transacción** que el cambio de estado. Un `OutboxRelayService` (`BackgroundService`) publica los pendientes cada 500 ms con un productor idempotente (`EnableIdempotence = true`, `Acks = All`) y los marca como procesados.

**3. Inbox (consumidor idempotente)**: cada consumidor registra `(MessageId, ConsumerName)` en `{schema}.InboxMessages` **en la misma transacción** que su efecto. Si el mensaje ya existe, lo omite. El offset se confirma manualmente (`EnableAutoCommit = false`) **después** de confirmar la base de datos.

**4. Errores**: 3 reintentos con *backoff* exponencial (1 s, 2 s, 4 s). Si siguen fallando, el mensaje se publica en `{topic}.dlq` con los *headers* del error y se confirma el offset para no bloquear la partición.

**5. Envelope CloudEvents 1.0 (JSON, modo estructurado)**: `id` (= `MessageId`), `source` (`/rutaviva/{service}`), `type` (`rutaviva.serviceorders.ServiceOrderStatusChanged.v1`), `subject` (ID del agregado), `time` y `data`. Los contratos son `record` en `RutaViva.Contracts`, versionados por sufijo.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Cero eventos perdidos ante una caída entre la confirmación y la publicación: el relay los reenvía al reiniciar | Positive |
| POS-002 | Los mensajes duplicados (reinicio de un consumidor o reintento del productor) tienen 0 efectos repetidos gracias al Inbox | Positive |
| POS-003 | Todos los eventos de un vehículo se procesan en orden dentro de su partición, lo que reduce los conflictos de concurrencia optimista en Maintenance | Positive |
| POS-004 | Agregar un consumidor nuevo (por ejemplo, un servicio de facturación) supone 0 cambios en los productores | Positive |
| POS-005 | Los eventos son inspeccionables y se pueden emitir a mano con Kafka UI durante la demo | Positive |
| NEG-001 | Latencia adicional de hasta 500 ms por el *polling* del relay | Negative |
| NEG-002 | Dos tablas técnicas por schema (Outbox e Inbox) que requieren limpieza periódica (retención de 7 días) | Negative |
| NEG-003 | Sin Schema Registry, la compatibilidad de los contratos se protege solo con pruebas de contrato y versionado por sufijo | Negative |
| NEG-004 | Un mensaje en la DLQ requiere intervención manual. No hay reprocesamiento automático | Negative |

## Alternativas consideradas

### ALT-001 — MassTransit con el *rider* de Kafka

**Descripción**: Usar la abstracción de MassTransit, que trae Outbox incluido.

**Rejection rationale**: MassTransit v9 requiere licencia comercial, y la v8 deja de recibir soporte. Además, el *rider* de Kafka de MassTransit no cubre todos los escenarios de la versión para RabbitMQ y Azure Service Bus. Con `Confluent.Kafka` y unas 300 líneas de BuildingBlocks se controla todo el flujo.

### ALT-002 — Publicar directamente en Kafka desde el handler, sin Outbox

**Descripción**: El handler llama a `producer.ProduceAsync` después de `SaveChangesAsync`.

**Rejection rationale**: Es el *dual-write problem*. Si Kafka no está disponible o el proceso cae, el estado queda confirmado sin evento, y se pierde una orden (RN-3) o un aviso (RN-2).

### ALT-003 — Un topic por tipo de evento

**Descripción**: `service-order-quoted`, `service-order-approved`, etc.

**Rejection rationale**: Kafka no garantiza el orden entre topics. Un consumidor podría procesar `Completed` antes que `InProgress` de la misma orden. Con un topic por agregado y la clave por vehículo se conserva el orden.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | `IIntegrationEventPublisher.Enqueue(IIntegrationEvent)` solo escribe en el Outbox. Application nunca referencia `Confluent.Kafka` (prueba de arquitectura) |
| IMP-002 | `KafkaConsumerHost<TMessage>` en BuildingBlocks gestiona el bucle de consumo, la deserialización del envelope, el Inbox, los reintentos y la DLQ. Cada servicio solo implementa `IIntegrationEventHandler<TEvent>` |
| IMP-003 | Los grupos de consumidores se nombran `rutaviva-{service}`. Notifications usa `rutaviva-notifications-{instanceId}` para que cada instancia reciba todos los mensajes (ADR-0016) |
| IMP-004 | El script `deploy/local/create-topics.sh` crea los topics con `kafka-topics.sh --create --if-not-exists`. Se deshabilita `auto.create.topics.enable` |
| IMP-005 | Las trazas de OpenTelemetry se propagan con el *header* `traceparent` en cada mensaje para seguir el flujo en el dashboard (ADR-0018) |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Richardson, C. *Pattern: Transactional outbox* e *Idempotent Consumer* (microservices.io) |
| REF-002 | CloudEvents 1.0 Specification: Kafka Protocol Binding |
| REF-003 | Confluent docs: *Idempotent producer* y *Consumer offset management* |
| REF-004 | adr-0007-int-integration-events-signalr: decisión reemplazada |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0012` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
