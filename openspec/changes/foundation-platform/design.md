## Context

Proyecto *greenfield*: el repositorio solo contiene ADRs, el modelo de datos, el mockup aprobado y datos de ejemplo. Motivación en `proposal.md`. Este change fija la base técnica y, sobre todo, el **registro de contratos** (§ Registro de contratos) que los otros siete changes consumen sin redefinir. Si un change posterior necesita un contrato nuevo, lo agrega aquí antes de implementarlo.

### Precedencia ante contradicciones de la documentación

Regla: prevalece el ADR vigente más reciente. Contradicciones detectadas y su resolución:

| # | Contradicción | Resolución |
|---|---|---|
| C-01 | El análisis §1, §7, §8 y `samples/README.md` hablan de Kafka, topics y Simulators | ADR-0024 elimina Kafka; ADR-0010 elimina Simulators. La telemetría y la revisión por fecha llegan por HTTP (ADR-0014, ADR-0017). `samples/kafka/*.jsonl` se reemplaza por `samples/http/*.http` (change `maintenance-core`) |
| C-02 | `data-model.md` y el script SQL usan `InboxMessages` | ADR-0024 §5: `EventCheckpoints(SubscriberName, LastOffset)` reemplaza a Inbox. El script de referencia se actualiza (tarea 8.2) |
| C-03 | A-13 dice «se reintenta unos segundos»; ADR-0014 regla 5 dice que no se reintenta en el servidor | ADR-0014: se ignora con `UnknownVehicle` y log estructurado, sin reintento |
| C-04 | Análisis §10 S2 dice «8 estados» | ADR-0003: 9 estados (incluye `OnSite`) |
| C-05 | Rutas mezcladas: `/api/v1/alerts`, `/api/v1/routine-types`, `/api/v1/approval-policies` frente a los prefijos por servicio del Gateway (ADR-0016) | Decisión D-09: todas las rutas llevan el segmento del servicio. Ver tabla de endpoints |
| C-06 | ADR-0014 IMP-003 no guarda historial de lecturas; el mockup tiene la pestaña «Lecturas» | ADR-0025 (nuevo): log `maintenance.OdometerReadings` |
| C-07 | El script SQL no tiene schema `eventbus` | Se agrega `eventbus.Events` y `eventbus.DeadLetters` con su usuario `rutaviva_eventbus_app` |

## Goals / Non-Goals

**Goals:**
- Un único lugar para cada preocupación técnica transversal (DRY), con cero conocimiento de negocio.
- Que agregar un servicio nuevo sea: crear sus 4 proyectos, declarar sus streams y handlers, y nada más.
- Que las garantías de RN-3 y RN-5 (exactamente un efecto por evento) se prueben una vez en la base y se hereden.

**Non-Goals:**
- Un *framework* genérico reutilizable fuera de RutaViva (YAGNI).
- Varias instancias por servicio o particionado del EventBus.

## Decisions

### D-01 Estructura de la solución

```
RutaViva.slnx
Directory.Build.props        net10.0, Nullable, ImplicitUsings, TreatWarningsAsErrors, LangVersion 14
Directory.Packages.props     versiones centralizadas
.editorconfig                csharp_style_var_* = false:error; IDE0008 error (IDE0007 en none) y EnforceCodeStyleInBuild
src/
  BuildingBlocks/
    RutaViva.BuildingBlocks.Domain/
    RutaViva.BuildingBlocks.Application/
    RutaViva.BuildingBlocks.Infrastructure/
    RutaViva.BuildingBlocks.EventBus.Client/
    RutaViva.BuildingBlocks.Api/
  Contracts/
    RutaViva.IntegrationContracts/
  Services/{Context}/
    RutaViva.{Context}.Domain/
    RutaViva.{Context}.Application/
    RutaViva.{Context}.Infrastructure/
    RutaViva.{Context}.Api/                    REST (Minimal API)
  Gateways/
    RutaViva.ApiGateway/                       YARP
  Platform/
    EventBus/
      RutaViva.EventBus.Application/
      RutaViva.EventBus.Infrastructure/
      RutaViva.EventBus.Hub/                   SignalR interno (/internal/hubs/events)
    Notifications/
      RutaViva.Notifications.Application/
      RutaViva.Notifications.Hub/              SignalR público (/hubs/notifications)
    RutaViva.AppHost/                          Aspire
    RutaViva.ServiceDefaults/                  Aspire
  Web/
    rutaviva-fleet-maintenance-ui/             Angular 22
tests/
  RutaViva.Architecture.Tests/
  RutaViva.IntegrationContracts.Tests/
  RutaViva.Acceptance.Tests/
  RutaViva.Testing.Fixtures/
  BuildingBlocks/RutaViva.BuildingBlocks.UnitTests/
  Gateways/RutaViva.ApiGateway.IntegrationTests/
  Platform/RutaViva.EventBus.Application.UnitTests/
  Platform/RutaViva.EventBus.Hub.IntegrationTests/
  Platform/RutaViva.Notifications.Application.UnitTests/
  Platform/RutaViva.Notifications.Hub.IntegrationTests/
  Services/{Context}/RutaViva.{Context}.{Domain.UnitTests|Application.UnitTests|Api.IntegrationTests}/
deploy/local/sql/            00-database.sql, 01-schemas-and-users.sql (ciclo D-20)
```

