## Why

RutaViva se compone de 5 microservicios de negocio, 2 de soporte y 3 técnicos (ADR-0010) que solo se comunican por eventos (ADR-0024). Antes de construir cualquier bounded context hace falta la base técnica común: bloques de construcción DDD, mensajería confiable (Outbox, EventBus, checkpoints), convenciones de API y las *fitness functions* que impiden que la arquitectura se degrade. Sin esa base, cada servicio reinventaría la misma infraestructura (copy-paste) y las reglas RN-3 y RN-5, que dependen de entrega *at-least-once* sin pérdidas, no tendrían garantía.

Corresponde al corte **S0** del análisis (§10).

## What Changes

- Solución .NET 10 `RutaViva.slnx` con la estructura de carpetas del análisis §9, `Directory.Build.props`, `Directory.Packages.props` (gestión central de versiones) y `.editorconfig` con `var` prohibido como error (ADR-0009 IMP-004).
- `BuildingBlocks.Domain`: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`, `Result`/`Result<T>`/`Error`, guardas de invariantes.
- `BuildingBlocks.Application`: abstracciones CQRS sin MediatR (`ICommandHandler`, `IQueryHandler`), `IUnitOfWork`, `IClock`, `IIntegrationEventPublisher`, `IIntegrationEventHandler<T>`, despacho de eventos de dominio y decoradores (validación y logging).
- `BuildingBlocks.Infrastructure`: `SaveChangesInterceptor` que despacha eventos de dominio y escribe el Outbox en la misma transacción (ADR-0011 IMP-002), `OutboxRelayService`, `EventCheckpoint`, convenciones EF Core (enums por nombre, `rowversion`, `datetimeoffset(0)`).
- `BuildingBlocks.EventBus.Client`: `EventBusPublisher` y `EventBusSubscriberHost` (bucle secuencial, aviso por SignalR, sondeo de respaldo, reintentos y *dead letter*) (ADR-0024 IMP-001).
- `BuildingBlocks.Api`: mapeo `Result` → `ProblemDetails`, `JsonStringEnumConverter`, correlación, versionado `/api/v1`.
- `RutaViva.IntegrationContracts`: envelope CloudEvents 1.0, nombres de streams, constantes del hub del EventBus y **todos los eventos de integración v1** del sistema (registro de contratos en `design.md`).
- Servicio **`RutaViva.EventBus.Hub`**: hub interno `/internal/hubs/events`, log `eventbus.Events` con único escritor, `eventbus.DeadLetters` (ADR-0024).
- `RutaViva.ServiceDefaults` (OpenTelemetry, health checks, service discovery) y `RutaViva.AppHost` de Aspire (ADR-0018) **(Recortable)**.
- Script de base: BD `RutaViva`, schemas y un usuario por servicio con `GRANT` propio y `DENY` al resto, generado en ciclo (data-model D-20); actualización del script de referencia a ADR-0024 (schema `eventbus`, `EventCheckpoints` en lugar de `InboxMessages`).
- Proyectos de pruebas transversales: `RutaViva.Architecture.Tests` (NetArchTest) y `RutaViva.IntegrationContracts.Tests` (*snapshots* JSON propios, ADR-0027).

## Capabilities

### New Capabilities

- `event-bus`: recepción, almacenamiento ordenado e idempotente y reparto de eventos de integración entre servicios, con lectura por offset y *dead letters*.
- `reliable-messaging`: publicación atómica con Transactional Outbox y consumo idempotente con checkpoint por suscriptor, reintentos y recuperación tras desconexión.
- `api-conventions`: convenciones observables de todas las APIs HTTP: versión, errores `ProblemDetails`, enums como texto, correlación y *health checks*.
- `architecture-governance`: reglas de dependencia y de estilo verificadas automáticamente en cada build (fitness functions).

### Modified Capabilities

(ninguna: el proyecto no tiene specs previas)

## Impact

- Código nuevo: `src/BuildingBlocks/*`, `src/Contracts/RutaViva.IntegrationContracts`, `src/Platform/EventBus/RutaViva.EventBus.Hub`, `src/Platform/RutaViva.ServiceDefaults`, `src/Platform/RutaViva.AppHost`, `tests/RutaViva.Architecture.Tests`, `tests/RutaViva.IntegrationContracts.Tests`, `deploy/local/sql/`.
- Documentación: `docs/architecture/database/rutaviva-reference-schema.sql` se alinea con ADR-0024.
- Dependencias NuGet: EF Core 10 (SqlServer), `Microsoft.AspNetCore.SignalR.Client`, FluentValidation, OpenTelemetry, Aspire, xUnit v3, AwesomeAssertions, NSubstitute, Microsoft.Data.SqlClient (BD de prueba desechable, ADR-0027), NetArchTest.Rules.
- Todos los changes posteriores (`maintenance-core`, `service-orders-core`, `supporting-contexts`, `edge-gateway-notifications`, `data-import`, `reporting`, `fleet-maintenance-ui`) dependen de este.

## Fuera de alcance

- Lógica de negocio de cualquier bounded context.
- Autenticación entre servicios (ADR-0024 NEG-006) y de usuarios (fuera del reto).
- Retención del log de eventos (ADR-0024 NEG-005).
- Varias instancias del EventBus o de un suscriptor (ADR-0024 NEG-003).

**Recortable** (orden del análisis §10): `RutaViva.AppHost` de Aspire (el sistema arranca también por servicio con el README).
