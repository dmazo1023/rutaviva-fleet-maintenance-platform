---
title: "[ADR-0010] Descomponer RutaViva en microservicios por bounded context con Clean Architecture interna"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "architecture"
  - "microservices"
  - "bounded-context"
  - "clean-architecture"
  - "ddd"
related_specs: []
related_plans: []
supersedes: "adr-0001"
superseded_by: ""
---

> **Revisión 2026-11-10 (en estado Proposed):** se elimina Kafka y la comunicación entre servicios pasa al EventBus propio (ADR-0024). Se elimina el servicio Simulators y se agrega EventBus.

## Contexto

Este ADR reemplaza al ADR-0001, que proponía un monolito modular. Tras su redacción aparecieron drivers nuevos del equipo:

- La solución debe funcionar como un **CRM**: gestión de clientes, aprobadores, talleres, vehículos, rutinas, alertas y órdenes.
- El equipo necesita comunicación asíncrona entre servicios sin operar un broker externo, para poder desplegar también en AWS (ADR-0024). Tiene **SQL Server** disponible en localhost.
- Es una restricción explícita del equipo que **cada servicio tenga una única responsabilidad** y que no exista un monolito que concentre todo el sistema.
- Los módulos funcionales candidatos son 7: clientes, aprobadores, talleres, vehículos, rutinas y odómetro, alertas y órdenes.
- Varios de ellos comparten invariantes. Un aprobador no existe sin su cliente, y una alerta nace de la transición de estado de una rutina dentro de la misma regla RN-2.
- El plazo efectivo es de unas 30 horas.

Un *bounded context* es un límite de modelo y de lenguaje. Un microservicio es una unidad de despliegue. Cuando cada entidad se convierte en un servicio (antipatrón *entity services*), aparecen servicios anémicos y llamadas encadenadas para mantener invariantes que deberían vivir dentro de un mismo modelo.

## Decisión

Descompondremos el sistema en **5 microservicios de negocio, uno por bounded context**, 2 de soporte (DataImport y Reporting) y 3 componentes técnicos (Gateway, Notifications y EventBus):

| Servicio | Responsabilidad única | Tipo de subdominio | Agregados |
|---|---|---|---|
| **Customers** | Clientes y sus aprobadores | Soporte | `Customer` (contiene `Approver`) |
| **Workshops** | Catálogo de talleres | Soporte | `Workshop` |
| **Fleet** | Registro de vehículos y taller asignado | Soporte | `Vehicle` |
| **Maintenance** | Rutinas, odómetro, evaluación de vencimientos y alertas | **Core** | `MaintenancePlan` (contiene `MaintenanceRoutine`), `Alert` |
| **ServiceOrders** | Ciclo de vida de la orden, cotización y aprobación | **Core** | `ServiceOrder` |
| **DataImport** | Carga inicial desde CSV con validación (ADR-0022) | Soporte (onboarding) | `ImportJob` |
| **Reporting** | Proyecciones de lectura y reportes descargables (ADR-0023) | Genérico (read model) | — (proyecciones) |
| Gateway | Punto de entrada único (YARP) | Técnico | — |
| Notifications | Difusión en tiempo real de eventos a la web (SignalR) | Genérico | — |
| EventBus | Guardar y repartir eventos de integración (SignalR + log en SQL, ADR-0024). Solo red interna | Técnico | — (log `eventbus.Events`) |