Alternativa descartada: un `.sln` por servicio. Rechazada porque las pruebas de arquitectura necesitan ver todos los ensamblados y el plazo no justifica varias soluciones.

### D-01b Convención de nombres (ADR-0026)

| Elemento | Regla | Ejemplo |
|---|---|---|
| Proyecto .NET | `RutaViva.{Componente}.{Capa o tipo}` | `RutaViva.Maintenance.Api` |
| Sufijo del host desplegable | Dice qué expone: `.Api` (REST), `.Hub` (SignalR), `ApiGateway` (proxy) | `RutaViva.Notifications.Hub` |
| Librería compartida | Dice qué contiene, no «Common» ni «Shared» | `RutaViva.IntegrationContracts` |
| Proyecto de pruebas | `{proyecto bajo prueba}.{UnitTests|IntegrationTests}` o `RutaViva.{Ámbito}.Tests` para las transversales | `RutaViva.Maintenance.Api.IntegrationTests` |
| Aplicación web | `rutaviva-{dominio}-ui` en kebab-case | `rutaviva-fleet-maintenance-ui` |
| Carpeta de primer nivel en `src/` | Por tipo de componente: `BuildingBlocks`, `Contracts`, `Services`, `Gateways`, `Platform`, `Web` | — |
| Excepciones | `RutaViva.AppHost` y `RutaViva.ServiceDefaults` conservan la convención de Aspire | — |

### D-02 Bloques de dominio (`RutaViva.BuildingBlocks.Domain`)

```csharp
public abstract class Entity<TId> where TId : notnull { public TId Id { get; protected init; } }
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    protected void Raise(IDomainEvent domainEvent);
    public void ClearDomainEvents();
}
public abstract record ValueObject;                          // igualdad estructural por record
public interface IDomainEvent { DateTimeOffset OccurredAt { get; } }
public sealed record Error(string Code, string Message, ErrorType Type);
public enum ErrorType { Validation = 1, NotFound = 2, Conflict = 3, BusinessRule = 4 }
public class Result { bool IsSuccess; Error Error; static Result Success(); static Result Failure(Error error); }
public sealed class Result<T> : Result { T Value; static implicit operator Result<T>(T value); static implicit operator Result<T>(Error error); }
```

- Los identificadores son *strongly typed IDs* por servicio (`readonly record struct VehicleId(Guid Value)`), definidos en el Domain de cada servicio, no aquí.
- Las reglas de negocio devuelven `Result`/`Result<T>` con un `Error` de catálogo (`static class ServiceOrderErrors { public static readonly Error InvalidTransition = ... }`). Las excepciones quedan para errores de programación (guardas de argumentos nulos).
- Alternativa descartada: excepciones de dominio para el flujo de negocio. Rechazada por ADR-0001/0010 IMP-002 (`Result<T>`) y porque convierten reglas esperadas en control de flujo por excepciones.

### D-03 CQRS sin MediatR (`RutaViva.BuildingBlocks.Application`)

```csharp
public interface ICommand<TResult>;
public interface IQuery<TResult>;
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{ Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken); }
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{ Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken); }
public interface IUnitOfWork { Task<Result> SaveChangesAsync(CancellationToken cancellationToken); }
public interface IClock { DateTimeOffset UtcNow { get; } DateOnly TodayIn(TimeZoneInfo timeZone); }
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{ Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken); }
public interface IIntegrationEventMapper<in TDomainEvent> where TDomainEvent : IDomainEvent
{ IReadOnlyCollection<IIntegrationEvent> Map(TDomainEvent domainEvent); }
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{ Task<Result> HandleAsync(TEvent integrationEvent, EventMetadata metadata, CancellationToken cancellationToken); }
public interface IIntegrationEventPublisher
{ void Enqueue(IIntegrationEvent integrationEvent, string subject); }
public interface IEventCheckpointStore
{
    Task<long> GetLastOffsetAsync(string subscriberName, CancellationToken cancellationToken);
    Task SaveAsync(string subscriberName, long offset, CancellationToken cancellationToken);
}
```

