---
title: "[ADR-0007] Publicar eventos de integración a través de un puerto y difundirlos en tiempo real a la UI con SignalR"
status: "Superseded"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "integration"
  - "signalr"
  - "integration-events"
  - "outbox"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: "adr-0012"
---

## Contexto

- El sistema debe «avisar al cliente» cuando una rutina está por vencer (RN-2). El enunciado aclara que **basta con publicar el evento**: el envío real de avisos queda fuera de alcance.
- El cliente también debe enterarse cuando una cotización supera el umbral y requiere su aprobación (RN-4).
- La entrega incluye una interfaz de usuario, que en una demo debería reflejar los cambios provocados por otros actores (telemetría, taller y job diario) sin recargar la página.
- Los eventos de dominio (ADR-0002) son internos al bounded context y pueden cambiar de forma sin aviso. Un consumidor externo necesita un contrato estable.
- El stack de la compañía incluye SignalR.

## Decisión

Separaremos los **eventos de dominio** de los **eventos de integración** y los publicaremos a través de un puerto:

1. En Application, cada handler de un evento de dominio relevante lo traduce a un **evento de integración** versionado (por ejemplo, `MaintenanceRoutineDueSoonIntegrationEvent` v1) y lo entrega a **`IIntegrationEventPublisher`**.
2. En Infrastructure y Api, el puerto se implementa con un **publicador compuesto** (patrón Composite) que delega en:
   - `StructuredLogIntegrationEventPublisher`: registra cada evento con `EventName`, `EventVersion` y `AggregateId`. Es la «publicación» exigida.
   - `SignalRIntegrationEventPublisher`: difunde el evento a los clientes conectados a `MaintenanceNotificationsHub` (`/hubs/maintenance`) mediante `IHubContext<MaintenanceNotificationsHub, IMaintenanceNotificationsClient>` (hub fuertemente tipado).
3. Eventos de integración iniciales: `MaintenanceRoutineDueSoon`, `ServiceOrderCreated`, `ServiceOrderApprovalRequested`, `ServiceOrderStatusChanged` y `MaintenanceRoutineRestarted`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | El aviso «por vencer» y la cotización pendiente aparecen en la UI en menos de 1 s después de la lectura o la acción del taller, sin *polling* | Positive |
| POS-002 | Agregar un canal real (correo, Service Bus) es agregar un publicador al Composite: 0 cambios en los handlers (principio Abierto/Cerrado) | Positive |
| POS-003 | Application no referencia SignalR. La prueba de arquitectura lo verifica | Positive |
| POS-004 | El contrato de integración es independiente del modelo interno: renombrar un evento de dominio no rompe a la UI | Positive |
| NEG-001 | Sin Outbox, un evento puede perderse si el proceso cae entre la confirmación y la publicación. Es aceptable con persistencia en memoria, porque el estado también se pierde. Con una base de datos real se adoptaría el patrón Outbox | Negative |
| NEG-002 | SignalR no garantiza la entrega a los clientes desconectados. La UI recarga el estado por REST al reconectar (`onreconnected`) | Negative |
| NEG-003 | Hay dos jerarquías de eventos (dominio e integración) y sus traductores, unas 10 clases adicionales | Negative |

## Alternativas consideradas

### ALT-001 — *Polling* desde Angular cada N segundos

**Descripción**: La UI consulta `GET /service-orders` y `GET /vehicles` periódicamente.

**Rejection rationale**: Agrega una latencia de hasta N segundos y tráfico constante sin cambios. No demuestra la «publicación del evento» que pide el enunciado. Se conserva como plan B si falta tiempo (análisis §10).

### ALT-002 — Azure Service Bus o Event Grid como broker

**Descripción**: Publicar los eventos de integración en un broker gestionado.

**Rejection rationale**: No hay un segundo consumidor ni un segundo servicio en el alcance. Requiere una suscripción de Azure y conectividad durante la demo. Sería un martillo dorado frente al requisito de solo publicar el evento.

### ALT-003 — Publicar directamente los eventos de dominio por SignalR

**Descripción**: Enviar al hub las mismas clases de dominio.

**Rejection rationale**: Acopla la UI al modelo interno: un cambio en el agregado rompería el cliente. Además, la capa de dominio dependería de la serialización que necesita la presentación.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | `IMaintenanceNotificationsClient` define un método por evento (`RoutineDueSoon(RoutineDueSoonMessage message)`, etc.). No se envían cadenas mágicas con `SendAsync("...")` |
| IMP-002 | En Angular se usa `@microsoft/signalr` encapsulado en `MaintenanceNotificationsService`, que expone *signals* y reconexión automática con `withAutomaticReconnect()` |
| IMP-003 | Para mostrar en vivo el aviso «una sola vez por ciclo», la UI muestra un *toast* por cada evento recibido: dos lecturas seguidas en zona «por vencer» producen un solo *toast* |
| IMP-004 | Sin autenticación (fuera de alcance): todos los clientes reciben todos los eventos. La evolución es agrupar por `FleetId` (`Groups.AddToGroupAsync`) |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Microsoft Learn: *Strongly typed hubs in ASP.NET Core SignalR* |
| REF-002 | Richardson, C. *Transactional Outbox pattern* (microservices.io) |
| REF-003 | adr-0002-arch-aggregate-boundaries: eventos de dominio |
| REF-004 | adr-0008-tech-angular-22-frontend: consumidor del hub |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0007` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