- Cada servicio de negocio sigue **Clean Architecture** (`Domain`, `Application`, `Infrastructure`, `Api`) y **CQRS**, y es dueño exclusivo de su schema de base de datos (ADR-0013).
- **La comunicación entre servicios es solo por eventos del EventBus** (ADR-0024). No hay llamadas HTTP síncronas entre microservicios. **Única excepción documentada**: DataImport consume las APIs públicas de los dueños como lo haría cualquier cliente (ADR-0022, NEG-001). Los datos ajenos que un servicio necesita para validar se mantienen como **réplicas locales** alimentadas por eventos (*event-carried state transfer*).
- El código técnico común (clases base, Outbox, checkpoints y cliente del EventBus) vive en `BuildingBlocks`. Los **contratos de integración** viven en `RutaViva.IntegrationContracts`. **Ningún tipo de dominio se comparte** entre servicios.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Cada servicio tiene una sola razón de cambio de negocio. Por ejemplo, un cambio en la política de aprobación solo toca ServiceOrders | Positive |
| POS-002 | La caída de Customers, Workshops o Fleet no detiene la evaluación de vencimientos ni las órdenes, que trabajan con réplicas locales | Positive |
| POS-003 | Se ensamblan 5 servicios de negocio en lugar de 7. Se eliminan 2 integraciones (Customers↔Approvers y Maintenance↔Alerts) que habrían requerido coordinar invariantes por red | Positive |
| POS-004 | Los core domains (Maintenance y ServiceOrders) concentran el diseño rico. Los de soporte se mantienen deliberadamente delgados | Positive |
| NEG-001 | Hay 10 procesos .NET más SQL Server en local, lo que exige un orquestador local (ADR-0018) | Negative |
| NEG-002 | Consistencia eventual entre servicios: registrar un vehículo justo después de crear un cliente puede fallar con 422 durante una ventana de milisegundos a segundos | Negative |
| NEG-003 | Los BuildingBlocks compartidos acoplan la versión técnica de todos los servicios. Es aceptable en un solo repositorio, pero requeriría empaquetarlos como NuGet versionado si los equipos se separan | Negative |
| NEG-004 | Unas 6 horas más de esfuerzo que el monolito modular (proyectos, *hosting* y contratos), lo que obliga a aplicar el plan de recorte del análisis (§10) | Negative |

## Alternativas consideradas

### ALT-001 — Un microservicio por entidad (7 servicios)

**Descripción**: Customers, Approvers, Vehicles, Workshops, Maintenance, Alerts y ServiceOrders como servicios separados.

**Rejection rationale**: Separa invariantes que deben ser consistentes al mismo tiempo. Un aprobador pertenece a un cliente, y una alerta debe crearse exactamente una vez cuando la rutina cambia de estado. Esto obligaría a usar sagas o compensaciones para reglas que hoy son locales. Además, Approvers y Alerts serían servicios anémicos (antipatrón *entity services*).

### ALT-002 — Monolito modular (ADR-0001)

**Descripción**: Un solo desplegable con módulos internos.

**Rejection rationale**: Incumple la restricción explícita del equipo de no concentrar el sistema en un único componente. Tampoco permite demostrar la integración asíncrona por eventos entre contextos, que es la forma de trabajo habitual de la compañía.

### ALT-003 — Comunicación síncrona HTTP entre servicios

**Descripción**: Fleet consulta a Customers por HTTP antes de registrar un vehículo, y ServiceOrders consulta a Fleet antes de crear una orden.

**Rejection rationale**: Genera acoplamiento temporal: si Customers está caído, Fleet también falla. También produce cadenas de latencia y duplica en cada cliente la lógica de reintentos. Las réplicas por eventos resuelven la validación con datos locales.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Convención de proyectos: `RutaViva.{Service}.{Layer}`, por ejemplo `RutaViva.ServiceOrders.Domain` |
| IMP-002 | Siguen vigentes las convenciones de ADR-0001: sin MediatR, `Result<T>`, sin `var`, constantes para literales y sin comentarios en el código |
| IMP-003 | Las réplicas locales se llaman `{Entity}Reference` (por ejemplo, `ApproverReference`). Son de solo lectura para el dominio y las actualiza únicamente un consumidor |
| IMP-004 | Las pruebas de arquitectura verifican que ningún `RutaViva.{A}.*` referencie a `RutaViva.{B}.*`. Solo se permiten `BuildingBlocks` y `Contracts` |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Newman, S. (2021). *Building Microservices*, 2.ª ed.: descomposición por bounded context |
| REF-002 | Fowler, M. *Event-Carried State Transfer* (martinfowler.com, «What do you mean by Event-Driven?») |
| REF-003 | adr-0001-arch-clean-modular-monolith: decisión reemplazada |
| REF-004 | adr-0024-int-eventbus-signalr-sql-log · adr-0013-data-sqlserver-schema-per-service · adr-0018-proc-local-orchestration-aspire |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0010` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