- `IIntegrationEvent` (marcador) y `EventMetadata` viven en `RutaViva.IntegrationContracts.Abstractions`, no en BuildingBlocks: los contratos no dependen de nada, y BuildingBlocks.Application referencia a los contratos (nunca al revés).
- `IIntegrationEventPublisher.Enqueue` escribe en el Outbox de la unidad de trabajo actual y se confirma con su `SaveChangesAsync`. Es la única vía para publicar un evento de integración **sin** evento de dominio, reservada a hechos que no cambian un agregado (caso único: `OdometerReadingRecorded` de una lectura ignorada, ADR-0014 y ADR-0025). Todo lo demás sale por el *mapper* de D-04.
- `IEventCheckpointStore` tiene dos implementaciones: `EfEventCheckpointStore<TContext>` (en la transacción del efecto) e `InMemoryEventCheckpointStore` para suscriptores sin base de datos (Notifications, con `StartPosition.Latest`).
- Carga masiva (patrón **Template Method**): `BulkRegistrationHandler<TRow, TCommand>` resuelve el command de cada fila (`ResolveCommandAsync`, abstracto), calcula su clave natural (`NaturalKey`, abstracto), devuelve `Skipped` si ya existe, `Invalid` con `Bulk.DuplicateInBatch` si se repite en el lote y, si no, ejecuta el mismo `ICommandHandler` del alta individual. `IBulkExecutionScope` y `DryRunUnitOfWork` (patrón **Null Object**) hacen que `dryRun=true` ejecute las reglas sin guardar, sin condiciones `if (dryRun)` repartidas. Lo usan los 5 endpoints masivos.

- Decoradores (patrón **Decorator**) registrados con Scrutor: `ValidationCommandHandlerDecorator` (FluentValidation, devuelve `ErrorType.Validation`) y `LoggingCommandHandlerDecorator` (log estructurado con el nombre del command y el resultado). Los handlers no repiten validación ni logging (DRY, SRP).
- Las queries leen con proyecciones `AsNoTracking` directas a DTOs a través de un puerto `I{Context}Queries` de Application implementado en Infrastructure; no pasan por los agregados (CQRS).
- `IUnitOfWork.SaveChangesAsync` traduce `DbUpdateConcurrencyException` a `Concurrency.Conflict` (`ErrorType.Conflict`).
- Alternativa descartada: MediatR. Rechazada por ADR-0010 IMP-002 y porque el *pipeline* se cubre con dos decoradores explícitos y tipados.

### D-04 Despacho de eventos de dominio y Outbox en la misma transacción

`DomainEventsToOutboxInterceptor : SaveChangesInterceptor` (ADR-0011 IMP-002):
1. Recoge los `DomainEvents` de las raíces rastreadas y los limpia.
2. Ejecuta los `IDomainEventHandler<T>` del mismo servicio (pueden crear otros agregados; sus eventos se procesan en la siguiente vuelta, con un máximo de 5 vueltas para cortar ciclos).
3. Pasa cada evento por su `IIntegrationEventMapper<T>` y escribe un `OutboxMessage` por evento de integración.
4. Llama a `base.SavingChangesAsync`. Todo ocurre dentro de la estrategia de ejecución (`CreateExecutionStrategy`) por `EnableRetryOnFailure` (ADR-0013 IMP-002).

Un evento de dominio no se publica tal cual: el *mapper* lo traduce a un contrato de integración (**Anti-Corruption** hacia fuera: el modelo interno puede cambiar sin romper suscriptores).

### D-05 Tablas técnicas por schema de negocio

| Tabla | Columnas |
|---|---|
| `{schema}.OutboxMessages` | `Id uniqueidentifier PK` (= `MessageId`), `Stream varchar(60)`, `EventType varchar(150)`, `Subject varchar(60)`, `Payload nvarchar(max)` (envelope), `OccurredAt`, `ProcessedAt null`, `Attempts int`, `LastError nvarchar(1000) null`; índice filtrado `WHERE ProcessedAt IS NULL` |
| `{schema}.EventCheckpoints` | `SubscriberName varchar(60) PK`, `LastOffset bigint`, `UpdatedAt` |
| `eventbus.Events` | `Offset bigint identity PK`, `MessageId uniqueidentifier UK`, `Stream`, `Type`, `Subject`, `OccurredAt`, `StoredAt`, `Payload nvarchar(max)` |
| `eventbus.DeadLetters` | `Id bigint identity PK`, `SubscriberName`, `Offset`, `Error nvarchar(2000)`, `ReportedAt` |

Configuraciones EF compartidas: `OutboxMessageConfiguration`, `EventCheckpointConfiguration` y la extensión `modelBuilder.ApplyRutaVivaConventions()` (enums por nombre `varchar(20)`, `datetimeoffset(0)`, `decimal(18,2)`, `rowversion` en raíces mediante la interfaz marcadora `IHasRowVersion`).

### D-06 Servicio EventBus

