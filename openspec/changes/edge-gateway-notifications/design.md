## Context

Motivación en `proposal.md`. Este change consume la base de `foundation-platform`: rutas normalizadas (design D-09), streams y eventos `V1` (design § Registro de contratos), cliente del EventBus con `StartPosition.Latest` (design D-07) y convenciones de API (design D-08). Gateway y Notifications son **componentes técnicos y genéricos** (ADR-0010): no tienen dominio ni schema de base de datos.

## Goals / Non-Goals

**Goals:**
- Que la SPA conozca un solo origen y ninguna dirección interna.
- Que cada evento relevante llegue a la audiencia correcta en el orden del EventBus, con un mensaje estable aunque cambien los contratos de integración.
- Que agregar un tipo de mensaje nuevo sea agregar un *handler* y una traducción, sin tocar el hub (OCP).

**Non-Goals:**
- Garantía de entrega a la web (la web recarga por REST al reconectar, ADR-0016 IMP-003).
- Seguridad de los grupos (sin autenticación en el reto).
- Orquestación, agregación o caché en el Gateway.

## Decisions

### D-01 Proyectos y capas

| Proyecto | Contenido | Depende de |
|---|---|---|
| `RutaViva.ApiGateway` | Host YARP, CORS, correlación, salud. Sin capas: es infraestructura pura y no tiene casos de uso | `BuildingBlocks.Api` (solo `CorrelationIdMiddleware`), `ServiceDefaults` |
| `RutaViva.Notifications.Application` | Mensajes (DTOs), enums de tipo de cambio, `NotificationAudience`, puerto `IRealtimeNotifier`, traductores y *handlers* `IIntegrationEventHandler<T>` | `BuildingBlocks.Application`, `RutaViva.IntegrationContracts` |
| `RutaViva.Notifications.Hub` | `NotificationsHub`, `INotificationsClient`, `NotificationsHubMethods`, adaptador `SignalRRealtimeNotifier`, *hosting* y registro del suscriptor | `Notifications.Application`, `BuildingBlocks.EventBus.Client`, `BuildingBlocks.Api` |

No hay `Domain` ni `Infrastructure` de persistencia: crearlos vacíos sería *lava flow*. La prueba de arquitectura agrega Notifications a la regla «Application sin AspNetCore» en un archivo propio (`NotificationsArchitectureTests.cs`), sin editar las reglas existentes.

Alternativa descartada: un único proyecto Notifications. Rechazada porque mezclaría la traducción (lógica de audiencia y mapeo, que se prueba sin SignalR) con el transporte.

### D-02 Rutas de YARP (`RutaViva.ApiGateway/appsettings.json`)

| RouteId | Match | ClusterId | Destino (service discovery / fallback de desarrollo) |
|---|---|---|---|
| `customers` | `/api/v1/customers/{**catch-all}` | `customers` | `https+http://customers` |
| `workshops` | `/api/v1/workshops/{**catch-all}` | `workshops` | `https+http://workshops` |
| `fleet` | `/api/v1/fleet/{**catch-all}` | `fleet` | `https+http://fleet` |
| `maintenance` | `/api/v1/maintenance/{**catch-all}` | `maintenance` | `https+http://maintenance` |
| `service-orders` | `/api/v1/service-orders/{**catch-all}` | `serviceorders` | `https+http://serviceorders` |
| `imports` | `/api/v1/imports/{**catch-all}` | `dataimport` | `https+http://dataimport` |
| `reports` | `/api/v1/reports/{**catch-all}` | `reporting` | `https+http://reporting` |
| `hubs` | `/hubs/{**catch-all}` | `notifications` | `https+http://notifications` |

- Cada ruta también acepta la raíz del segmento (`/api/v1/fleet`) con un segundo `Match` sin *catch-all* cuando el servicio expone colecciones en la raíz (`GET /api/v1/customers`). Se resuelve declarando `Path = "/api/v1/customers/{**catch-all}"`, que en YARP también coincide con la ruta vacía.
- **Sin ruta para `/internal/**`**: al no existir, YARP responde 404. Se prueba explícitamente (spec `api-gateway`).
- `appsettings.Development.json` tiene las direcciones locales fijas para arrancar sin Aspire (ADR-0018 IMP-001); con Aspire las resuelve `AddServiceDiscoveryDestinationResolver()`.
- Sin `HttpRequest.Transforms` que reescriban la ruta: los servicios ya exponen la misma ruta pública (D-09).
- Sin política de reintentos ni *health checks* activos de YARP: reintentar un POST no idempotente duplicaría efectos (KISS, spec «sin reintento»).
- `Kestrel:Limits:MaxRequestBodySize` = 30 MB (constante en configuración) para la carga de 5 CSV de hasta 5 MB (ADR-0022).

