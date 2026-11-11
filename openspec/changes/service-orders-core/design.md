## Context

Motivación en `proposal.md`; requisitos en `specs/`. Este change construye el servicio ServiceOrders sobre `foundation-platform` (BuildingBlocks, Outbox, `EventBusSubscriberHost`, convenciones de API) y usa **exactamente** los nombres del registro de contratos de `openspec/changes/foundation-platform/design.md` (§ Streams, § Eventos de integración, § Endpoints públicos). Fuentes de diseño: ADR-0003 (máquina de estados), ADR-0011 (RN-3 en tres niveles), ADR-0019 (política), ADR-0020 (pausa), ADR-0021 (cita) y `docs/architecture/data-model.md` § `serviceorders`.

Dependencias de otros changes:
- `foundation-platform`: todo BuildingBlocks y `RutaViva.IntegrationContracts`.
- `maintenance-core` produce `maintenance.routine` (`MaintenanceRoutineOverdue` y los demás eventos con `RoutineSnapshot`).
- `supporting-contexts` produce `fleet.vehicle` y `customers.customer`.
- Para probar este change de forma aislada, las pruebas de integración publican esos eventos directamente en un EventBus en proceso.

## Goals / Non-Goals

**Goals:**
- Que toda la lógica de RN-3 y RN-4 viva en el agregado y en dos servicios de dominio, sin una sola regla en los endpoints ni en los handlers (modelo rico).
- Que la tabla de transiciones sea la **fuente única** del flujo: la usa la guarda del agregado y el cálculo de `availableActions`, de modo que la UI no duplica reglas.
- Que cada acción sea un método con intención de negocio y no un `SetStatus`.

**Non-Goals:**
- Patrón State de GoF (ADR-0003 ALT-001): se evalúa solo si los estados necesitan comportamiento propio (NEG-003).
- Sagas u orquestación con Maintenance (ADR-0011 ALT-001).

## Decisions

### D-01 Estructura del servicio

```
src/Services/ServiceOrders/
  RutaViva.ServiceOrders.Domain/
    ServiceOrders/            ServiceOrder.cs, ServiceOrderId.cs, ServiceOrderNumber.cs, ServiceLine.cs, ServiceLineId.cs,
                              ServiceLines.cs, ApprovalRequest.cs, WorkshopAppointment.cs, ServiceRecord.cs,
                              ServiceOrderHistoryEntry.cs, ServiceOrderTransitions.cs, ServiceOrderActionCatalog.cs,
                              ServiceOrderErrors.cs, Events/*.cs, Enums/*.cs
    ApprovalPolicies/         ApprovalPolicy.cs, ApprovalPolicyId.cs, ApprovalPolicyErrors.cs
    References/               VehicleReference.cs, ApproverReference.cs, RoutineReference.cs, Enums/*.cs
    Services/                 ApprovalAuthorizationPolicy.cs, AvailableActionsResolver.cs, ManualCreationEligibility.cs
    Shared/                   Money.cs, Currency.cs, Actor.cs
  RutaViva.ServiceOrders.Application/
    Abstractions/             IServiceOrderRepository.cs, IServiceOrderNumberGenerator.cs, IApprovalPolicyRepository.cs,
                              IApprovalPolicyProvider.cs, IVehicleReferenceRepository.cs, IApproverReferenceRepository.cs,
                              IRoutineReferenceRepository.cs, IServiceOrderQueries.cs, IApprovalPolicyQueries.cs
    ServiceOrders/Commands/   un command + validator por acción; ServiceOrderActionHandler<TCommand> (base)
    ServiceOrders/Queries/    GetServiceOrders, GetServiceOrderDetail, GetApprovalInbox
    ApprovalPolicies/         RegisterApprovalPolicy (command), GetApprovalPolicies (query)
    IntegrationEvents/        Handlers/*.cs (consumidores), Mappers/*.cs (dominio → contratos)
  RutaViva.ServiceOrders.Infrastructure/
    Persistence/              ServiceOrdersDbContext.cs, Configurations/*.cs, Migrations/, Repositories/*.cs, Queries/*.cs
    Numbering/                SqlSequenceServiceOrderNumberGenerator.cs
    DependencyInjection.cs
  RutaViva.ServiceOrders.Api/
    Endpoints/                ServiceOrderEndpoints.cs, WorkshopActionEndpoints.cs, ApproverActionEndpoints.cs,
                              ApprovalPolicyEndpoints.cs, Requests/*.cs
    Program.cs
```