- Hub `EventsHub` en `/internal/hubs/events`, métodos con nombres en `RutaViva.IntegrationContracts.EventBus.EventBusHubMethods` (ADR-0024 IMP-002): `Publish(envelope) → long`, `Subscribe(string[] streams)`, `GetEvents(string[] streams, long afterOffset, int max) → EventRecord[]`, `GetLatestOffset() → long`, `ReportFailure(string subscriber, long offset, string error)`. Aviso al cliente: `EventsAvailable(string stream, long offset)`.
- `SingleWriterEventStore` serializa las inserciones con `SemaphoreSlim(1)`; la idempotencia por `MessageId` se resuelve con el índice único (2601/2627 → leer el offset existente).
- Capas (D-01): `RutaViva.EventBus.Application` contiene `PublishEventCommand`, `GetEventsQuery`, `ReportFailureCommand`, la validación del envelope y el puerto `IEventStore`; `RutaViva.EventBus.Infrastructure` contiene `SingleWriterEventStore` y el `EventBusDbContext` (schema `eventbus`); `RutaViva.EventBus.Hub` solo aloja `EventsHub`, que traduce llamadas SignalR a commands y queries (Adapter) sin lógica. No hay proyecto Domain: el log no tiene invariantes de negocio más allá de la validación del envelope (YAGNI).
- El EventBus no usa Outbox (es el log).

### D-07 Cliente del EventBus (`RutaViva.BuildingBlocks.EventBus.Client`)

- `EventBusPublisher : IIntegrationEventTransport` usado por `OutboxRelayService` (`BackgroundService`, ciclo de 500 ms, lote de 50).
- `EventBusSubscriberHost : BackgroundService`: `HubConnection.WithAutomaticReconnect()`, un `Channel<long>` de capacidad 1 como señal de despertar (aviso, reconexión o temporizador de 5 s), bucle secuencial `GetEvents → despachar → checkpoint`.
- Cada servicio declara su suscripción con un *builder*: `services.AddEventBusSubscriber("maintenance", startFrom: StartPosition.Checkpoint).Handle<VehicleRegisteredV1, VehicleRegisteredHandler>(Streams.FleetVehicle)`. El *builder* registra el mapa `type → handler` (patrón **Registry** + **Strategy**), sin `switch` por tipo.
- `IntegrationEventDispatcher` abre una transacción por evento: ejecuta el handler, guarda el checkpoint y confirma. Reintentos 1 s/2 s/4 s con Polly v8 (`ResiliencePipeline`), luego `ReportFailure` y avance.
- `StartPosition.Latest` para Notifications (spec `reliable-messaging`), con `InMemoryEventCheckpointStore`.
- Un evento de un stream suscrito cuyo `type` no tiene handler registrado se **omite**: avanza el checkpoint y no genera *dead letter* ni reintentos. Así cada suscriptor registra solo los eventos que le interesan de un stream compartido.
- Interfaces marcadoras en `RutaViva.IntegrationContracts`: `IRoutineSnapshotEvent { RoutineSnapshot Routine }` e `IServiceOrderSnapshotEvent { ServiceOrderSnapshot Order }`. El *builder* acepta `HandleFamily<TMarker, THandler>(stream)`, de modo que Notifications y Reporting usan un handler por familia en lugar de uno por evento (DRY).

### D-08 Convenciones de API (`RutaViva.BuildingBlocks.Api`)

- `ResultExtensions.ToHttpResult()` mapea `ErrorType` → 400/404/409/422 con `ProblemDetails` y la extensión `code`. Los endpoints nunca construyen `ProblemDetails` a mano.
- `CorrelationIdMiddleware` (encabezado `X-Correlation-Id`, scope de logging).
- `JsonStringEnumConverter` global con `allowIntegerValues: false`.
- Endpoints Minimal API agrupados por *feature* con una interfaz `IEndpointModule { void MapEndpoints(IEndpointRouteBuilder app); }` descubierta por reflexión una sola vez al arrancar.

### D-09 Normalización de rutas

Todas las rutas públicas siguen `/api/v1/{service-segment}/...` y el Gateway enruta por prefijo `/api/v1/{service-segment}/{**catch-all}`. Esto corrige C-05 sin cambiar el comportamiento acordado en los ADRs.

### D-10 Script de base de datos

`01-schemas-and-users.sql` genera, para la lista de 8 schemas (`customers`, `workshops`, `fleet`, `maintenance`, `serviceorders`, `imports`, `reporting`, `eventbus`), el schema, el login/usuario `rutaviva_{schema}_app` con contraseña por variable `sqlcmd`, el `GRANT` sobre su schema y el `DENY` sobre los demás, en un único ciclo (D-20). Las tablas las crean las migraciones de EF Core de cada servicio con un usuario administrativo. En local los servicios se conectan con autenticación de Windows (`ConnectionStrings:RutaViva`) y los usuarios por servicio se verifican con `EXECUTE AS USER` (ADR-0028).

## Registro de contratos

> Fuente única de nombres. Los changes posteriores referencian estas secciones por su ancla (`design.md#streams`, `design.md#eventos-de-integración`, `design.md#endpoints-públicos`).