Tiempos de espera: la ruta `imports` usa la política YARP `ImportsTimeout` de 120 s (`ActivityTimeout` de su cluster), pedida por `data-import` (confirmación síncrona, su riesgo R-03). El resto usa 30 s. Ambos valores son constantes de `GatewayOptions`, no números en el código.

Alternativa descartada: Ocelot. Rechazada porque YARP es de Microsoft, soporta WebSockets y *service discovery* de Aspire de forma nativa.

### D-03 CORS y correlación en el Gateway

- Política única `SpaCorsPolicy` con `WithOrigins(options.AllowedOrigins)` (sección `Gateway:Cors:AllowedOrigins`), `AllowAnyHeader`, `AllowAnyMethod`, `AllowCredentials` (necesario para SignalR) y `WithExposedHeaders("X-Correlation-Id", "Content-Disposition")` (la descarga de reportes lee el nombre del archivo).
- `CorrelationIdMiddleware` de BuildingBlocks antes de `MapReverseProxy`. Una transformación de YARP (`AddRequestTransform`) copia el valor del contexto al encabezado de la petición reenviada. Así el servicio destino reutiliza el mismo valor (spec `api-conventions`).
- Los servicios de negocio no configuran CORS: solo el Gateway es público.

### D-04 Suscripción de Notifications

```csharp
services.AddEventBusSubscriber(SubscriberName, StartPosition.Latest)
    .Handle<ServiceOrderCreatedV1, ServiceOrderChangedHandler<ServiceOrderCreatedV1>>(Streams.ServiceOrdersServiceOrder)
    ...;
```

| Stream | Por qué |
|---|---|
| `serviceorders.service-order` | Bandeja y detalle de órdenes, aprobación prioritaria (ADR-0016) |
| `maintenance.alert` | Bandeja y contador de alertas |
| `maintenance.routine` | Pista de la rutina, estado de vencimiento y pestaña «Lecturas» (ADR-0025) |
| `fleet.vehicle` | **Decisión de este change.** La lista de flota debe mostrar al instante los vehículos cargados por DataImport y el estado activo/inactivo que cambia otro usuario. `MaintenancePlanStatusChanged` no basta: es un reflejo derivado y no existe para un vehículo recién registrado. Fleet es la fuente de verdad del estado del vehículo |

`MaintenancePlanStatusChanged` y `VehicleResponsibleAssigned` **no** se registran (YAGNI: la UI recarga el detalle al abrirlo). El suscriptor los omite sin error (ver Contratos nuevos requeridos, CN-02).

`SubscriberName = "notifications"` es una constante de `NotificationsSubscription`.

### D-05 Mapa evento → método del cliente → audiencia

| Evento de integración | Método de `INotificationsClient` | `kind` | Audiencia |
|---|---|---|---|
| `ServiceOrderCreated` | `ServiceOrderChanged` | `Created` | `customer:{CustomerId}`, `workshop:{WorkshopId}`, `approver:{ResponsibleApproverId}`? |
| `ServiceOrderStatusChanged` | `ServiceOrderChanged` | `StatusChanged` | ídem |
| `ServiceOrderLinesChanged` | `ServiceOrderChanged` | `LinesChanged` | ídem |
| `ServiceOrderAppointmentScheduled` | `ServiceOrderChanged` | `AppointmentScheduled` | ídem |
| `ServiceOrderCompleted` | `ServiceOrderChanged` | `Completed` | ídem |
| `ServiceOrderApprovalRequested` | `ApprovalRequested` | — | `customer:{CustomerId}`, `approver:{ResponsibleApproverId}`? (**sin** taller) |
| `AlertRaised` | `AlertChanged` | `Raised` | `customer:{CustomerId}` |
| `AlertStatusChanged` | `AlertChanged` | `StatusChanged` | `customer:{CustomerId}` |
| `MaintenanceRoutineRegistered` … `MaintenanceRoutineParametersChanged` (7) | `RoutineChanged` | `Registered`, `DueSoon`, `Overdue`, `DueStatusReverted`, `Restarted`, `StateChanged`, `ParametersChanged` | `customer:{CustomerId}` |
| `OdometerReadingRecorded` | `OdometerReadingRecorded` | — | `customer:{CustomerId}` |
| `VehicleRegistered` | `VehicleChanged` | `Registered` | `customer:{CustomerId}` |
| `VehicleStatusChanged` | `VehicleChanged` | `StatusChanged` | `customer:{CustomerId}` |

