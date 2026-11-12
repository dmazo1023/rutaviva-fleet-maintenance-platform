---
title: "[ADR-0030] Incluir en el MVP un ServiceOrders mínimo con cotización, aprobación por tope y cierre por evento"
status: "Accepted"
date_created: "2026-11-12"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "scope"
  - "mvp"
  - "service-orders"
related_specs:
  - "openspec/changes/service-orders-core"
related_plans: []
supersedes: ""
superseded_by: ""
---

> Amplía el alcance del ADR-0029. El registro manual de servicio realizado en Maintenance se mantiene como alternativa del operador.

## Contexto

- El ADR-0029 dejó ServiceOrders para la siguiente fase por plazo. La orden de servicio, la cotización del taller y la aprobación de costos altos (RN-3 y RN-4) son el centro del reto.
- Maintenance ya consume `ServiceOrderCompletedV1` y reinicia la rutina (RN-5) con `RestartRoutineCommand`. El contrato `ServiceOrderSnapshot` y la pantalla de órdenes de la SPA ya existían.
- El diseño completo de `service-orders-core` (9 estados, citas, servicios adicionales, políticas por cliente) no cabe en el plazo; una rebanada vertical sí.

## Decisión

- Nuevo servicio `RutaViva.ServiceOrders` con las cuatro capas, schema `serviceorders` y el mismo patrón de los demás servicios (CQRS, `Result<T>`, Outbox y relay al EventBus).
- Agregado `ServiceOrder` con la tabla de transiciones como fuente única: `Created → Quoted → PendingApproval | Approved → InProgress → Completed`, y `PendingApproval → Cancelled` al rechazar. Cada cambio queda en el historial de la orden.
- RN-4 con la política de dominio `QuoteApprovalPolicy`: una cotización por encima del tope configurado (`ServiceOrders:AutoApprovalMaxAmount`, 1.000.000 COP) queda pendiente de aprobación; si no, se aprueba sola.
- Las acciones disponibles se calculan por estado y perfil (Taller cotiza, inicia y completa; Aprobador aprueba o rechaza).
- Al completar, la orden publica `ServiceOrderCompletedV1` con el kilometraje del servicio; Maintenance reinicia la rutina y resuelve sus alertas.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | El MVP recorre el ciclo del reto de punta a punta: alerta, orden, cotización, aprobación de costos altos, servicio y nuevo ciclo | Positive |
| POS-002 | Maintenance no cambió: el reinicio llega por el contrato de integración ya definido | Positive |
| NEG-001 | La orden recibe la placa, el cliente, el taller y la rutina desde la SPA, sin réplicas propias; un cliente podría enviar datos inconsistentes. En la fase siguiente ServiceOrders mantendrá réplicas de Fleet y Maintenance por eventos | Negative |
| NEG-002 | Un solo tope global en lugar de políticas por cliente, y el aprobador no se valida contra Customers | Negative |
| NEG-003 | Quedan fuera citas, llegada al taller, servicios adicionales y cancelación por el cliente | Negative |

## Alternativas consideradas

| Alternativa | Por qué se descartó |
|---|---|
| Mantener el ADR-0029 sin órdenes | Deja sin demostrar RN-3 y RN-4, el núcleo del reto |
| `service-orders-core` completo | No cabe en el plazo con pruebas y verificación de punta a punta |
| Validar vehículo y taller con llamadas HTTP síncronas a Fleet | Acopla los servicios en tiempo de ejecución; las réplicas por eventos son la solución objetivo |