### Streams

Constantes en `RutaViva.IntegrationContracts.Streams`:

| Constante | Stream | Productor | Suscriptores |
|---|---|---|---|
| `CustomersCustomer` | `customers.customer` | Customers | Fleet, ServiceOrders, Reporting |
| `WorkshopsWorkshop` | `workshops.workshop` | Workshops | Fleet, Reporting |
| `FleetVehicle` | `fleet.vehicle` | Fleet | Maintenance, ServiceOrders, Reporting |
| `MaintenanceRoutine` | `maintenance.routine` | Maintenance (agregado `MaintenancePlan`) | ServiceOrders, Notifications, Reporting |
| `MaintenanceAlert` | `maintenance.alert` | Maintenance | Notifications, Reporting |
| `ServiceOrdersServiceOrder` | `serviceorders.service-order` | ServiceOrders | Maintenance, Notifications, Reporting |

### Envelope

`IntegrationEventEnvelope` (CloudEvents 1.0): `specversion = "1.0"`, `id` (Guid = `MessageId`), `source = "/rutaviva/{service}"`, `type = "rutaviva.{service}.{EventName}.v1"`, `subject` (ID del agregado), `time`, `datacontenttype = "application/json"`, `traceparent` (extensión) y `data`. `EventMetadata` expuesto a los handlers: `MessageId`, `Offset`, `Type`, `Subject`, `OccurredAt`.

### Eventos de integración

Todos son `sealed record` en `RutaViva.IntegrationContracts.{Service}` con sufijo `V1` en el nombre de la clase y `EventName` sin sufijo en el `type`. Los enums de los contratos son `string` (tolerant reader, data-model E-04): el consumidor los convierte a su propio enum. Todos incluyen `OccurredAt` (`DateTimeOffset`).

**customers.customer**

| Evento | Campos |
|---|---|
| `CustomerRegistered` | `CustomerId`, `TaxId`, `Name`, `Email`, `Status` |
| `CustomerStatusChanged` | `CustomerId`, `Status` |
| `ApproverRegistered` | `ApproverId`, `CustomerId`, `FullName`, `Email`, `Status` |
| `ApproverStatusChanged` | `ApproverId`, `CustomerId`, `Status` |

**workshops.workshop**

| Evento | Campos |
|---|---|
| `WorkshopRegistered` | `WorkshopId`, `TaxId`, `Name`, `City`, `Status` |
| `WorkshopStatusChanged` | `WorkshopId`, `Status` |

**fleet.vehicle**

| Evento | Campos |
|---|---|
| `VehicleRegistered` | `VehicleId`, `Plate`, `Vin?`, `Brand`, `Model`, `ModelYear`, `CustomerId`, `WorkshopId`, `ResponsibleApproverId?`, `Status`, `InitialOdometerKm`, `RegisteredAt` |
| `VehicleStatusChanged` | `VehicleId`, `Plate`, `CustomerId`, `Status` (`Active`/`Inactive`) |
| `VehicleResponsibleAssigned` | `VehicleId`, `ResponsibleApproverId?` |

**maintenance.routine** (todos los eventos de rutina llevan el bloque `RoutineSnapshot`)

`RoutineSnapshot`: `RoutineId`, `VehicleId`, `Plate`, `CustomerId`, `RoutineTypeCode`, `RoutineName`, `State`, `DueStatus`, `CycleNumber`, `CycleStartKm`, `CycleStartDate`, `IntervalKm?`, `IntervalMonths?`, `EffectiveDueSoonPercent`, `NextDueKm?`, `NextDueDate?`, `OdometerKm`, `ConsumedPercent`.

| Evento | Campos adicionales | Semántica |
|---|---|---|
| `MaintenanceRoutineRegistered` | `Routine` | Rutina creada (carga inicial o alta) |
| `MaintenanceRoutineDueSoon` | `Routine`, `TriggeredBy` | Transición `UpToDate → DueSoon` (RN-2) |
| `MaintenanceRoutineOverdue` | `Routine`, `TriggeredBy` | Transición a `Overdue`, una vez por ciclo. **ServiceOrders crea la orden** (RN-3) |
| `MaintenanceRoutineDueStatusReverted` | `Routine`, `Reason = ParametersChanged` | `DueSoon → UpToDate` por cambio de parámetros (A-16) |
| `MaintenanceRoutineRestarted` | `Routine`, `ServiceOrderId`, `ServicedKm`, `ServicedAt` | Ciclo reiniciado por `ServiceOrderCompleted` (RN-5) |
| `MaintenanceRoutineStateChanged` | `Routine` | Activación o desactivación (ADR-0020) |
| `MaintenanceRoutineParametersChanged` | `Routine` | Intervalos o % cambiaron (próximo mantenimiento recalculado) |
| `MaintenancePlanStatusChanged` | `VehicleId`, `Plate`, `CustomerId`, `Status` (`Active`/`Suspended`) | Reacción a `VehicleStatusChanged` |
| `OdometerReadingRecorded` | `VehicleId`, `Plate`, `CustomerId`, `Kilometers`, `ReadAt`, `Outcome`, `IgnoredReason?`, `CurrentOdometerKm` | Cada lectura de un vehículo conocido (ADR-0025) |

