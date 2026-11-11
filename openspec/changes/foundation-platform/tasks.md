> Plan de ejecución en paralelo: [parallel-execution.md](parallel-execution.md)

## 1. Solución y reglas de compilación

- [x] 1.1 Crear `RutaViva.slnx`, `Directory.Build.props` (net10.0, Nullable, TreatWarningsAsErrors, LangVersion 14), `Directory.Packages.props` con las versiones centrales, `.editorconfig` (`csharp_style_var_*=false:error`, IDE0008 error (IDE0007 en none) y EnforceCodeStyleInBuild) y `global.json`. Verificar con `dotnet build RutaViva.slnx` en verde y con un archivo temporal que use `var` haciendo fallar el build (luego se borra)
- [x] 1.2 Crear los proyectos vacíos de `src/BuildingBlocks/*` (Domain, Application, Infrastructure, EventBus.Client, Api), `src/Contracts/RutaViva.IntegrationContracts` y sus referencias permitidas. Verificar con `dotnet build` y `dotnet sln list` mostrando los 6 proyectos

## 2. Contratos (primero, los consumen todos los demás)

- [ ] 2.1 `RutaViva.BuildingBlocks.Domain`: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`, `Error`, `ErrorType`, `Result`, `Result<T>` y `Guard` (design D-02). Verificar con pruebas en `tests/BuildingBlocks/RutaViva.BuildingBlocks.UnitTests/Domain/` (igualdad de VOs, `Raise`/`ClearDomainEvents`, conversiones implícitas de `Result<T>`)
- [ ] 2.2 `RutaViva.BuildingBlocks.Application`: `ICommand<T>`, `IQuery<T>`, `ICommandHandler`, `IQueryHandler`, `IUnitOfWork`, `IClock`, `IDomainEventHandler<T>`, `IIntegrationEventMapper<T>`, `IIntegrationEventHandler<T>`, `IIntegrationEventPublisher`, `IEventCheckpointStore` (design D-03; `IIntegrationEvent` y `EventMetadata` viven en los contratos). Verificar con `dotnet build` y con la prueba de arquitectura 7.2 (sin dependencias de AspNetCore ni EF)
- [ ] 2.3 `RutaViva.IntegrationContracts`: `Abstractions/` (`IIntegrationEvent`, `EventMetadata`), `Streams`, `IntegrationEventEnvelope`, `EventBusHubMethods`, `EventRecord`, `Bulk/` (`BulkRequest<TRow>`, `BulkRowResult`, `BulkRowOutcome`, `BulkRowError`, las 5 filas `*BulkRow` y `BulkErrorCodes`, design § Contrato del lote masivo), y todos los eventos `V1` de customers, workshops y fleet con sus campos (design § Eventos de integración). Verificar con `dotnet build` y con la prueba de contrato 7.3 para esos eventos
- [ ] 2.4 `RutaViva.IntegrationContracts`: eventos `V1` de maintenance.routine (`RoutineSnapshot` incluido), maintenance.alert y serviceorders.service-order (`ServiceOrderSnapshot` incluido), con las interfaces marcadoras `IRoutineSnapshotEvent` e `IServiceOrderSnapshotEvent` (design D-07). Verificar con la prueba de contrato 7.3 para esos eventos

## 3. Aplicación transversal

- [ ] 3.1 `ValidationCommandHandlerDecorator` y `LoggingCommandHandlerDecorator` con registro por Scrutor (`AddCommandHandlers(Assembly)`). Verificar con pruebas unitarias: un command inválido devuelve `ErrorType.Validation` sin invocar el handler, y el log incluye el nombre del command
- [ ] 3.2 `SystemClock : IClock` con `TodayIn(TimeZoneInfo)` y, en `tests/RutaViva.Testing.Common`, `FakeClock` y `SqlServerTestDatabase` (ADR-0027: BD `RutaViva_Tests_{guid}` en el SQL local, cadena de `RUTAVIVA_TEST_SQL`, limpieza de huérfanas). Verificar con prueba unitaria de `TodayIn` en `America/Bogota` cerca de la medianoche UTC y con una prueba de integración que crea, usa y borra la BD
- [ ] 3.3 `BulkRegistrationHandler<TRow, TCommand>`, `IBulkExecutionScope` y `DryRunUnitOfWork` (design D-03, Template Method + Null Object). Verificar con pruebas unitarias sobre un command de prueba: fila existente = `Skipped`, fila repetida en el lote = `Invalid` con `Bulk.DuplicateInBatch`, `dryRun=true` no llama a `SaveChangesAsync` y `dryRun=false` confirma cada fila por separado

## 4. Infraestructura de persistencia y Outbox

- [ ] 4.1 `ApplyRutaVivaConventions` (enums por nombre `varchar(20)`, `datetimeoffset(0)`, `decimal(18,2)`, `IHasRowVersion`), `OutboxMessage` + `OutboxMessageConfiguration`, `EventCheckpoint` + `EventCheckpointConfiguration` (design D-05). Verificar con prueba de modelo EF (`IModel`) que las columnas tienen los tipos esperados
- [ ] 4.2 `EfUnitOfWork<TContext>` con traducción de `DbUpdateConcurrencyException` a `Concurrency.Conflict` y estrategia de ejecución. Verificar con prueba de integración (BD desechable `SqlServerTestDatabase`, ADR-0027) de dos guardados concurrentes que devuelve `ErrorType.Conflict`
- [ ] 4.3 `DomainEventsToOutboxInterceptor` (design D-04): handlers internos, mappers a integración, Outbox y tope de 5 vueltas. Verificar con pruebas de integración: evento con efecto interno confirmado juntos; transacción fallida sin filas en Outbox; ciclo infinito devuelve error
- [ ] 4.4 `OutboxIntegrationEventPublisher : IIntegrationEventPublisher` (escribe en el Outbox de la unidad de trabajo actual) y `EfEventCheckpointStore<TContext>`. Verificar con prueba de integración: un `Enqueue` sin `SaveChangesAsync` no deja filas y con `SaveChangesAsync` deja una

## 5. Servicio EventBus

- [ ] 5.1 `src/Platform/EventBus/RutaViva.EventBus.Application`: validación del envelope, `PublishEventCommand`, `GetEventsQuery`, `ReportFailureCommand` y puerto `IEventStore` (design D-06). Verificar con pruebas unitarias en `tests/Platform/RutaViva.EventBus.Application.UnitTests` de la validación del envelope (`type` con formato `rutaviva.{service}.{EventName}.v{n}`)
- [ ] 5.2 `src/Platform/EventBus/RutaViva.EventBus.Infrastructure`: `EventBusDbContext` (schema `eventbus`: `Events`, `DeadLetters`), migración inicial y `SingleWriterEventStore` con idempotencia por `MessageId`. Verificar con pruebas de integración en `tests/Platform/RutaViva.EventBus.Hub.IntegrationTests`: duplicado devuelve el mismo offset; 50 publicaciones paralelas dan offsets consecutivos
- [ ] 5.3 `src/Platform/EventBus/RutaViva.EventBus.Hub`: `EventsHub` en `/internal/hubs/events` con los 5 métodos y el aviso `EventsAvailable` al grupo `stream:{name}`. Verificar con prueba de integración `WebApplicationFactory` + cliente SignalR: un suscriptor de `fleet.vehicle` recibe el aviso y uno de otro stream no

## 6. Cliente del EventBus

- [ ] 6.1 `EventBusPublisher` y `OutboxRelayService` (500 ms, lote 50, `Attempts`/`LastError`). Verificar con prueba de integración: con el EventBus caído 3 ciclos el mensaje queda pendiente con 3 intentos y luego se publica una sola vez
- [ ] 6.2 `EventBusSubscriberHost`, `IntegrationEventDispatcher`, *builder* `AddEventBusSubscriber(...).Handle<TEvent, THandler>(stream)` `HandleFamily<TMarker, THandler>(stream)`, `StartPosition`, `InMemoryEventCheckpointStore` y omisión de eventos sin handler (design D-07). Verificar con pruebas de integración: evento entregado 5 veces = 1 efecto; suscriptor detenido se pone al día con 3 eventos; aviso perdido se recupera en ≤ 5 s; evento sin handler avanza el checkpoint sin *dead letter*
- [ ] 6.3 Reintentos 1/2/4 s con Polly v8 y `ReportFailure` con avance del checkpoint. Verificar con prueba de integración: handler que siempre falla genera 1 *dead letter* y el siguiente evento se procesa

## 7. API común y gobierno de arquitectura

- [ ] 7.1 `RutaViva.BuildingBlocks.Api`: `ResultExtensions.ToHttpResult`, `CorrelationIdMiddleware`, `IEndpointModule` + `MapEndpointModules`, configuración JSON (enums como texto sin enteros). Verificar con pruebas `WebApplicationFactory` sobre una API mínima de prueba: 400/404/409/422 con `code`, eco de `X-Correlation-Id` y `400` ante enum desconocido
- [ ] 7.2 `tests/RutaViva.Architecture.Tests`: regla de dependencia por capa, aislamiento entre servicios, Application sin AspNetCore/EF/CsvHelper/ClosedXML, sin *setters* públicos en entidades, enums que empiezan en 1 (spec `architecture-governance`) y convención de nombres de ADR-0026 IMP-002 (sufijos por carpeta de `src/` y sin `Common`/`Shared`/`Utils`/`Core`). Las reglas se parametrizan por la lista de servicios para no repetirlas. Verificar con `dotnet test` en verde y con una violación temporal que hace fallar cada regla
- [ ] 7.3 `tests/RutaViva.IntegrationContracts.Tests`: `ContractSnapshot` (helper propio, ADR-0027: serializa con las opciones JSON de la solución y compara con `Snapshots/{Evento}.approved.json`; si el archivo no existe lo crea y la prueba falla) y una prueba parametrizada por cada evento `V1` dentro del envelope, descubiertos por reflexión sobre el ensamblado `RutaViva.IntegrationContracts`. Los `.approved.json` se versionan. Verificar con `dotnet test` y renombrando un campo para ver el fallo

## 8. Base de datos local y documentación

- [ ] 8.1 `deploy/local/sql/00-database.sql` y `01-schemas-and-users.sql` (8 schemas y usuarios en un ciclo, contraseñas por variables `sqlcmd`, design D-10). Verificar ejecutando con `sqlcmd` contra el SQL Server local y comprobando que `rutaviva_fleet_app` no puede leer `maintenance`
- [ ] 8.2 Alinear `docs/architecture/database/rutaviva-reference-schema.sql` y `docs/architecture/data-model.md` con ADR-0024, ADR-0025 y ADR-0026 y con las columnas de design § Contratos consolidados (schema `eventbus`, `EventCheckpoints` en lugar de `InboxMessages`, `maintenance.OdometerReadings`, `Alerts.Plate/CustomerId/RoutineName`, `ServiceOrders.Plate/ResponsibleApproverId`, `TaxId`/`Email` en réplicas de Fleet, `VehicleRoutineStatus.Plate/CustomerId`, `LastEventOffset` en proyecciones, `ImportStages.FailureReason`, D-19 por offset, y la estructura del análisis §9). Verificar con `sqlcmd -i` del script sobre una BD vacía sin errores

## 9. Hosting (Recortable)

- [ ] 9.1 (Recortable) `RutaViva.ServiceDefaults`: OpenTelemetry, `/health` con chequeo de SQL Server y `/alive`. Verificar con prueba `WebApplicationFactory`: `/health` = 503 con la BD caída y `/alive` = 200
- [ ] 9.2 (Recortable) `RutaViva.AppHost` con SQL Server como `AddConnectionString("rutaviva-sql")`, parámetros secretos y el EventBus registrado. Verificar con `dotnet run --project src/Platform/RutaViva.AppHost` y el dashboard mostrando el EventBus sano