### D-02 Enums del dominio (data-model §5, valores explícitos desde 1)

`ServiceOrderStatus` (9), `ServiceOrderOrigin`, `CancellationReason`, `ApprovalReason`, `ApprovalPriority`, `ServiceLineType`, `PricingStatus`, `ServiceLineApprovalStatus`, `ServiceLineRequester`, `ServiceOrderChangeType`, y además:

```csharp
public enum ServiceOrderAction
{
    Quote = 1, AutoApprove = 2, RequestApproval = 3, Approve = 4, Reject = 5,
    RequestAdditionalService = 6, PriceAdditionalService = 7, ScheduleAppointment = 8,
    RescheduleAppointment = 9, MarkOnSite = 10, AddAdditionalService = 11, Start = 12,
    Complete = 13, Cancel = 14, Hold = 15, Resume = 16
}
public enum ActorProfile { Workshop = 1, Approver = 2, System = 3, Operator = 4 }
```

Las réplicas usan su propia copia de `VehicleStatus`, `ApproverStatus`, `RoutineState` y `RoutineDueStatus` en `References/Enums` (ADR-0010: no se comparten tipos de dominio).

### D-03 Tabla declarativa de transiciones (fuente única)

`ServiceOrderTransitions` es una clase estática con un `FrozenDictionary<ServiceOrderStatus, FrozenSet<ServiceOrderAction>>` y conjuntos con nombre (data-model E-06: nunca comparar el orden numérico):

| Estado | Acciones |
|---|---|
| `Created` | `Quote`, `Cancel`, `Hold` |
| `Quoted` | `AutoApprove`, `RequestApproval` |
| `PendingApproval` | `Approve`, `Reject`, `RequestAdditionalService`, `PriceAdditionalService`, `Cancel`, `Hold` |
| `Approved` | `ScheduleAppointment`, `RescheduleAppointment`, `MarkOnSite`, `Cancel`, `Hold` |
| `OnSite` | `AddAdditionalService`, `Start`, `Cancel`, `Hold` |
| `InProgress` | `Complete`, `Hold` |
| `OnHold` | `Resume`, `Cancel` |
| `Completed`, `Cancelled` | — |

```csharp
public static class ServiceOrderTransitions
{
    public static readonly FrozenSet<ServiceOrderStatus> OpenStatuses;            // todos menos Completed y Cancelled
    public static readonly FrozenSet<ServiceOrderStatus> ClosedStatuses;          // Completed, Cancelled
    public static readonly FrozenSet<ServiceOrderStatus> HoldableStatuses;        // Created, PendingApproval, Approved, OnSite, InProgress
    public static readonly FrozenSet<ServiceOrderStatus> CancellableStatuses;     // Created, PendingApproval, Approved, OnSite
    public static bool Allows(ServiceOrderStatus status, ServiceOrderAction action);
}
```

`ServiceOrderActionCatalog` asocia cada acción a su único actor (`ActorProfile`), según la spec `service-order-lifecycle`.

Las **guardas contextuales** que no dependen solo del estado viven en el agregado como métodos privados de una línea, evaluados después de `Allows`:

| Acción | Guarda | Error |
|---|---|---|
| `Approve` | sin líneas `PendingPricing` | `ServiceOrder.PendingPricingLines` |
| `RequestAdditionalService` | `ApprovalRequest.Reason == Quote` | `ServiceOrder.InvalidTransition` |
| `PriceAdditionalService` | la línea existe y está `PendingPricing` | `ServiceOrder.LineNotFound`, `ServiceOrder.LineAlreadyPriced` |
| `ScheduleAppointment` / `RescheduleAppointment` | sin cita / con cita (el command decide cuál según el estado de la cita) | — |
| `MarkOnSite` | cita registrada | `ServiceOrder.AppointmentRequired` |
| `Cancel` en `OnHold` | `StatusBeforeHold ∈ CancellableStatuses` | `ServiceOrder.InvalidTransition` |

### D-04 Agregado `ServiceOrder` (métodos con intención)