Orden de emisión en una misma lectura: `OdometerReadingRecorded` → por cada rutina que cambia, `MaintenanceRoutineDueSoon` u `MaintenanceRoutineOverdue` → (stream `maintenance.alert`) `AlertStatusChanged` de la alerta escalada → `AlertRaised`. Todos en la misma transacción y con offsets consecutivos.

**maintenance.alert**

| Evento | Campos |
|---|---|
| `AlertRaised` | `AlertId`, `VehicleId`, `Plate`, `CustomerId`, `RoutineId`, `RoutineName`, `CycleNumber`, `Type`, `TriggeredBy`, `Status`, `RaisedAt` |
| `AlertStatusChanged` | `AlertId`, `VehicleId`, `Plate`, `CustomerId`, `RoutineId`, `Type`, `TriggeredBy`, `Status`, `AcknowledgedAt?`, `AcknowledgedBy?`, `ResolvedAt?`, `ResolutionReason?` |

**serviceorders.service-order** (todos llevan el bloque `ServiceOrderSnapshot`)

`ServiceOrderSnapshot`: `ServiceOrderId`, `Number`, `Origin`, `VehicleId`, `Plate`, `RoutineId`, `RoutineName`, `CustomerId`, `WorkshopId`, `ResponsibleApproverId?`, `Status`, `StatusBeforeHold?`, `ApprovalReason?`, `ApprovalPriority?`, `RoutineAmount?`, `AdditionalAmount?`, `TotalAmount?`, `Currency?`, `AutoApproved`, `AppointmentAt?`, `CancellationReason?`, `CreatedAt`.

| Evento | Campos adicionales | Semántica |
|---|---|---|
| `ServiceOrderCreated` | `Order` | Orden automática o manual creada |
| `ServiceOrderStatusChanged` | `Order`, `FromStatus`, `ToStatus` | Cada transición (incluye `Quoted` transitorio, A-14) |
| `ServiceOrderApprovalRequested` | `Order`, `Reason`, `Priority` | Entra en `PendingApproval`. `High` = servicios adicionales con el vehículo en sitio (A-15) |
| `ServiceOrderLinesChanged` | `Order`, `Lines[]` (`LineId`, `LineType`, `Description`, `Amount?`, `PricingStatus`, `RequestedBy`, `ApprovalStatus`) | Servicio pedido, cotizado, aprobado o rechazado sin cambio de estado |
| `ServiceOrderAppointmentScheduled` | `Order`, `ScheduledAt`, `Notes?`, `IsReschedule` | ADR-0021 |
| `ServiceOrderCompleted` | `Order`, `ServicedKm`, `ServicedAt` | **Maintenance reinicia la rutina** (RN-5) |

### Endpoints públicos

Propiedad de cada change. Todos bajo el Gateway (D-09).

