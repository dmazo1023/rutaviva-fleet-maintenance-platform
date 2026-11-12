---
title: "[ADR-0029] Entregar el MVP con el registro de servicio realizado en Maintenance y dejar ServiceOrders para la siguiente fase"
status: "Accepted"
date_created: "2026-11-11"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "scope"
  - "mvp"
  - "maintenance"
  - "service-orders"
related_specs:
  - "openspec/changes/maintenance-core"
  - "openspec/changes/service-orders-core"
related_plans: []
supersedes: ""
superseded_by: ""
---

> Decisión de alcance tomada por plazo de entrega. No reemplaza a los ADR-0003 ni ADR-0011, que siguen describiendo la solución objetivo.

## Contexto

- El reto pide detectar cuándo le toca mantenimiento a cada vehículo, crear la orden de servicio para el taller y permitir aprobar los costos altos.
- Al cierre del plazo están implementados y probados Maintenance (RN-1, RN-2, RN-5 y RN-6), Customers, Workshops, Fleet, el EventBus y la SPA. Construir ServiceOrders según `openspec/changes/service-orders-core` (máquina de 9 estados, aprobación por monto, autorización del aprobador y RN-3 en tres niveles) se estimó en 2,5 a 4 horas, con riesgo de no terminar.
- Maintenance ya reinicia la rutina cuando recibe `ServiceOrderCompletedV1` (RN-5), con `RestartRoutineCommand`, su handler e idempotencia probada.

## Decisión

- El MVP cierra el ciclo de mantenimiento dentro de Maintenance: el operador registra el **servicio realizado** de una rutina (km y fecha) con `POST /api/v1/maintenance/plans/{vehicleId}/routines/{routineId}/services`, que ejecuta el mismo `RestartRoutineCommand` que usará ServiceOrders. La rutina inicia un nuevo ciclo y sus alertas se resuelven con el motivo `RoutineRestarted`.
- Cada registro manual lleva un identificador propio en el lugar del id de la orden, de modo que el evento `MaintenanceRoutineRestartedV1` conserva su contrato.
- ServiceOrders (cotización del taller, aprobación de costos altos y estados de la orden) queda para la siguiente fase, con su diseño completo ya versionado en `service-orders-core` y en los ADR-0003 y ADR-0011. La sección de órdenes de la SPA queda visible como próxima fase.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | El MVP demuestra el ciclo completo de mantenimiento: lectura de odómetro, vencimiento, alerta, servicio realizado y nuevo ciclo | Positive |
| POS-002 | Cuando exista ServiceOrders, el reinicio llegará por `ServiceOrderCompletedV1` sin cambiar Maintenance: el comando y el evento ya son los definitivos | Positive |
| NEG-001 | Sin ServiceOrders no hay cotización ni aprobación de costos altos (RN-3 y RN-4) en esta entrega | Negative |
| NEG-002 | El registro manual permite reiniciar una rutina sin orden; en la fase siguiente el endpoint quedará restringido al perfil Operador o se retirará | Negative |

## Alternativas consideradas

| Alternativa | Por qué se descartó |
|---|---|
| ServiceOrders completo (25 tareas) | 3,5 a 4 horas con riesgo de corte por límite de uso antes de la entrega |
| ServiceOrders mínimo (21 tareas) | 2,5 a 3 horas; el margen restante no cubría la integración ni la prueba de punta a punta |
| Insertar los datos de la demo por SQL | Los estados de las rutinas y las alertas los calcula el dominio; se cargan con los endpoints masivos |