```csharp
public sealed class ServiceOrder : AggregateRoot<ServiceOrderId>, IHasRowVersion
{
    public static ServiceOrder CreateAutomatic(ServiceOrderNumber number, RoutineOverdueFacts facts, VehicleReference vehicle, DateTimeOffset now);
    public static Result<ServiceOrder> CreateManual(ServiceOrderNumber number, RoutineReference routine, VehicleReference vehicle, DateTimeOffset now);

    public Result Quote(WorkshopId workshop, IReadOnlyCollection<QuotedLine> lines, ApprovalPolicy policy, DateTimeOffset now);
    public Result Approve(ApproverAuthorization authorization, DateTimeOffset now);
    public Result Reject(ApproverAuthorization authorization, DateTimeOffset now);
    public Result Cancel(ApproverAuthorization authorization, DateTimeOffset now);
    public Result RequestAdditionalService(ApproverAuthorization authorization, ServiceDescription description, DateTimeOffset now);
    public Result PriceAdditionalService(WorkshopId workshop, ServiceLineId lineId, Money amount, DateTimeOffset now);
    public Result AddAdditionalService(WorkshopId workshop, ServiceDescription description, Money amount, DateTimeOffset now);
    public Result ScheduleAppointment(WorkshopId workshop, WorkshopAppointment appointment, DateTimeOffset now);
    public Result MarkOnSite(WorkshopId workshop, DateTimeOffset now);
    public Result Start(WorkshopId workshop, DateTimeOffset now);
    public Result Complete(WorkshopId workshop, ServiceRecord record, DateTimeOffset now);
    public void Hold(DateTimeOffset now);      // idempotente; sin Result: lo invoca el sistema por evento
    public void Resume(DateTimeOffset now);    // idempotente
    public IReadOnlyList<AvailableAction> AvailableActionsFor(ActorContext actor);
}

public sealed record AvailableAction(ServiceOrderAction Action, bool Enabled, string? BlockedReason);
public sealed record ActorContext(ActorProfile Profile, Guid? ActorId, ApproverAuthorization? Authorization);
```

- Cada método sigue la misma forma corta: `EnsureCanExecute(action)` → guardas contextuales → mutación → `TransitionTo(target, actor, now)` o `RecordHistory(...)` → `Raise(...)`. `EnsureCanExecute(ServiceOrderAction)` es la **única** guarda de estado (ADR-0003).
- `Quote` registra `Created → Quoted` y ejecuta internamente `AutoApprove` o `RequestApproval` (A-14); copia `policy.AutoApprovalMaxAmount` en `AutoApprovalMaxAmountApplied` (D-05 del data-model).
- `ApproverAuthorization` es un VO que solo puede construir `ApprovalAuthorizationPolicy` (constructor `internal`); así el agregado no puede aprobarse sin pasar por la política (hace ilegal el estado ilegal).
- `ServiceLines` es una **colección de primera clase** (lista privada de `ServiceLine`): `Total()`, `RoutineAmount()`, `AdditionalAmount()`, `HasPendingPricing()`, `ApprovePendingAdditional()`, `RejectPendingAdditional()`. Evita que `ServiceOrder` se convierta en objeto dios.
- `ApprovalRequest` (VO `Reason`, `Priority`) decide el destino: `ApprovalRequest.ResolveApprovedStatus()` devuelve `Approved` u `OnSite`, y `ResolveRejectedStatus()` devuelve `Cancelled` u `OnSite`. La regla de «a dónde vuelve la orden» vive en el VO, no en un `if` del agregado.
- `Money` (`record`, `Amount` decimal ≥ 0 con 2 decimales, `Currency` ISO 4217; sumar monedas distintas devuelve `Money.CurrencyMismatch`). `ServiceRecord` (`ServicedKm` ≥ 0, `ServicedAt` no futura respecto a `now`). `WorkshopAppointment` (`ScheduledAt` > `now`, `Notes` ≤ 250). `ServiceOrderNumber` formatea `OS-{n:D6}`.

### D-05 Servicios de dominio y especificaciones

| Servicio | Responsabilidad | Patrón |
|---|---|---|
| `ApprovalAuthorizationPolicy` | `Result<ApproverAuthorization> Authorize(ApproverReference approver, VehicleReference vehicle)`: aprobador activo **y** (mismo `CustomerId` que el dueño **o** `ApproverId == ResponsibleApproverId`) | Policy (Evans) |
| `ManualCreationEligibility` | Especificaciones `RoutineIsOverdueAndActive`, `VehicleIsActive` combinadas; devuelve el primer error | Specification |
| `AvailableActionsResolver` | Filtra por `Allows(status, action)` ∩ actor de la acción; si el actor no está autorizado (aprobador) o no es el taller asignado, devuelve lista vacía; luego evalúa las guardas contextuales y marca `Enabled = false` con `BlockedReason = Error.Code` (`ServiceOrder.PendingPricingLines`, `ServiceOrder.AppointmentRequired`) en lugar de ocultar la acción; elige `ScheduleAppointment` o `RescheduleAppointment` según exista cita; nunca incluye acciones de `System` | Usa la misma tabla y las mismas guardas que el agregado (DRY): la guarda devuelve `Result` y el resolver solo lee su `Error` |