| Servicio (change) | Endpoints |
|---|---|
| Customers (`supporting-contexts`) | `GET/POST /api/v1/customers`, `GET /api/v1/customers/{id}`, `PATCH /api/v1/customers/{id}/status`, `POST /api/v1/customers/{id}/approvers`, `PATCH /api/v1/customers/{id}/approvers/{approverId}/status`, `POST /api/v1/customers/bulk?dryRun=`, `POST /api/v1/customers/approvers/bulk?dryRun=` |
| Workshops (`supporting-contexts`) | `GET/POST /api/v1/workshops`, `GET /api/v1/workshops/{id}`, `PATCH /api/v1/workshops/{id}/status`, `POST /api/v1/workshops/bulk?dryRun=` |
| Fleet (`supporting-contexts`) | `GET/POST /api/v1/fleet/vehicles`, `GET /api/v1/fleet/vehicles/{id}`, `GET /api/v1/fleet/vehicles/by-plate/{plate}`, `PATCH /api/v1/fleet/vehicles/{id}/status`, `PUT /api/v1/fleet/vehicles/{id}/responsible`, `POST /api/v1/fleet/vehicles/bulk?dryRun=` |
| Maintenance (`maintenance-core`) | `POST /api/v1/maintenance/odometer-readings`, `POST /api/v1/maintenance/time-evaluations`, `POST /api/v1/maintenance/reevaluations`, `GET /api/v1/maintenance/plans?customerId=` (resumen por vehículo), `GET /api/v1/maintenance/plans/{vehicleId}`, `GET /api/v1/maintenance/plans/{vehicleId}/odometer-readings`, `PUT /api/v1/maintenance/plans/{vehicleId}/routines/{routineId}`, `PATCH /api/v1/maintenance/plans/{vehicleId}/routines/{routineId}/state`, `POST /api/v1/maintenance/routines/bulk?dryRun=`, `GET /api/v1/maintenance/routine-types`, `PUT /api/v1/maintenance/routine-types/{id}`, `GET /api/v1/maintenance/alerts`, `POST /api/v1/maintenance/alerts/{id}/acknowledge` |
| ServiceOrders (`service-orders-core`) | `POST /api/v1/service-orders` (manual), `GET /api/v1/service-orders`, `GET /api/v1/service-orders/{id}`, `GET /api/v1/service-orders/approvals`, `POST …/{id}/quote`, `POST …/{id}/additional-services/requests`, `POST …/{id}/additional-services/{lineId}/price`, `POST …/{id}/additional-services`, `PUT …/{id}/appointment`, `POST …/{id}/on-site`, `POST …/{id}/start`, `POST …/{id}/complete`, `POST …/{id}/approve`, `POST …/{id}/reject`, `POST …/{id}/cancel`, `GET/POST /api/v1/service-orders/approval-policies` |
| DataImport (`data-import`) | `POST /api/v1/imports`, `POST /api/v1/imports/{id}/confirm`, `GET /api/v1/imports/{id}`, `GET /api/v1/imports/{id}/errors.csv?entity=`, `GET /api/v1/imports/templates/{entity}.csv` |
| Reporting (`reporting`) | `GET /api/v1/reports`, `GET /api/v1/reports/{code}`, `GET /api/v1/reports/{code}/download?format=` |
| Notifications (`edge-gateway-notifications`) | Hub `/hubs/notifications`. Servidor: `JoinCustomer`, `LeaveCustomer`, `JoinApprover`, `LeaveApprover`, `JoinWorkshop`, `LeaveWorkshop`. Cliente: `ServiceOrderChanged`, `ApprovalRequested`, `AlertChanged`, `RoutineChanged`, `OdometerReadingRecorded`, `VehicleChanged` |

### Contrato del lote masivo (`bulk?dryRun=`)

Request: `{ "rows": [ { "rowNumber": int, ...campos de la entidad } ] }` (máximo 500). Response `200`: `[{ "rowNumber": int, "outcome": "Valid" | "Invalid" | "Skipped", "errors": [ { "code": string, "message": string } ] }]`. Con `dryRun=true` no persiste nada. `Skipped` = la entidad ya existe (idempotencia por clave natural). Es el único contrato HTTP que consume otro servicio (DataImport, ADR-0022).

Tipos en `RutaViva.IntegrationContracts.Bulk`: `BulkRequest<TRow>(IReadOnlyList<TRow> Rows)`, `BulkRowResult`, `BulkRowOutcome`, `BulkRowError(Code, Message)` y una fila por entidad. Los campos opcionales vacíos llegan como `null` y el dueño aplica sus valores por defecto.

| Fila | Campos | Clave natural (`Skipped`) | Endpoint |
|---|---|---|---|
| `WorkshopBulkRow` | `RowNumber`, `TaxId`, `Name`, `Email`, `Phone`, `Address`, `City` | `TaxId` | `POST /api/v1/workshops/bulk` |
| `CustomerBulkRow` | `RowNumber`, `TaxId`, `Name`, `Email`, `Phone` | `TaxId` | `POST /api/v1/customers/bulk` |
| `ApproverBulkRow` | `RowNumber`, `CustomerTaxId`, `FullName`, `Email` | `CustomerTaxId` + `Email` | `POST /api/v1/customers/approvers/bulk` |
| `VehicleBulkRow` | `RowNumber`, `Plate`, `Vin?`, `Brand`, `Model`, `ModelYear`, `OwnerTaxId`, `WorkshopTaxId`, `ResponsibleApproverEmail?`, `InitialOdometerKm?` | `Plate` | `POST /api/v1/fleet/vehicles/bulk` |
| `RoutineBulkRow` | `RowNumber`, `Plate`, `RoutineTypeCode`, `LastServiceKm?`, `LastServiceDate?`, `IntervalKm?`, `IntervalMonths?`, `DueSoonThresholdPercent?` | `Plate` + `RoutineTypeCode` | `POST /api/v1/maintenance/routines/bulk` |

Códigos de error estables (constantes en `RutaViva.IntegrationContracts.Bulk.BulkErrorCodes`). DataImport reintenta la propagación (hasta 10 s) solo ante los marcados como «referencia»:

| Código | Productor | Referencia |
|---|---|---|
| `Bulk.DuplicateInBatch` | Todos | No |
| `Customer.NotFound` | Customers (aprobadores) | Sí |
| `Vehicle.OwnerNotFound`, `Vehicle.WorkshopNotFound`, `Vehicle.ResponsibleNotFound` | Fleet | Sí |
| `Vehicle.ResponsibleAmbiguous` | Fleet | No |
| `MaintenancePlan.NotFound` | Maintenance | Sí |
| `RoutineType.NotFound`, `Routine.LastServiceKmAboveOdometer`, `Routine.IncompleteLastService` | Maintenance | No |

