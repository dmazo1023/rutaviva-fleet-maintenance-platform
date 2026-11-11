---
title: "[ADR-0009] Adoptar la pirámide de pruebas con TDD en el dominio, integración con Testcontainers, contratos de eventos y fitness functions"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "process"
  - "testing"
  - "tdd"
  - "testcontainers"
  - "fitness-functions"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: ""
---

> **Enmienda 2026-11-10:** las pruebas de integración usan el SQL Server local con una BD desechable por fixture en lugar de Testcontainers (ADR-0027).

> **Revisión 2026-11-10 (en estado Proposed):** se ajusta a microservicios, SQL Server y Kafka (ADR-0010, ADR-0012 y ADR-0013). Luego se reemplaza Kafka por el EventBus propio (ADR-0024).

## Contexto

- Los revisores evalúan las buenas prácticas, y la guía del plan carrera señala como debilidad previa la «claridad en tipos de pruebas y su aplicación práctica».
- Hay 6 reglas de negocio con casos borde explícitos y 13 supuestos documentados.
- Con microservicios, los riesgos nuevos son tres: la integración real con SQL Server (índices filtrados, `rowversion`), la entrega *at-least-once* del EventBus (Outbox y checkpoints) y la **compatibilidad de los contratos de eventos** entre productor y consumidor.
- La estructura de capas y la independencia entre servicios pueden degradarse si nada las verifica.

## Decisión

Adoptaremos la **pirámide de pruebas** con un propósito explícito por nivel:

| Proyecto (por servicio core) | Tipo | Qué verifica | Herramientas |
|---|---|---|---|
| `{Service}.Domain.UnitTests` | Unitaria **sin mocks**, con TDD | RN-1 a RN-6, VOs, máquina de estados, alertas y casos borde | xUnit v3, AwesomeAssertions |
| `{Service}.Application.UnitTests` | Unitaria con dobles | Handlers, traducción a eventos de integración y validators | xUnit v3, NSubstitute |
| `{Service}.Api.IntegrationTests` | Integración | Repositorios EF contra SQL Server real, índices únicos filtrados, Outbox y checkpoints con un EventBus real en proceso, evento duplicado = 1 efecto, suscriptor detenido que se pone al día | Testcontainers (MsSql), `WebApplicationFactory` (servicio y EventBus) |
| `RutaViva.IntegrationContracts.Tests` | Contrato | Cada contrato de `RutaViva.IntegrationContracts` serializa y deserializa con el envelope CloudEvents, y los cambios incompatibles fallan con un *snapshot* | xUnit + Verify (*snapshots* JSON) |
| `RutaViva.Acceptance.Tests` | Aceptación (BDD) | Un `.feature` por regla RN-n contra los servicios core | Reqnroll + Gherkin |
| `RutaViva.Architecture.Tests` | Fitness function | Regla de dependencia por servicio, ningún servicio referencia a otro, Application sin `Microsoft.AspNetCore` (incluido el cliente de SignalR) | NetArchTest.Rules |

Los nombres siguen `Method_Should_Result_When_Condition`, los datos se construyen con **Test Data Builders** y las fechas se pasan como dato (`AsOfDate`) o con `FakeClock`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Las pruebas unitarias y de arquitectura corren en menos de 15 s sin Docker, así que el ciclo TDD es rápido | Positive |
| POS-002 | Las invariantes de RN-2 y RN-3 se verifican tres veces: dominio, consumidor duplicado e índice en SQL Server | Positive |
| POS-003 | Un cambio incompatible en un evento falla en la PR del productor y no en producción del consumidor | Positive |
| POS-004 | Objetivo: ≥ 90 % de cobertura de líneas en el Domain de Maintenance y ServiceOrders, y ≥ 80 % en su Application | Positive |
| NEG-001 | Las pruebas de integración requieren Docker y tardan unos 60 s por servicio en levantar contenedores | Negative |
| NEG-002 | Los servicios de soporte (Customers, Workshops y Fleet) solo tienen pruebas de dominio y una de integración del camino feliz, por el plazo | Negative |
| NEG-003 | No se usa Pact: las pruebas de *snapshot* cubren la compatibilidad dentro de un solo repositorio, pero no entre repositorios | Negative |

## Alternativas consideradas

### ALT-001 — Pact (contratos dirigidos por el consumidor)

**Descripción**: Contratos publicados en un Pact Broker.

**Rejection rationale**: Todos los servicios viven en un solo repositorio y comparten `RutaViva.IntegrationContracts`. Los *snapshots* dan la misma señal sin un broker adicional.

### ALT-002 — Proveedor InMemory de EF y bus simulado en las pruebas de integración

**Descripción**: Evitar Docker en la integración.

**Rejection rationale**: El proveedor InMemory no soporta índices filtrados ni `rowversion`, y un bus simulado no reproduce las reentregas ni la recuperación desde el checkpoint. Justo lo que más importa verificar quedaría sin cubrir.

### ALT-003 — Omitir las pruebas de arquitectura

**Descripción**: Confiar en la revisión de código.

**Rejection rationale**: No es repetible. Con 5 servicios, un `ProjectReference` cruzado entre servicios es un error fácil de cometer y caro de detectar a mano.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Casos borde de RN-2: un intervalo de 10.000 km con el ciclo iniciado en 0 debe dar `UpToDate` en 8.999 km, `DueSoon` en 9.000 km y `Overdue` en 10.000 km |
| IMP-002 | Casos borde de RN-4 con una política de 1.500.000 COP: 1.500.000 se aprueba automáticamente y 1.500.001 pasa a `PendingApproval`. Aprueba un aprobador del dueño o el responsable del vehículo; uno de otro cliente recibe `ApproverNotAuthorized`. Cambiar la política no altera las órdenes ya cotizadas. Si el taller agrega un servicio en `OnSite`, la orden pasa a `PendingApproval` con prioridad `High`. Al aprobar o rechazar vuelve a `OnSite`, y el rechazo marca solo las líneas adicionales como `Rejected`. No se pueden agregar servicios en `Approved` ni en `InProgress`. Desactivar una rutina no cambia el estado de su orden abierta |
| IMP-003 | Prueba de integración clave: publicar 5 veces el mismo `MaintenanceRoutineOverdue` → exactamente 1 fila en `serviceorders.ServiceOrders` |
| IMP-004 | `var` prohibido: `.editorconfig` con `csharp_style_var_*= false:error` y `TreatWarningsAsErrors`. NetArchTest cubre las dependencias |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Cohn, M. (2009). *Succeeding with Agile*: pirámide de pruebas |
| REF-002 | Ford, N. et al. (2017). *Building Evolutionary Architectures*: fitness functions |
| REF-003 | Testcontainers for .NET: módulo MsSql |
| REF-004 | adr-0010 · adr-0011 · adr-0013 · adr-0024 |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0009` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