### D-06 `ApprovalPolicy` (agregado de solo inserción)

`ApprovalPolicy.Register(Currency currency, decimal autoApprovalMaxAmount, DateTimeOffset effectiveFrom, string createdBy, DateTimeOffset now)` devuelve `Result<ApprovalPolicy>` (`ApprovalPolicy.InvalidAmount` si < 0). Sin métodos de modificación. `bool AllowsAutoApproval(Money total)` = `total.Amount <= AutoApprovalMaxAmount` (moneda igual). Semilla por migración (`HasData`): 1.500.000 COP vigente desde `2026-01-01`. `IApprovalPolicyProvider.GetCurrentAsync(Currency, DateTimeOffset asOf)` consulta `TOP (1) … WHERE EffectiveFrom <= @asOf ORDER BY EffectiveFrom DESC` sin caché (ADR-0019 IMP-002).

### D-07 Application: commands, queries y handler base

Commands (uno por acción, cada uno con su `AbstractValidator`):
`CreateManualServiceOrderCommand`, `QuoteServiceOrderCommand`, `RequestAdditionalServiceCommand`, `PriceAdditionalServiceCommand`, `AddAdditionalServiceCommand`, `ScheduleAppointmentCommand`, `MarkOnSiteCommand`, `StartServiceOrderCommand`, `CompleteServiceOrderCommand`, `ApproveServiceOrderCommand`, `RejectServiceOrderCommand`, `CancelServiceOrderCommand`, `RegisterApprovalPolicyCommand`.

`ServiceOrderActionHandler<TCommand>` (patrón **Template Method**): `HandleAsync` = cargar la orden (`ServiceOrder.NotFound`) → `ExecuteAsync(order, command, ct)` abstracto → `IUnitOfWork.SaveChangesAsync`. Las subclases de acciones del aprobador cargan además `ApproverReference` y `VehicleReference` y piden la autorización. Así no se repite el esqueleto en 12 handlers (DRY) y cada handler concreto tiene 3 a 6 líneas.

Queries (lectura directa a DTOs por `IServiceOrderQueries`, `AsNoTracking`):
- `GetServiceOrdersQuery(status?, statusGroup?, customerId?, vehicleId?, routineId?, workshopId?, origin?, page, pageSize)` → `PagedResult<ServiceOrderSummaryDto>`. `ServiceOrderStatusGroup` (`PendingApproval`, `Open`, `Closed`, `All`) se traduce a conjuntos con nombre de `ServiceOrderTransitions` (`OpenStatuses`, `ClosedStatuses`), nunca a comparaciones de orden (CR-04).
- `GetServiceOrderDetailQuery(id, actor, actorId?)` → `ServiceOrderDetailDto` con líneas, historial y `availableActions: [{ action, enabled, blockedReason }]` (CR-05). Rehidrata el agregado y llama a `AvailableActionsResolver`; es la única query que pasa por el dominio porque las acciones son una regla.
- `GetApprovalInboxQuery(approverId)` → órdenes `PendingApproval` de los clientes del aprobador o de los vehículos donde es responsable, `ORDER BY ApprovalPriority DESC, UpdatedAt ASC` sobre `IX_ServiceOrders_ApprovalInbox` (ADR-0003 IMP-003).
- `GetApprovalPoliciesQuery(currency)` → historial con `isCurrent`.

### D-08 Consumidores de integración (suscriptor `serviceorders`)

Registrados con `AddEventBusSubscriber("serviceorders", StartPosition.Checkpoint)`:

| Stream | Evento | Handler | Efecto |
|---|---|---|---|
| `fleet.vehicle` | `VehicleRegistered` | `VehicleRegisteredHandler` | Upsert de `VehicleReference` |
| `fleet.vehicle` | `VehicleStatusChanged` | `VehicleStatusChangedHandler` | Actualiza la réplica y llama `Hold()` o `Resume()` en `GetOpenByVehicleAsync` |
| `fleet.vehicle` | `VehicleResponsibleAssigned` | `VehicleAssignmentHandler` | Actualiza la réplica |
| `customers.customer` | `ApproverRegistered`, `ApproverStatusChanged` | `ApproverReferenceHandler` | Upsert de `ApproverReference` |
| `maintenance.routine` | `MaintenanceRoutineRegistered`, `MaintenanceRoutineDueSoon`, `MaintenanceRoutineDueStatusReverted`, `MaintenanceRoutineRestarted`, `MaintenanceRoutineStateChanged`, `MaintenanceRoutineParametersChanged` | `RoutineSnapshotHandler` (uno por tipo, delegan en `RoutineReferenceProjector`) | Upsert de `RoutineReference` desde `RoutineSnapshot` |
| `maintenance.routine` | `MaintenanceRoutineOverdue` | `MaintenanceRoutineOverdueHandler` | Proyecta la réplica y crea la orden automática |

`MaintenanceRoutineOverdueHandler`: proyecta la réplica → si `ExistsOpenAsync(vehicleId, routineId)` devuelve `true`, registra `DuplicateOpenServiceOrder` y termina con éxito (nivel 2 rápido) → `CreateAutomatic` (en `OnHold` si la réplica del vehículo está `Inactive`) → guardar. Si el guardado viola `UX_ServiceOrders_OneOpenPerRoutine` (2601/2627), el repositorio devuelve `ServiceOrder.DuplicateOpenServiceOrder` y el handler lo trata como éxito sin reintento (nivel 3, ADR-0011 IMP-003). El nivel 1 (checkpoint por offset) lo da `EventBusSubscriberHost`.

Orden garantizado: el EventBus tiene orden global por offset (ADR-0024 POS-004) y Fleet publica `VehicleRegistered` antes de que Maintenance registre rutinas, así que la réplica del vehículo existe cuando llega un vencimiento. Si no existiera, el handler devuelve `ServiceOrder.VehicleReferenceMissing`, se reintenta 3 veces y termina en *dead letter* (visible y diagnosticable, nunca silencioso).

`MaintenanceRoutineStateChanged` solo actualiza la réplica: **no** toca órdenes (A-17).

### D-09 Mappers de dominio a integración y semántica de eventos

Un `IIntegrationEventMapper<T>` por evento de dominio, todos construyen el `ServiceOrderSnapshot` con un único `ServiceOrderSnapshotFactory` (DRY). `Plate` y `ResponsibleApproverId` se copian en la orden al crearla (desde `VehicleReference`) para que el snapshot no consulte réplicas.

Semántica de los montos del snapshot (Reporting CR-01), calculada por `ServiceLines`:
- `RoutineAmount` = suma de las líneas `Routine` con `PricingStatus = Priced`.
- `AdditionalAmount` = suma de las líneas `Additional` con `PricingStatus = Priced` **y** `ApprovalStatus = Approved` (las pendientes de aprobación o rechazadas no cuentan).
- `TotalAmount` = suma de las líneas con precio que no están `Rejected` (el valor que ve el aprobador al decidir, ADR-0003 §1). Los tres son nulos antes de la cotización.

Semántica temporal (Reporting CR-02): cada transición produce su propio evento de dominio `ServiceOrderStatusChangedDomainEvent` con el `OccurredAt` del instante en que ocurrió (el `now` de la operación), incluida la transitoria `Created → Quoted`, que se emite **antes** de `Quoted → Approved` o `Quoted → PendingApproval`. Ambas comparten instante por ocurrir en la misma operación; su orden lo fija el offset. Ninguna transición se colapsa en un solo evento.

| Operación | Eventos publicados, en este orden (misma transacción, offsets consecutivos) |
|---|---|
| Crear (automática o manual) | `ServiceOrderCreated` (+ `ServiceOrderStatusChanged` `Created → OnHold` si nace en pausa) |
| `Quote` ≤ umbral | `ServiceOrderStatusChanged` (`Created → Quoted`), `ServiceOrderStatusChanged` (`Quoted → Approved`) |
| `Quote` > umbral | `ServiceOrderStatusChanged` (`Created → Quoted`), `ServiceOrderStatusChanged` (`Quoted → PendingApproval`), `ServiceOrderApprovalRequested` (`Quote`, `Normal`) |
| `RequestAdditionalService`, `PriceAdditionalService` | `ServiceOrderLinesChanged` |
| `AddAdditionalService` | `ServiceOrderLinesChanged`, `ServiceOrderStatusChanged` (`OnSite → PendingApproval`), `ServiceOrderApprovalRequested` (`AdditionalServices`, `High`) |
| `Approve` / `Reject` con `AdditionalServices` | `ServiceOrderLinesChanged`, `ServiceOrderStatusChanged` (`PendingApproval → OnSite`) |
| `Approve` / `Reject` con `Quote`, `Cancel`, `MarkOnSite`, `Start`, `Hold`, `Resume` | `ServiceOrderStatusChanged` |
| `ScheduleAppointment` | `ServiceOrderAppointmentScheduled` |
| `Complete` | `ServiceOrderStatusChanged` (`InProgress → Completed`), `ServiceOrderCompleted` |