`?` = solo si el valor no es nulo. **Semántica**: 1 evento → exactamente 1 llamada al cliente por audiencia, en el orden de offset (el suscriptor es secuencial, foundation D-07). Notifications no reordena, no agrupa ni retrasa. Por eso, cuando el taller agrega un servicio en `OnSite`, la web recibe primero `ServiceOrderChanged` (`OnSite → PendingApproval`) y luego `ApprovalRequested`; la UI actualiza la fila con el primero y muestra la alerta prioritaria persistente con el segundo, sin recargar dos veces.

### D-06 Contrato del hub (`RutaViva.Notifications.Hub`)

```csharp
public interface INotificationsClient
{
    Task ServiceOrderChanged(ServiceOrderNotification notification);
    Task ApprovalRequested(ApprovalRequestNotification notification);
    Task AlertChanged(AlertNotification notification);
    Task RoutineChanged(RoutineNotification notification);
    Task OdometerReadingRecorded(OdometerReadingNotification notification);
    Task VehicleChanged(VehicleNotification notification);
}

public sealed class NotificationsHub : Hub<INotificationsClient>
{
    public Task JoinCustomer(string customerId);   public Task LeaveCustomer(string customerId);
    public Task JoinApprover(string approverId);   public Task LeaveApprover(string approverId);
    public Task JoinWorkshop(string workshopId);   public Task LeaveWorkshop(string workshopId);
}
```

- `NotificationsHubMethods` expone como constantes (`nameof`) los 6 métodos del cliente y los 6 del servidor; una prueba verifica que coinciden con los miembros de las interfaces. `fleet-maintenance-ui` replica estos nombres en `core/hub-methods.ts`.
- El hub solo valida el identificador (`Guid.TryParse`, no vacío) y delega en `NotificationAudience.GroupNameFor(...)`; sin lógica adicional. Error → `HubException` con un mensaje constante.
- Ruta del hub: `/hubs/notifications` (constante `NotificationsHubRoute`).
- JSON de SignalR: `camelCase` y `JsonStringEnumConverter` (igual que las APIs, foundation D-08).

### D-07 Mensajes (`RutaViva.Notifications.Application.Messages`)

Todos son `sealed record` con `EventId` (`Guid`, = `MessageId`) y `OccurredAt`. Los enums de estado de otros servicios viajan como `string` (no se duplican enums ajenos, tolerant reader); los tipos de cambio son enums propios.

| Mensaje | Campos |
|---|---|
| `ServiceOrderNotification` | `EventId`, `OccurredAt`, `Kind` (`ServiceOrderNotificationKind`), `ServiceOrderId`, `Number`, `VehicleId`, `Plate`, `RoutineId`, `RoutineName`, `CustomerId`, `WorkshopId`, `Status`, `FromStatus?`, `ApprovalReason?`, `ApprovalPriority?`, `TotalAmount?`, `Currency?`, `AutoApproved`, `AppointmentAt?`, `CancellationReason?` |
| `ApprovalRequestNotification` | `EventId`, `OccurredAt`, `ServiceOrderId`, `Number`, `VehicleId`, `Plate`, `RoutineName`, `CustomerId`, `Reason`, `Priority`, `TotalAmount?`, `Currency?` |
| `AlertNotification` | `EventId`, `OccurredAt`, `Kind` (`AlertNotificationKind`), `AlertId`, `VehicleId`, `Plate`, `CustomerId`, `RoutineId`, `RoutineName?`, `Type`, `TriggeredBy`, `Status`, `ResolutionReason?` |
| `RoutineNotification` | `EventId`, `OccurredAt`, `Kind` (`RoutineNotificationKind`), `RoutineId`, `VehicleId`, `Plate`, `CustomerId`, `RoutineName`, `State`, `DueStatus`, `TriggeredBy?`, `CycleNumber`, `NextDueKm?`, `NextDueDate?`, `OdometerKm`, `ConsumedPercent`, `EffectiveDueSoonPercent` |
| `OdometerReadingNotification` | `EventId`, `OccurredAt`, `VehicleId`, `Plate`, `CustomerId`, `Kilometers`, `ReadAt`, `Outcome`, `IgnoredReason?`, `CurrentOdometerKm` |
| `VehicleNotification` | `EventId`, `OccurredAt`, `Kind` (`VehicleNotificationKind`), `VehicleId`, `Plate`, `CustomerId`, `Status` |

