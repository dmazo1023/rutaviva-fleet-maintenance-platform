---
title: "[ADR-0001] Adoptar un monolito modular con Clean Architecture y DDD táctico"
status: "Superseded"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "architecture"
  - "clean-architecture"
  - "ddd"
  - "cqrs"
  - "dotnet-10"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: "adr-0010"
---

## Contexto

RutaViva necesita un sistema de mantenimiento preventivo con seis reglas de negocio (RN-1 a RN-6) y tres actores externos: el jefe de flota, el taller y la plataforma de telemetría. La entrega es una prueba técnica para el ascenso a Desarrollador Senior. Los revisores evalúan explícitamente la **claridad de la estructura** y de las decisiones de diseño, tomando como referencia los principios de arquitecturas limpias.

Fuerzas presentes:

- El plazo efectivo es de unas 36 horas de trabajo (2026-11-10 a 2026-11-12 05:00).
- La entrega se presenta desde un portátil, sin infraestructura en la nube garantizada.
- El enunciado permite persistir en memoria y deja fuera de alcance la autenticación, los pagos y el envío real de avisos.
- El stack habitual de la compañía es .NET, Angular, Azure Functions y SignalR.
- El dominio tiene un único lenguaje ubicuo («rutina», «orden», «cotización») compartido por los actores internos, y dos fuentes externas no controladas (telemetría y taller).
- La guía de estudio del plan carrera señala como debilidades previas la separación de capas, los conceptos de Bounded Context y Agregado, y la justificación de las decisiones con drivers.

## Decisión

Adoptaremos un **monolito modular de un solo desplegable** (`RutaViva.Api`, ASP.NET Core en .NET 10), organizado con **Clean Architecture** en cuatro proyectos (`Domain`, `Application`, `Infrastructure`, `Api`) y **DDD táctico** dentro de un único bounded context: *Preventive Maintenance*.

- **Domain**: los agregados `Vehicle` y `ServiceOrder`, los objetos de valor, los eventos de dominio, las políticas y las interfaces de repositorio. Sin dependencias de NuGet.
- **Application**: CQRS con un command o una query por caso de uso, organizado en *vertical slices* (`Vehicles/RecordOdometerReading/`). Incluye handlers, validators (FluentValidation), behaviors del pipeline (validación y logging) y puertos (`IClock`, `IUnitOfWork`, `IIntegrationEventPublisher`).
- **Infrastructure**: los adaptadores de los puertos, como los repositorios en memoria, el reloj del sistema y el publicador de eventos.
- **Api**: el adaptador de entrada con Minimal API, el hub de SignalR, el middleware de `ProblemDetails`, el job diario y el *composition root*.
- La regla de dependencia (Api → Application → Domain; Infrastructure → Application y Domain) se verifica con pruebas de arquitectura (NetArchTest), que actúan como *fitness function*.
- La telemetría y el taller se modelan como contextos externos: Anti-Corruption Layer para la telemetría y Open Host Service (`/api/v1`) para el taller.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Un solo proceso que arranca con `dotnet run` y una SPA. La demo no depende de Azure, Docker ni emuladores | Positive |
| POS-002 | Las 6 reglas de negocio se prueban en `Domain.UnitTests` sin mocks ni infraestructura; el objetivo es más de 90 % de cobertura de líneas en Domain | Positive |
| POS-003 | Los mismos casos de uso se pueden invocar desde HTTP, el job diario o un host futuro (Azure Functions) sin cambiar Application | Positive |
| POS-004 | Una violación de capas hace fallar `ArchitectureTests` en `dotnet test` antes del commit | Positive |
| NEG-001 | Cuatro proyectos de producción y cinco de pruebas suponen más *boilerplate* que una API de un solo proyecto (unos 20 archivos estructurales antes de la primera regla) | Negative |
| NEG-002 | El escalado es solo vertical: todo escala junto. Es aceptable sin un requisito de carga en el enunciado | Negative |
| NEG-003 | Sin MediatR, el despachador de commands y queries se implementa en el proyecto (unas 60 líneas) | Negative |

## Alternativas consideradas

### ALT-001 — Microservicios (servicio de Rutinas, servicio de Órdenes y Azure Function de ingesta)

**Descripción**: Tres desplegables independientes comunicados por Azure Service Bus, cada uno con su propio almacén.

**Rejection rationale**: El enunciado no tiene drivers de escalabilidad independiente ni de autonomía de equipos (Ley de Conway). Con persistencia en memoria, los procesos no comparten estado, así que haría falta un broker y una base de datos reales, que están fuera de alcance. Además, el costo operativo y el plazo de 36 horas lo hacen inviable. Sería optimización prematura y martillo dorado.

### ALT-002 — API en capas tradicional (Controllers → Services → Repositories) con modelo anémico

**Descripción**: Un proyecto con controllers, servicios con la lógica y entidades con solo propiedades.

**Rejection rationale**: Concentra las reglas en servicios «Dios» y produce un modelo de dominio anémico, que es un antipatrón explícito de las restricciones del reto. No permite demostrar agregados, invariantes ni eventos, que son el criterio de evaluación.

### ALT-003 — Dos bounded contexts separados (Maintenance Planning y Service Orders)

**Descripción**: Dos módulos con modelos y lenguajes propios, integrados por eventos de integración.

**Rejection rationale**: El lenguaje ubicuo es único. «Rutina» y «orden» no cambian de significado entre actores internos, y un bounded context se define por el límite del modelo y del lenguaje, no por el número de agregados. Separarlos crearía duplicación de modelos sin una fuerza que lo justifique. Queda como evolución si un equipo distinto asume las órdenes.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | No se usa MediatR: desde la v13 tiene licencia comercial. Se usan interfaces propias `ICommandHandler<TCommand, TResult>` y `IQueryHandler<TQuery, TResult>`, más decorators para los behaviors |
| IMP-002 | Errores de negocio esperados: `Result<T>` en Application. Las invariantes violadas en el dominio usan `DomainException`, que el middleware traduce a `409 Conflict` con `ProblemDetails` |
| IMP-003 | Las convenciones obligatorias en C# son: sin `var`, tipos explícitos, constantes para los literales de texto, métodos con una sola responsabilidad y sin comentarios en el código. Se refuerzan con `.editorconfig` (`csharp_style_var_* = false:error`) y se compila con `TreatWarningsAsErrors` |
| IMP-004 | Los nombres de código, clases y logs estructurados van en inglés. La documentación va en español |
| IMP-005 | Las pruebas de arquitectura verifican que Domain no referencie `Microsoft.*` ni `System.Net.*`, y que Application no referencie `Microsoft.AspNetCore.*` |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Martin, R. C. (2017). *Clean Architecture*. Prentice Hall |
| REF-002 | Evans, E. (2003). *Domain-Driven Design*. Addison-Wesley |
| REF-003 | [Análisis del reto](../architecture/analisis-del-reto.md): drivers, mapa de contextos y C4 |
| REF-004 | adr-0002-arch-aggregate-boundaries: límites de los agregados dentro de este bounded context |
| REF-005 | adr-0009-proc-testing-strategy-fitness-functions: pruebas que protegen esta estructura |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0001` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