### Contratos consolidados de los changes

Pedidos de los changes incorporados a este registro (el detalle está en la sección «Contratos nuevos requeridos» de cada `design.md`):

| Origen | Contrato | Dónde quedó |
|---|---|---|
| `supporting-contexts` C-N1, C-N2 | Plantilla masiva en BuildingBlocks y `Bulk.DuplicateInBatch` | D-03 y tabla de códigos |
| `supporting-contexts` C-N3 | `VehicleWorkshopAssigned` sin productor | **Eliminado** del registro y de sus consumidores (YAGNI; elegir el taller está fuera del alcance) |
| `edge-gateway-notifications` CN-01, CN-02, CN-03 | `IEventCheckpointStore`, omisión de eventos sin handler, interfaces marcadoras | D-03 y D-07 |
| `maintenance-core` CN-01, CN-02, CN-03, CN-05 | `GET /plans?customerId=`, `IIntegrationEventPublisher.Enqueue`, códigos y `RoutineBulkRow` | Endpoints, D-03 y contrato masivo |
| `data-import` 1, 2, 4, 5 | Filas masivas, códigos de referencia, claves naturales, `errors.csv?entity=` | Contrato masivo y endpoints |
| `fleet-maintenance-ui` CR-07 | Métodos del hub de Notifications | Endpoints |
| Columnas nuevas (`maintenance-core` CN-04, `service-orders-core`, `reporting`, `supporting-contexts`, `data-import` 6) | `Alerts.Plate/CustomerId/RoutineName`, `ServiceOrders.Plate/ResponsibleApproverId`, `VehicleRoutineStatus.Plate/CustomerId`, `LastEventOffset` en proyecciones, `TaxId`/`Email` en las réplicas de Fleet, `ImportStages.FailureReason` | Tarea 8.2 (sincronizar `data-model.md` y el script de referencia) |

## Patrones aplicados

| Patrón | Dónde | Por qué |
|---|---|---|
| Transactional Outbox, Idempotent Consumer | D-04, D-07 | *Dual write* y entrega *at-least-once* (ADR-0024) |
| Decorator | D-03 | Validación y logging transversales sin tocar handlers (OCP) |
| Registry + Strategy | D-07 | Despacho de eventos por tipo sin `switch` |
| Adapter | D-06, D-07 | El hub y el cliente SignalR traducen; Application no conoce el transporte (DIP) |
| Template Method | `EntityTypeConfiguration` base de raíces (`rowversion`, eventos ignorados) | Configuración repetida en un solo lugar |
| Result / Notification | D-02 | Errores de negocio como valores |

## Antipatrones evitados

| Antipatrón | Cómo se evita |
|---|---|
| Modelo anémico | Las clases base no exponen *setters*; la prueba `architecture-governance` lo verifica |
| Objeto dios | El EventBus separa hub (Adapter), store (escritor único) y handlers; el cliente separa publisher, subscriber host y dispatcher |
| Números mágicos | Intervalos, tamaños de lote y reintentos en `EventBusOptions` y `OutboxOptions` con valores por defecto como constantes |
| Copy-paste | Outbox, checkpoints, convenciones EF y mapeo HTTP en BuildingBlocks; cada servicio solo declara |
| Martillo dorado | El EventBus no transporta la telemetría (alto volumen, ADR-0014) ni las consultas |

## Risks / Trade-offs

- [El EventBus es punto único de fallo] → Outbox en los productores y checkpoint en los suscriptores: nada se pierde, solo se retrasa (ADR-0024 POS-002).
- [Bucle de eventos de dominio infinito] → Máximo de 5 vueltas en el interceptor, con error explícito.
- [Docker no está disponible en el equipo de desarrollo ni en el de la sustentación] → ADR-0027: las pruebas de integración usan `SqlServerTestDatabase` (`tests/RutaViva.Testing.Fixtures`), que crea una BD `RutaViva_Tests_{guid}` en el SQL Server local, aplica las migraciones y la borra al terminar. La cadena base se lee de la variable `RUTAVIVA_TEST_SQL` con valor por defecto `Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;`. Se marcan con `Trait("Category","Integration")`.
- [BuildingBlocks acopla versiones] → Aceptado en un único repositorio (ADR-0010 NEG-003).

## Migration Plan

No aplica (*greenfield*). Arranque local: ejecutar `deploy/local/sql/*.sql` con `sqlcmd` y las variables de contraseña, aplicar migraciones por servicio y arrancar el AppHost (o cada servicio).

## Open Questions

- Versión exacta de Aspire disponible en el equipo (`AddJavaScriptApp` frente a `AddNpmApp`). No cambia las specs.