Enums propios (valores desde 1, data-model E-01): `ServiceOrderNotificationKind` (`Created`, `StatusChanged`, `LinesChanged`, `AppointmentScheduled`, `Completed`), `AlertNotificationKind` (`Raised`, `StatusChanged`), `RoutineNotificationKind` (7 valores de D-05), `VehicleNotificationKind` (`Registered`, `StatusChanged`).

### D-08 Audiencia y puerto de salida

```csharp
public sealed record NotificationAudience
{
    public IReadOnlyList<string> Groups { get; }
    public static NotificationAudience ForCustomer(Guid customerId);
    public NotificationAudience WithWorkshop(Guid workshopId);
    public NotificationAudience WithApprover(Guid? approverId);   // nulo → sin cambio
    public static string GroupNameFor(AudienceKind kind, Guid id); // "customer:{id}" | "approver:{id}" | "workshop:{id}"
}

public interface IRealtimeNotifier
{
    Task ServiceOrderChangedAsync(NotificationAudience audience, ServiceOrderNotification notification, CancellationToken ct);
    Task ApprovalRequestedAsync(NotificationAudience audience, ApprovalRequestNotification notification, CancellationToken ct);
    Task AlertChangedAsync(NotificationAudience audience, AlertNotification notification, CancellationToken ct);
    Task RoutineChangedAsync(NotificationAudience audience, RoutineNotification notification, CancellationToken ct);
    Task OdometerReadingRecordedAsync(NotificationAudience audience, OdometerReadingNotification notification, CancellationToken ct);
    Task VehicleChangedAsync(NotificationAudience audience, VehicleNotification notification, CancellationToken ct);
}
```

- `NotificationAudience` es un objeto de valor inmutable: elimina grupos repetidos y concentra la regla «quién recibe qué» en `ServiceOrderAudiencePolicy` (órdenes: dueño + taller + responsable; aprobaciones: dueño + responsable). Los prefijos de grupo son constantes de la clase.
- `SignalRRealtimeNotifier` (Api) implementa el puerto con `IHubContext<NotificationsHub, INotificationsClient>.Clients.Groups(audience.Groups)`. El puerto tiene un método por tipo de mensaje para evitar un `switch` por tipo en el adaptador (cada método es una línea).
- Una conexión que está en dos grupos de la misma audiencia puede recibir el mensaje dos veces; el `EventId` permite al cliente descartarlo (spec). No se resuelve en el servidor porque SignalR no expone la pertenencia a grupos (KISS).

### D-09 Traductores y *handlers*

- `ServiceOrderNotificationTranslator`, `AlertNotificationTranslator`, `RoutineNotificationTranslator`, `OdometerReadingNotificationTranslator`, `VehicleNotificationTranslator`: funciones puras `evento + EventMetadata → mensaje` (patrón **Translator** / **Anti-Corruption Layer** hacia la web). Se prueban sin SignalR.
- *Handlers* genéricos para no repetir código: `ServiceOrderChangedHandler<TEvent> where TEvent : IServiceOrderSnapshotEvent` y `RoutineChangedHandler<TEvent> where TEvent : IRoutineSnapshotEvent` (requieren CN-03). El `kind` sale de un mapa constante `Type → Kind` en el traductor (patrón **Registry**), no de un `switch`.
- Handlers específicos: `ApprovalRequestedHandler`, `AlertRaisedHandler`, `AlertStatusChangedHandler`, `OdometerReadingRecordedHandler`, `VehicleRegisteredHandler`, `VehicleStatusChangedHandler`.
- Cada handler: traducir → calcular audiencia → llamar al puerto → `Result.Success()`. Un fallo de SignalR se propaga como excepción y el suscriptor aplica sus reintentos (foundation D-07).

## Contratos nuevos requeridos

Ampliaciones del registro de contratos de `foundation-platform` que este change necesita. Se deben agregar allí antes de implementar las tareas 2.x:

| ID | Contrato | Motivo |
|---|---|---|
| CN-01 | `IEventCheckpointStore` (puerto en `BuildingBlocks.EventBus.Client`: `GetAsync(subscriber)`, `SaveAsync(subscriber, offset)`) con dos implementaciones: `EfEventCheckpointStore` (en la transacción del efecto) e `InMemoryEventCheckpointStore`; y que `AddEventBusSubscriber(..., StartPosition.Latest)` admita un suscriptor **sin `DbContext`** | Notifications no tiene schema ni base de datos (ADR-0013: «Notifications y Gateway no tienen schema»), pero foundation D-07 guarda el checkpoint en una transacción de EF |
| CN-02 | Semántica del `IntegrationEventDispatcher`: un evento de un stream suscrito **sin handler registrado** para su `type` se omite, avanza el checkpoint y no genera *dead letter* | Notifications ignora `MaintenancePlanStatusChanged` y `VehicleResponsibleAssigned` (spec «Eventos sin interés para la web»). Reporting y ServiceOrders necesitan lo mismo |
| CN-03 | Interfaces marcadoras en `RutaViva.IntegrationContracts`: `IRoutineSnapshotEvent { RoutineSnapshot Routine { get; } }` (las 7 de rutina) e `IServiceOrderSnapshotEvent { ServiceOrderSnapshot Order { get; } }` (las 6 de órdenes) | Un solo handler genérico por familia en Notifications y Reporting (DRY); sin ellas habría 13 handlers casi idénticos |

## Patrones aplicados

| Patrón | Dónde | Por qué |
|---|---|---|
| API Gateway / Reverse Proxy | `RutaViva.ApiGateway` | Un origen para la SPA, sin topología interna en el cliente (ADR-0016) |
| Translator / Anti-Corruption Layer | D-09 | Los contratos de integración pueden evolucionar sin romper la web |
| Adapter | `SignalRRealtimeNotifier` | Application no conoce SignalR (DIP; regla de arquitectura) |
| Value Object | `NotificationAudience` | Regla de audiencia inmutable, sin duplicados y probada en un solo lugar |
| Registry | Mapa `Type → Kind` | Sin `switch` por tipo de evento (OCP) |
| Typed Hub | `Hub<INotificationsClient>` | Nombres de métodos verificados por el compilador, sin cadenas mágicas |

## Antipatrones evitados

| Antipatrón | Cómo se evita |
|---|---|
| Gateway con lógica (BFF gordo / objeto dios) | El Gateway solo enruta, CORS y correlación; no hay endpoints propios salvo salud |
| Cadenas mágicas | Rutas del hub, métodos, prefijos de grupo y nombre del suscriptor como constantes; métodos del cliente por `nameof` |
| Copy-paste | Handlers genéricos por familia (CN-03) y audiencia centralizada |
| Fuga de modelo | La web nunca recibe un contrato de integración tal cual |
| *Lava flow* | Sin proyectos Domain/Infrastructure vacíos en Notifications |
| Números mágicos | Límite de cuerpo y orígenes CORS en configuración tipada (`GatewayOptions`) |

## Risks / Trade-offs

- [Cualquier conexión puede unirse a cualquier grupo, sin autenticación] → Aceptado por el alcance del reto; la evolución es tomar `CustomerId`/`ApproverId` del token y validar en `Join*`. Documentado en el README.
- [Avisos perdidos durante una desconexión] → La web recarga por REST al reconectar (`withAutomaticReconnect`, change `fleet-maintenance-ui`).
- [Mensajes duplicados para una conexión en dos grupos] → `eventId` para descartar en el cliente.
- [Notifications arranca después de un evento crítico] → Por diseño (`StartPosition.Latest`); el estado real siempre se lee por REST.
- [Una sola instancia de Notifications] → Sin *backplane* de SignalR (ADR-0024 NEG-003). Con varias instancias se agregaría Redis o Azure SignalR Service.
- [El Gateway es punto único de entrada] → Sin estado y sin lógica: reiniciarlo es inmediato.

## Migration Plan

No aplica (*greenfield*). Orden de arranque: EventBus → servicios de negocio → Notifications → Gateway → SPA (Aspire `WaitFor` o README).

## Open Questions

- Si Aspire se recorta, las direcciones de `appsettings.Development.json` deben coincidir con los `launchSettings.json` de cada servicio; se fijan al implementar cada servicio sin cambiar las specs.