Reacciones esperadas: Maintenance reinicia la rutina **solo** con `ServiceOrderCompleted` (no con `ServiceOrderStatusChanged`); Notifications usa `ServiceOrderApprovalRequested` para la alerta prioritaria y `ServiceOrderStatusChanged`/`ServiceOrderLinesChanged` para refrescar la orden; Reporting usa el snapshot de todos.

### D-10 Persistencia

- `ServiceOrdersDbContext` con `HasDefaultSchema("serviceorders")` y `ApplyRutaVivaConventions()`.
- `ServiceOrder` mapeado a `ServiceOrders`; `ServiceLine` y `ServiceOrderHistoryEntry` como entidades propias con FK y `ON DELETE CASCADE`; VOs como *complex types* (`Money`, `ApprovalRequest`, `WorkshopAppointment`, `ServiceRecord`).
- `Number` con `HasDefaultValueSql("NEXT VALUE FOR serviceorders.ServiceOrderNumbers")` no sirve para el dominio (el número se necesita antes de guardar): `SqlSequenceServiceOrderNumberGenerator` ejecuta `SELECT NEXT VALUE FOR …` y lo pasa a la factoría.
- Restricciones del script de referencia por migración: `UX_ServiceOrders_OneOpenPerRoutine` (filtrado con los 7 estados abiertos), `IX_ServiceOrders_ApprovalInbox`, `CK_ServiceOrders_HoldData`, `CK_ServiceOrders_AppointmentRequired`, `CK_ServiceOrders_QuoteConsistency`, `CK_ServiceOrders_CompletionData`, `CK_ServiceOrders_CancellationData`, y un `CHECK` por enum.
- Columnas nuevas frente al script de referencia (se agregan en la migración y se informan para el script, tarea de `foundation-platform` 8.2): `Plate varchar(10)` y `ResponsibleApproverId uniqueidentifier null` en `ServiceOrders` (los exige el `ServiceOrderSnapshot`).

### D-11 API

Endpoints del registro de contratos agrupados en 4 módulos `IEndpointModule`. Requests:

| Endpoint | Body |
|---|---|
| `POST /api/v1/service-orders` | `{ vehicleId, routineId, requestedBy }` |
| `POST …/{id}/quote` | `{ workshopId, currency, lines: [{ description, amount }] }` |
| `POST …/{id}/additional-services/requests` | `{ approverId, description }` |
| `POST …/{id}/additional-services/{lineId}/price` | `{ workshopId, amount }` |
| `POST …/{id}/additional-services` | `{ workshopId, description, amount }` |
| `PUT …/{id}/appointment` | `{ workshopId, scheduledAt, notes? }` |
| `POST …/{id}/on-site`, `/start` | `{ workshopId }` |
| `POST …/{id}/complete` | `{ workshopId, servicedKm, servicedAt }` |
| `POST …/{id}/approve`, `/reject`, `/cancel` | `{ approverId }` |
| `POST /api/v1/service-orders/approval-policies` | `{ currency, autoApprovalMaxAmount, effectiveFrom?, createdBy }` |

El `approverId` y el `workshopId` del body identifican al actor (CR-06): sin autenticación, son la entrada de `ApprovalAuthorizationPolicy` y de la verificación del taller asignado. `GET /api/v1/service-orders/{id}?actor=Approver|Workshop|Operator&actorId=` y `GET /api/v1/service-orders?status=&statusGroup=&customerId=&vehicleId=&routineId=&workshopId=&origin=&page=&pageSize=` completan la API de lectura. Los endpoints solo traducen request → command → `ToHttpResult()`. Comandos de acción responden `200` con el `ServiceOrderDetailDto` del perfil que actuó (la UI repinta sin otra llamada); la creación manual responde `201` con `Location`.

