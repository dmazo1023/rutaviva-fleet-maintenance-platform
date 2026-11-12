---
title: "[ADR-0031] ServiceOrders con réplicas por eventos, orden automática al vencer y aprobador autorizado"
status: "Accepted"
date_created: "2026-11-12"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "integration"
  - "service-orders"
related_specs:
  - "openspec/changes/service-orders-core"
related_plans: []
supersedes: ""
superseded_by: ""
---

> Resuelve las consecuencias NEG-001 y parte de NEG-002 del ADR-0030.

## Contexto

- En el ADR-0030 la orden recibía placa, cliente, taller y rutina desde la SPA, y cualquier persona podía aprobar.
- El reto pide además una orden por vencimiento, como máximo una abierta por rutina (RN-3), y que apruebe el dueño o el responsable del vehículo (RN-4).
- El cliente del EventBus admite un suscriptor por servicio, que se reconstruye desde su checkpoint. Al estrenarlo, el suscriptor recorre todo el historial de eventos.

## Decisión

- ServiceOrders mantiene réplicas propias de vehículos, talleres, rutinas y aprobadores, alimentadas por `VehicleRegistered`, `VehicleStatusChanged`, `VehicleResponsibleAssigned`, `WorkshopRegistered`, `ApproverRegistered`, `ApproverStatusChanged` y `MaintenanceRoutineRegistered`, igual que Fleet con sus referencias.
- `POST /api/v1/service-orders` recibe solo `vehicleId`, `routineId` y `createdBy`. El resto lo resuelve `ServiceOrderReferenceResolver` con las réplicas; un vehículo inactivo o una rutina de otro vehículo se rechazan.
- `MaintenanceRoutineOverdue` crea la orden automática (origen `Automatic`). Es idempotente: si la rutina ya tiene una orden abierta, el evento se descarta. Un índice único filtrado lo garantiza también en la base.
- La creación automática aplica solo a vencimientos ocurridos desde `ServiceOrders:AutomaticOrdersSince` (`AutomaticOrderPolicy`). Así el primer recorrido del historial construye las réplicas, pero no crea órdenes por vencimientos antiguos que ya se atendieron.
- Aprobar y rechazar exigen `approverId`. `ApprovalAuthorizationPolicy` acepta a un aprobador activo del cliente dueño del vehículo o a su responsable; si no cumple, el API responde 422 `ServiceOrder.ApproverNotAuthorized`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | La SPA ya no puede enviar datos inconsistentes del vehículo o la rutina | Positive |
| POS-002 | RN-3 queda completa: una orden automática por vencimiento y la creación manual cuando la anterior se canceló | Positive |
| POS-003 | RN-4 valida quién aprueba | Positive |
| NEG-001 | Una réplica recién creada tarda unos segundos en llegar; mientras tanto, crear la orden responde 404 `VehicleNotFound` o `RoutineNotFound` | Negative |
| NEG-002 | El tope de aprobación sigue siendo global; las políticas por cliente quedan para la siguiente fase | Negative |

## Alternativas consideradas

| Alternativa | Por qué se descartó |
|---|---|
| Consultar Fleet y Maintenance por HTTP al crear la orden | Acopla los servicios en tiempo de ejecución |
| Un segundo suscriptor que empiece en el último evento solo para la automatización | El cliente del EventBus admite un suscriptor por servicio |
| Reprocesar el historial y crear órdenes por todos los vencimientos | Crearía órdenes para rutinas que ya se atendieron |
