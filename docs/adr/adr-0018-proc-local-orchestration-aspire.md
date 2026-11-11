---
title: "[ADR-0018] Orquestar el entorno local con .NET Aspire AppHost sobre SQL Server localhost"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "process"
  - "aspire"
  - "opentelemetry"
  - "local-development"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: ""
---

## Contexto

- La solución tiene 10 procesos .NET (5 servicios de negocio, DataImport, Reporting, Gateway, Notifications y EventBus), la SPA Angular y SQL Server en localhost. No hay broker externo (ADR-0024).
- La demo se presenta desde un portátil, y arrancar todo debe ser rápido y repetible.
- Un flujo de negocio atraviesa hasta 5 procesos (Gateway → Maintenance → EventBus → ServiceOrders → EventBus → Notifications → Angular). Sin trazabilidad distribuida es difícil diagnosticar dónde se detuvo.
- El equipo ya tiene SQL Server levantado de forma independiente.

## Decisión

Usaremos un **AppHost de .NET Aspire** (`RutaViva.AppHost`) como orquestador local:

- Registra los 10 proyectos .NET y la SPA Angular (`AddJavaScriptApp` o `AddNpmApp`, según la versión de Aspire instalada) con sus dependencias (`WaitFor`).
- Usa los recursos existentes: SQL Server como `AddConnectionString("rutaviva-sql")` (el servidor local del equipo). Los servicios reciben la URL del EventBus por *service discovery* (`WithReference(eventBus)`).
- `RutaViva.ServiceDefaults` configura OpenTelemetry (trazas, métricas y logs), *health checks* (`/health`, `/alive`) y *service discovery* para YARP.
- El dashboard de Aspire muestra los logs estructurados y las trazas distribuidas de punta a punta, propagadas por la extensión `traceparent` del envelope CloudEvents (ADR-0024, IMP-003).

Sigue existiendo el `README` con el arranque manual por servicio como alternativa sin Aspire. Ya no se necesita Docker para la infraestructura de mensajería.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Un solo comando (`dotnet run --project src/Platform/RutaViva.AppHost`) levanta los 9 componentes en el orden correcto | Positive |
| POS-002 | Una lectura de odómetro se sigue en una sola traza, desde el productor hasta el push de SignalR, en la demo | Positive |
| POS-003 | Los puertos y las URLs se inyectan por *service discovery*: 0 URLs escritas en `appsettings` entre servicios | Positive |
| NEG-001 | Requiere el workload o las plantillas de Aspire y Docker en el portátil de la demo | Negative |
| NEG-002 | Aspire es una herramienta de desarrollo y no sustituye a los manifiestos de despliegue (AKS y ArgoCD), que quedan fuera de alcance | Negative |

## Alternativas consideradas

### ALT-001 — Docker Compose con todos los servicios en contenedores

**Descripción**: Un Dockerfile por servicio y un compose completo.

**Rejection rationale**: Construir 8 imágenes en cada cambio alarga el ciclo de desarrollo y no ofrece un dashboard de trazas sin configurar Jaeger u OTel Collector adicionales.

### ALT-002 — Arrancar cada proyecto a mano (`dotnet run` × 8)

**Descripción**: 8 terminales y la configuración de puertos a mano.

**Rejection rationale**: Es propenso a errores en la demo y no aporta observabilidad. Se mantiene solo como alternativa documentada.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Si el tiempo apremia, Aspire es el **tercer** elemento en el orden de recorte del plan (análisis §10). El sistema funciona igual sin él |
| IMP-002 | Las contraseñas de SQL Server se pasan como `AddParameter(..., secret: true)` desde *user-secrets* del AppHost |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Microsoft Learn: *.NET Aspire overview* e *integración de SQL Server* |
| REF-002 | adr-0010-arch-microservices-by-bounded-context · adr-0016-int-gateway-notifications-signalr |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0018` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