## Registro de contratos que consume y produce

| Tipo | Contrato | Origen |
|---|---|---|
| Consume | `VehicleRegisteredV1`, `VehicleStatusChangedV1`, `VehicleResponsibleAssignedV1` | `foundation-platform` § fleet.vehicle |
| Consume | `ApproverRegisteredV1`, `ApproverStatusChangedV1` | § customers.customer |
| Consume | `MaintenanceRoutineRegisteredV1`, `MaintenanceRoutineDueSoonV1`, `MaintenanceRoutineOverdueV1`, `MaintenanceRoutineDueStatusRevertedV1`, `MaintenanceRoutineRestartedV1`, `MaintenanceRoutineStateChangedV1`, `MaintenanceRoutineParametersChangedV1` | § maintenance.routine |
| Produce | `ServiceOrderCreatedV1`, `ServiceOrderStatusChangedV1`, `ServiceOrderApprovalRequestedV1`, `ServiceOrderLinesChangedV1`, `ServiceOrderAppointmentScheduledV1`, `ServiceOrderCompletedV1` | § serviceorders.service-order |
| Produce | Endpoints de ServiceOrders | § Endpoints públicos |

### Contratos nuevos requeridos

Ninguno. Observación para `foundation-platform`: el registro no fija los nombres de los campos del request de cada endpoint; D-11 los define para ServiceOrders.

## Patrones aplicados

| Patrón | Dónde | Por qué |
|---|---|---|
| Máquina de estados por tabla declarativa | D-03 | 9 estados que solo permiten o niegan acciones: una tabla es más legible que 9 clases (ADR-0003 ALT-001, KISS) |
| Policy | `ApprovalAuthorizationPolicy`, `ApprovalPolicy` | Reglas que cambian por negocio aisladas del agregado |
| Specification | `ManualCreationEligibility` | Condiciones combinables y probables por separado |
| First-class collection | `ServiceLines` | Totales y estados de líneas fuera del agregado raíz |
| Value Object | `Money`, `ApprovalRequest`, `WorkshopAppointment`, `ServiceRecord`, `ServiceOrderNumber` | Invariantes en la construcción |
| Template Method | `ServiceOrderActionHandler<TCommand>` | Esqueleto cargar → actuar → guardar sin copy-paste |
| Factory Method | `ServiceOrder.CreateAutomatic`, `CreateManual` | Dos orígenes con invariantes distintas |
| Anti-Corruption (salida) | Mappers | El modelo interno no se filtra a los contratos |

## Antipatrones evitados

| Antipatrón | Cómo se evita |
|---|---|
| Modelo anémico | Sin `SetStatus`; cada transición es un método con intención y guardas propias |
| Objeto dios | Líneas en `ServiceLines`, destino de la aprobación en `ApprovalRequest`, autorización en una política, acciones disponibles en un resolver |
| Reglas duplicadas en la UI | `availableActions` sale de la misma tabla que la guarda |
| Números mágicos | Umbral en `ApprovalPolicies`; largo de notas, formato `OS-` y dígitos como constantes |
| Comparar enums por orden | Conjuntos con nombre (`CancellableStatuses`, `OpenStatuses`) |
| Check-then-act distribuido | RN-3 se cierra en la BD con el índice único, no consultando a otro servicio |

## Risks / Trade-offs

- [Réplica desactualizada: un aprobador revocado aprueba en la ventana de propagación] → Aceptado (ADR-0011 NEG-003); ventana de milisegundos a segundos.
- [La tabla de 9 × 16 pierde legibilidad si crecen los estados] → Prueba parametrizada sobre la tabla y migración a State documentada (ADR-0003 NEG-003).
- [El número de la `SEQUENCE` se consume aunque la creación falle por duplicado] → Aceptado: los huecos en la numeración no tienen significado de negocio.
- [Recortar la cita deja la guarda `AppointmentRequired` sin forma de cumplirse] → La guarda vive en la tarea recortable 9.1; si se recorta, `MarkOnSite` no la incluye.
- [Recortar los servicios adicionales] → La tabla conserva las acciones, pero sin endpoint ni método; la prueba 9 × 16 se ajusta en la misma tarea recortable.

## Migration Plan

Migración EF Core inicial `InitialServiceOrders` con la semilla de la política. Se aplica con el usuario administrativo (ADR-0013). Sin datos previos.
