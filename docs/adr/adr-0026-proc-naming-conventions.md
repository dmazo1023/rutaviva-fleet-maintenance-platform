---
title: "[ADR-0026] Nombrar proyectos, carpetas y la aplicación web según el tipo de componente que exponen"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "process"
  - "naming"
  - "conventions"
related_specs:
  - "openspec/changes/foundation-platform"
related_plans: []
supersedes: ""
superseded_by: ""
---

> Precisa la nota IMP-001 del ADR-0010 (`RutaViva.{Service}.{Layer}`) y los nombres de componentes de los ADR-0008, 0009, 0016, 0018 y 0024.

## Contexto

- La solución tiene 5 servicios de negocio, 2 de soporte, 3 componentes técnicos, librerías compartidas, una SPA y más de 20 proyectos de pruebas.
- Los nombres anteriores no decían qué tipo de componente era cada uno: `RutaViva.Gateway`, `RutaViva.Notifications`, `RutaViva.EventBus` y `RutaViva.Contracts`. La SPA se llamaba `rutaviva-crm`, un nombre genérico que no identifica el dominio.
- El equipo exige nombres claros y descriptivos que indiquen si el componente es una API, un hub, una UI u otro tipo.
- La solución la revisan evaluadores externos: la estructura de carpetas y los nombres son la primera explicación de la arquitectura.

## Decisión

| Elemento | Regla | Ejemplos |
|---|---|---|
| Proyecto .NET | `RutaViva.{Componente}.{Capa o tipo}` | `RutaViva.Maintenance.Domain`, `RutaViva.Maintenance.Api` |
| Host desplegable | El sufijo dice qué expone: `.Api` (REST), `.Hub` (SignalR) y `ApiGateway` (proxy inverso) | `RutaViva.Fleet.Api`, `RutaViva.EventBus.Hub`, `RutaViva.Notifications.Hub`, `RutaViva.ApiGateway` |
| Librería compartida | El nombre dice qué contiene. Se prohíben `Common`, `Shared`, `Utils` y `Core` | `RutaViva.IntegrationContracts`, `RutaViva.BuildingBlocks.EventBus.Client` |
| Pruebas de un proyecto | `{proyecto bajo prueba}.{UnitTests\|IntegrationTests}` | `RutaViva.Maintenance.Domain.UnitTests`, `RutaViva.Maintenance.Api.IntegrationTests` |
| Pruebas transversales | `RutaViva.{Ámbito}.Tests` | `RutaViva.Architecture.Tests`, `RutaViva.IntegrationContracts.Tests`, `RutaViva.Acceptance.Tests` |
| Aplicación web | `rutaviva-{dominio}-ui`, en kebab-case según la convención de Angular | `rutaviva-fleet-maintenance-ui` |
| Carpetas de `src/` | Por tipo de componente: `BuildingBlocks`, `Contracts`, `Services`, `Gateways`, `Platform`, `Web` | `src/Platform/EventBus/RutaViva.EventBus.Hub` |
| Excepciones | `RutaViva.AppHost` y `RutaViva.ServiceDefaults` conservan la convención de .NET Aspire, que los desarrolladores reconocen | — |

Los componentes técnicos con lógica propia se separan en capas igual que los servicios de negocio: `RutaViva.EventBus.{Application|Infrastructure|Hub}` y `RutaViva.Notifications.{Application|Hub}`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | El tipo de cada componente se lee en su nombre: un evaluador distingue la API REST, los hubs SignalR, el proxy y la UI sin abrir el código | Positive |
| POS-002 | Las pruebas de arquitectura pueden seleccionar ensamblados por sufijo (`.Api`, `.Hub`, `.Application`) para aplicar reglas por tipo | Positive |
| POS-003 | El proyecto de pruebas de un ensamblado se encuentra de forma directa por su nombre | Positive |
| NEG-001 | Nombres más largos (`RutaViva.Notifications.Hub.IntegrationTests`) | Negative |
| NEG-002 | Los ADRs anteriores se actualizan (siguen en estado Proposed) y los reemplazados conservan los nombres de su época | Negative |

## Alternativas consideradas

### ALT-001 — Sufijo `.Api` para todo host desplegable

**Descripción**: `RutaViva.Gateway.Api`, `RutaViva.EventBus.Api`, `RutaViva.Notifications.Api`.

**Rejection rationale**: Es uniforme, pero engaña: el EventBus y Notifications exponen hubs SignalR, no una API REST, y el Gateway no es una API propia.

### ALT-002 — Conservar los nombres anteriores

**Descripción**: `RutaViva.Gateway`, `RutaViva.Notifications`, `RutaViva.EventBus`, `RutaViva.Contracts`, `rutaviva-crm`.

**Rejection rationale**: No cumple la exigencia del equipo de que el nombre indique el tipo de componente, y `rutaviva-crm` no describe el dominio.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | La estructura completa está en `openspec/changes/foundation-platform/design.md` (D-01 y D-01b) |
| IMP-002 | `RutaViva.Architecture.Tests` verifica que todo proyecto en `src/Services` termine en `.Domain`, `.Application`, `.Infrastructure` o `.Api`, y que no existan ensamblados con `Common`, `Shared`, `Utils` o `Core` en el nombre |
| IMP-003 | Los espacios de nombres coinciden con el nombre del proyecto y la carpeta (regla IDE0130 como error) |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Microsoft Learn: *Names of namespaces* y *.NET Aspire project structure* |
| REF-002 | Angular style guide: nombres de proyecto en kebab-case |
| REF-003 | adr-0010-arch-microservices-by-bounded-context (IMP-001) |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0026` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
