---
title: "[ADR-0016] Exponer los servicios a la web con un Gateway YARP y difundir eventos en tiempo real con un servicio Notifications SignalR alimentado por el EventBus"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "integration"
  - "api-gateway"
  - "yarp"
  - "signalr"
  - "event-bus"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: ""
---

> **Revisión 2026-11-10 (en estado Proposed):** Notifications deja de consumir Kafka y se suscribe al EventBus (ADR-0024). El Gateway no expone el hub interno del EventBus.

## Contexto

- La SPA Angular necesita consumir 5 APIs de negocio, cada una en su propio puerto.
- El equipo quiere que la web **se actualice en tiempo real** cuando ocurren cambios, por ejemplo cuando el taller cambia el estado de una orden, se genera una alerta o una rutina se reinicia.
- Los cambios se publican como eventos en el EventBus (ADR-0024). Su hub es interno y no debe exponerse a los navegadores, porque permite publicar eventos.
- Si cada servicio expusiera su propio hub, Angular mantendría 5 conexiones WebSocket y 5 configuraciones de CORS.
- La autenticación está fuera de alcance.

## Decisión

Introduciremos dos componentes de borde, cada uno con una sola responsabilidad:

1. **`RutaViva.ApiGateway` (YARP)**: el único origen para Angular. Enruta `/api/customers/**`, `/api/workshops/**`, `/api/fleet/**`, `/api/maintenance/**` y `/api/service-orders/**` a su servicio, , `/api/imports/**` y `/api/reports/**` a su servicio, y `/hubs/**` a Notifications (con soporte de WebSockets). **No enruta `/internal/**`** (hub del EventBus). Centraliza CORS y la correlación de peticiones (`X-Correlation-Id`). **No contiene lógica de negocio ni agrega respuestas.**
2. **`RutaViva.Notifications.Hub`**: se suscribe en el EventBus a los streams `maintenance.routine`, `maintenance.alert` y `serviceorders.service-order`, empezando desde el último offset (los avisos viejos no le sirven a la web), y retransmite cada evento por un **hub fuertemente tipado** `NotificationsHub` (`INotificationsClient`) al **grupo del cliente** (`customer:{CustomerId}`). No persiste nada.

Flujo de ejemplo: el taller completa una orden → ServiceOrders (Outbox) → EventBus → Notifications → SignalR → Angular actualiza la fila de la orden y el estado de la rutina, sin recargar.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Angular conoce una sola URL base y una sola conexión WebSocket | Positive |
| POS-002 | Los servicios de negocio no referencian SignalR. Agregar un canal (correo, push) es agregar otro consumidor, con 0 cambios en los productores | Positive |
| POS-003 | Un cambio de estado hecho por el taller se ve en la web del cliente en menos de 1 s (relay de 500 ms más el consumo) | Positive |
| NEG-001 | Hay 2 procesos adicionales que operar | Negative |
| NEG-002 | Sin autenticación, cualquier cliente puede unirse a cualquier grupo con `JoinCustomer(customerId)`. La evolución es derivar el grupo del token | Negative |
| NEG-003 | Con varias instancias de Notifications, todas deben recibir todos los eventos (el EventBus lo permite porque Notifications no usa checkpoint) y se necesita un *backplane* de Redis para los navegadores | Negative |

## Alternativas consideradas

### ALT-001 — Angular llama directamente a cada servicio y cada servicio tiene su hub

**Descripción**: 5 URLs base y 5 hubs en Angular.

**Rejection rationale**: Duplica CORS y la configuración de SignalR en 5 servicios, y acopla la UI a la topología interna.

### ALT-002 — BFF con agregación de respuestas (GraphQL o endpoints compuestos)

**Descripción**: Un Backend for Frontend que combina datos de varios servicios.

**Rejection rationale**: Las vistas del CRM consultan un servicio a la vez. La agregación no tiene un driver hoy (YAGNI) y convertiría el gateway en un punto con lógica.

### ALT-003 — Long polling desde Angular

**Descripción**: Consultar periódicamente las APIs.

**Rejection rationale**: Los cambios se verían con N segundos de retraso y habría tráfico constante. Tampoco demuestra la integración con eventos.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Las rutas de YARP se definen en `appsettings.json` (`ReverseProxy:Routes/Clusters`), con los destinos resueltos por *service discovery* de Aspire (ADR-0018) |
| IMP-002 | Métodos de `INotificationsClient`: `ServiceOrderChanged`, `AlertChanged`, `RoutineChanged` y **`ApprovalRequested`** (con `Priority`). Las solicitudes `High` (servicios adicionales con el vehículo en sitio) se envían al grupo del dueño y al responsable (`approver:{ApproverId}`), y Angular las muestra como alerta prioritaria persistente. Los DTOs son propios de Notifications, no los contratos de integración |
| IMP-003 | Angular encapsula la conexión en `NotificationsService` (`withAutomaticReconnect()`), expone *signals* y recarga por REST al reconectar |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Microsoft Learn: *YARP: Yet Another Reverse Proxy* |
| REF-002 | Microsoft Learn: *Strongly typed hubs* y *Manage users and groups in SignalR* |
| REF-003 | adr-0024-int-eventbus-signalr-sql-log · adr-0008-tech-angular-22-frontend |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0016` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
