## Context

Motivación en `proposal.md`; requisitos en `specs/`. Este change construye el primer servicio de negocio sobre `foundation-platform` y usa sin redefinir su registro de contratos: streams (`foundation-platform/design.md#streams`), eventos (`#eventos-de-integración`), endpoints (`#endpoints-públicos`) y contrato masivo (`#contrato-del-lote-masivo-bulkdryrun`). Las piezas transversales (Outbox, interceptor de eventos de dominio, suscriptor, decoradores, `ResultExtensions`) vienen de BuildingBlocks.

## Goals / Non-Goals

**Goals:**
- Una única función de evaluación, pura y probada sin mocks, que sirva a todos los disparadores (lectura, revisión por tiempo, cambio de parámetros, activación, reanudación, carga) y a las consultas.
- Todas las invariantes de RN-1, RN-2, RN-5, RN-6 y A-16 dentro del agregado `MaintenancePlan`; la de «una alerta por ciclo y tipo» en `Alert` y en un índice único.
- Ningún host (API, scheduler, suscriptor) con lógica: solo traducen a commands.

**Non-Goals:**
- Historial de cambios de parámetros (solo `UpdatedBy`/`UpdatedAt`).
- Cache de parámetros: se leen siempre de la BD (ADR-0019).

## Decisions

### D-01 Estructura del servicio

```
src/Services/Maintenance/
  RutaViva.Maintenance.Domain/
    Common/            MaintenanceTimeZone, MaintenanceErrors, ids (VehicleId, RoutineId, RoutineTypeId, AlertId, CustomerId)
    RoutineTypes/      RoutineType, RoutineTypeStatus, RoutineTypeParameters, RoutineParameterCatalog, Events/
    Plans/             MaintenancePlan, MaintenanceRoutine, enums, VOs, Events/
    Plans/Evaluation/  IDueDimensionEvaluator, DistanceDueEvaluator, TimeDueEvaluator, DimensionAssessment,
                       RoutineAssessment, DueStatusPolicy, RoutineTransitionPolicy, RoutineTransition
    Alerts/            Alert, AlertType, AlertTrigger, AlertStatus, AlertResolutionReason, Events/
  RutaViva.Maintenance.Application/
    Abstractions/      puertos (repositorios, queries, log de lecturas)
    Telemetry/         IngestTelemetryReadingCommand + handler, TelemetryReadingTranslator, RecordOdometerReading*
    Evaluations/       ReevaluateMaintenancePlansCommand + handler, ReevaluationSummary
    RoutineTypes/      UpdateRoutineTypeParametersCommand + handler + validator, queries
    Routines/          RegisterRoutinesBulkCommand, UpdateRoutineParametersCommand, ChangeRoutineStateCommand (+ handlers, validators)
    Plans/             CreateMaintenancePlanCommand, ChangePlanStatusCommand, RestartRoutineCommand, queries de plan
    Alerts/            AcknowledgeAlertCommand, handlers de eventos de dominio de alertas, queries
    IntegrationEvents/ Inbound/ (handlers de fleet y serviceorders) · Outbound/ (mappers dominio → V1)
  RutaViva.Maintenance.Infrastructure/
    Persistence/       MaintenanceDbContext, Configurations/, Migrations/, repositorios, queries
    Scheduling/        DailyTimeEvaluationScheduler
    DependencyInjection.cs
  RutaViva.Maintenance.Api/
    Endpoints/         TelemetryEndpoints, TimeEvaluationEndpoints, PlanEndpoints, RoutineEndpoints, RoutineTypeEndpoints, AlertEndpoints
    Contracts/         requests y responses HTTP (incluye TelemetryOdometerReadingRequest)
    Program.cs
```

### D-02 Modelo de dominio

```csharp
public sealed class RoutineType : AggregateRoot<RoutineTypeId>
{
    public RoutineTypeCode Code { get; }  public string Name { get; }
    public MaintenanceInterval DefaultInterval { get; }  public DueSoonThreshold DueSoonThreshold { get; }
    public RoutineTypeStatus Status { get; }  public string UpdatedBy { get; }  public DateTimeOffset UpdatedAt { get; }
    public Result UpdateParameters(MaintenanceInterval interval, DueSoonThreshold threshold, string updatedBy, DateTimeOffset at);
    public RoutineTypeParameters ToParameters();                     // raise RoutineTypeParametersChanged en UpdateParameters
}

public sealed class MaintenancePlan : AggregateRoot<VehicleId>
{
    public LicensePlate Plate { get; }  public CustomerId CustomerId { get; }  public MaintenancePlanStatus Status { get; }
    public Odometer Odometer { get; }  public DateOnly LastTimeEvaluationDate { get; }
    public IReadOnlyCollection<MaintenanceRoutine> Routines { get; }
    public bool CanBeEvaluated { get; }                              // Status == Active (ADR-0020 IMP-002)
    public static MaintenancePlan Create(VehicleId id, LicensePlate plate, CustomerId customerId, MaintenancePlanStatus status, Odometer initial, DateOnly registeredOn);
    public Result<RoutineId> RegisterRoutine(RoutineRegistration registration, RoutineParameterCatalog catalog, DateOnly today);
    public OdometerReadingResult RecordOdometerReading(OdometerReading reading, RoutineParameterCatalog catalog);
    public ReevaluationOutcome Reevaluate(DateOnly asOfDate, RoutineParameterCatalog catalog);
    public Result UpdateRoutineOverrides(RoutineId id, RoutineOverrides overrides, string updatedBy, RoutineParameterCatalog catalog, DateOnly today);
    public Result ChangeRoutineState(RoutineId id, RoutineState state, string updatedBy, RoutineParameterCatalog catalog, DateOnly today);
    public Result RestartRoutine(RoutineId id, ServiceRecord service, Guid serviceOrderId, RoutineParameterCatalog catalog);
    public void Suspend();
    public ReevaluationOutcome Resume(DateOnly today, RoutineParameterCatalog catalog);
}

public sealed class MaintenanceRoutine : Entity<RoutineId>
{
    public RoutineTypeId RoutineTypeId { get; }  public RoutineOverrides Overrides { get; }
    public RoutineState State { get; }  public RoutineDueStatus DueStatus { get; }
    public MaintenanceCycle Cycle { get; }  public bool DueSoonNotified { get; }
    internal RoutineTransition ApplyAssessment(RoutineAssessment assessment);     // usa RoutineTransitionPolicy
    internal EffectiveRoutineParameters ResolveParameters(RoutineTypeParameters type);
}

public sealed class Alert : AggregateRoot<AlertId>
{
    public static Alert Raise(AlertOrigin origin, AlertType type, AlertTrigger trigger, DateTimeOffset raisedAt);
    public Result Acknowledge(string acknowledgedBy, DateTimeOffset at);          // solo Active
    public void Resolve(AlertResolutionReason reason, DateTimeOffset at);         // idempotente si ya Resolved
}
```

Objetos de valor (`sealed record`, validan en una fábrica `Create` que devuelve `Result<T>`):

| VO | Contenido e invariantes |
|---|---|
| `LicensePlate` | Normaliza a mayúsculas sin espacios; patrón `^[A-Z]{3}[0-9]{2}[0-9A-Z]$` |
| `Odometer` | `Kilometers` ≥ 0, `ReadAt`; `bool IsAdvancedBy(OdometerReading reading)` (RN-6) |
| `OdometerReading` | `Kilometers` ≥ 0, `ReadAt` |
| `MaintenanceInterval` | `int? Kilometers` > 0, `int? Months` > 0, al menos uno |
| `DueSoonThreshold` | `decimal Percent` con `0 < Percent < 100` |
| `RoutineOverrides` | `int? IntervalKm`, `int? IntervalMonths`, `decimal? DueSoonPercent` (nulo = usar el tipo) |
| `EffectiveRoutineParameters` | `MaintenanceInterval`, `DueSoonThreshold` y el origen de cada valor (`ParameterSource.RoutineType` / `Override`) |
| `MaintenanceCycle` | `Number` ≥ 1, `StartKm` ≥ 0, `StartDate`; `MaintenanceCycle Next(ServiceRecord service)` |
| `NextDue` | `long? Kilometers`, `DateOnly? Date` |
| `ServiceRecord` | `Kilometers` ≥ 0, `Date` |
| `EvaluationContext` | `long OdometerKm`, `DateOnly EffectiveDate` |
| `RoutineParameterCatalog` | Mapa inmutable `RoutineTypeId → RoutineTypeParameters`; `Result<RoutineTypeParameters> Find(RoutineTypeId)` y `FindByCode(RoutineTypeCode)` |

### D-03 Evaluación: Strategy por dimensión + Policy de combinación + Policy de transición

1. **Strategy** `IDueDimensionEvaluator` con `DistanceDueEvaluator` y `TimeDueEvaluator`: `DimensionAssessment? Assess(MaintenanceCycle cycle, EffectiveRoutineParameters parameters, EvaluationContext context)`. Devuelve `null` si la dimensión no tiene intervalo. `DimensionAssessment(DueDimension Dimension, RoutineDueStatus Status, decimal ConsumedPercent, long Remaining, decimal DueSoonWindowPercent)`.
   - Km: `nextDueKm = StartKm + IntervalKm`; `remaining = nextDueKm - OdometerKm`; `window = IntervalKm × % / 100` (decimal, sin redondeo); `Overdue` si `remaining ≤ 0`, `DueSoon` si `remaining ≤ window`, si no `UpToDate`. Casos 8.999/9.000/10.000 del spec.
   - Tiempo: `nextDueDate = StartDate.AddMonths(IntervalMonths)` (meses de calendario); `totalDays = nextDueDate − StartDate`; `remaining = nextDueDate − EffectiveDate` en días; `window = floor(totalDays × % / 100)`. 184 × 10 % → 18; 365 × 10 % → 36.
2. **Policy** `DueStatusPolicy.Combine(IReadOnlyList<DimensionAssessment>) → RoutineAssessment`: estado = el más severo (conjunto ordenado explícito `Severity`, no el valor numérico del enum, E-06); `TriggeredBy` = dimensiones que alcanzan ese estado (`Distance`, `Time`, `DistanceAndTime`); `ConsumedPercent` = **máximo** entre dimensiones presentes (puede superar 100; semántica acordada con `reporting` CR-03); `NextDue` con ambas fechas.
3. **Policy** `RoutineTransitionPolicy.Decide(RoutineDueStatus current, RoutineDueStatus assessed, bool dueSoonNotified) → RoutineTransition` con una tabla declarativa:

| Actual | Evaluado | Transición | Efecto |
|---|---|---|---|
| `UpToDate` | `DueSoon` | `BecameDueSoon` | evento; alerta solo si `!DueSoonNotified` |
| `UpToDate`/`DueSoon` | `Overdue` | `BecameOverdue` | evento; salto directo sin `DueSoon` (A-03) |
| `DueSoon` | `UpToDate` | `RevertedToUpToDate` | solo posible por parámetros: km y tiempo son monótonos (A-16) |
| `Overdue` | cualquiera | `None` | nunca retrocede (A-16) |
| igual | igual | `None` | sin eventos |

Como km (RN-6) y tiempo (ADR-0017) son monótonos, una bajada de estado solo puede venir de un cambio de parámetros; por eso no hace falta un «modo» de evaluación: todos los disparadores usan la misma política y el motivo de la reversión es siempre `ParametersChanged`.

La consulta de plan (CQRS) reutiliza las mismas estrategias y políticas puras sobre los datos leídos con `AsNoTracking`, para que la UI no calcule (fleet-maintenance-ui CR-02) y no exista una segunda fórmula (DRY).

### D-04 Fecha efectiva por disparador

| Disparador | `OdometerKm` | `EffectiveDate` | ¿Avanza `LastTimeEvaluationDate`? |
|---|---|---|---|
| Lectura aceptada | nueva lectura | `LastTimeEvaluationDate` | No |
| Revisión por tiempo / «Actualizar» | odómetro actual | `max(asOf, Last)` | Sí |
| Cambio de parámetros, activación, reanudación, alta de rutina | odómetro actual | `max(hoy Bogotá, Last)` | Sí |
| Reinicio (RN-5) | `max(odómetro, km del servicio)` | `max(fecha del servicio, Last)` | No |

`hoy` lo calcula Application con `IClock.TodayIn(MaintenanceTimeZone.Bogota)`; el dominio solo recibe fechas (ADR-0017 IMP-001).

### D-05 Eventos de dominio y semántica

Eventos de dominio (`Plans/Events`, `Alerts/Events`, `RoutineTypes/Events`), todos con `OccurredAt` y, los de rutina, con `RoutineStatusSnapshot` (datos de `RoutineSnapshot` del registro, construidos con el catálogo para que los *mappers* sean puros):

| Evento de dominio | Integración (mapper) | Stream |
|---|---|---|
| `RoutineRegistered` | `MaintenanceRoutineRegisteredV1` | `maintenance.routine` |
| `RoutineBecameDueSoon(snapshot, trigger, isFirstNoticeInCycle)` | `MaintenanceRoutineDueSoonV1` | `maintenance.routine` |
| `RoutineBecameOverdue(snapshot, trigger)` | `MaintenanceRoutineOverdueV1` | `maintenance.routine` |
| `RoutineDueStatusReverted(snapshot)` | `MaintenanceRoutineDueStatusRevertedV1` (`Reason = ParametersChanged`) | `maintenance.routine` |
| `RoutineRestarted(snapshot, serviceOrderId, service)` | `MaintenanceRoutineRestartedV1` | `maintenance.routine` |
| `RoutineStateChanged(snapshot)` | `MaintenanceRoutineStateChangedV1` | `maintenance.routine` |
| `RoutineParametersChanged(snapshot)` | `MaintenanceRoutineParametersChangedV1` | `maintenance.routine` |
| `MaintenancePlanStatusChanged` | `MaintenancePlanStatusChangedV1` | `maintenance.routine` |
| `RoutineTypeParametersChanged(routineTypeId)` | — (solo interno) | — |
| `AlertRaised` / `AlertStatusChanged` | `AlertRaisedV1` / `AlertStatusChangedV1` | `maintenance.alert` |

`OdometerReadingRecordedV1` no nace de un evento de dominio (ADR-0014: una lectura ignorada no emite eventos de dominio). Lo encola el handler de Application con `IIntegrationEventPublisher` en la misma unidad de trabajo, tras escribir el historial (ADR-0025). Semántica de sus campos (acordada con `reporting` CR-04): `Kilometers` = km recibidos; `CurrentOdometerKm` = **odómetro del plan después de procesar la lectura** (igual a `Kilometers` si fue `Accepted`; el valor previo sin cambios si fue `Ignored`).

Handlers internos de eventos de dominio (mismo servicio, antes de confirmar, ADR-0011):

| Evento | Handler | Efecto |
|---|---|---|
| `RoutineBecameDueSoon` con `isFirstNoticeInCycle` | `RaiseDueSoonAlertHandler` | `Alert.Raise(DueSoon)` |
| `RoutineBecameOverdue` | `RaiseOverdueAlertHandler` | Resuelve la `DueSoon` abierta del ciclo con `EscalatedToOverdue` y luego `Alert.Raise(Overdue)` |
| `RoutineDueStatusReverted` | `ResolveAlertsOnRevertHandler` | `ParametersChanged` |
| `RoutineRestarted` | `ResolveAlertsOnRestartHandler` | `RoutineRestarted` sobre las abiertas de la rutina |
| `RoutineStateChanged` a `Inactive` | `ResolveAlertsOnDeactivationHandler` | `RoutineDeactivated` |
| `RoutineTypeParametersChanged` | `ReevaluatePlansOnRoutineTypeChangedHandler` | Reevalúa los planes activos con rutinas de ese tipo (misma transacción, ADR-0019) |

**Orden de emisión** en una lectura que escala una rutina (offsets consecutivos, una transacción): `OdometerReadingRecorded` → `MaintenanceRoutineOverdue` → `AlertStatusChanged` (la `DueSoon`, `EscalatedToOverdue`) → `AlertRaised` (la `Overdue`). En el alta de una rutina ya vencida: `MaintenanceRoutineRegistered` → `MaintenanceRoutineOverdue` → `AlertRaised`. Reacciones aguas abajo: ServiceOrders crea la orden solo con `MaintenanceRoutineOverdue` (RN-3) y mantiene su réplica con todos los eventos de rutina; Notifications retransmite rutina y alertas; Reporting proyecta todo.

Eventos consumidos (suscriptor `"maintenance"`, `StartPosition.Checkpoint`):

| Evento | Handler | Command |
|---|---|---|
| `VehicleRegisteredV1` (`fleet.vehicle`) | `VehicleRegisteredHandler` | `CreateMaintenancePlanCommand` (idempotente: si existe, éxito sin cambios) |
| `VehicleStatusChangedV1` (`fleet.vehicle`) | `VehicleStatusChangedHandler` | `ChangePlanStatusCommand` (`Suspend` / `Resume`) |
| `ServiceOrderCompletedV1` (`serviceorders.service-order`) | `ServiceOrderCompletedHandler` | `RestartRoutineCommand(VehicleId, RoutineId, ServicedKm, ServicedAt, ServiceOrderId)`; plan o rutina inexistentes → log `ServiceOrderCompletedForUnknownRoutine` y éxito (no bloquea el stream) |

### D-06 Puertos de Application

```csharp
public interface IMaintenancePlanRepository
{
    Task<MaintenancePlan?> GetAsync(VehicleId id, CancellationToken ct);
    Task<VehicleId?> FindVehicleIdByPlateAsync(LicensePlate plate, CancellationToken ct);
    Task<IReadOnlyList<VehicleId>> GetActivePlanIdsPageAsync(VehicleId? after, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<VehicleId>> GetActivePlanIdsWithRoutineTypeAsync(RoutineTypeId routineTypeId, CancellationToken ct);
    Task<IReadOnlyDictionary<LicensePlate, MaintenancePlan>> GetByPlatesAsync(IReadOnlyCollection<LicensePlate> plates, CancellationToken ct);
    void Add(MaintenancePlan plan);
}
public interface IRoutineTypeRepository { Task<RoutineType?> GetAsync(RoutineTypeId id, CancellationToken ct); Task<RoutineParameterCatalog> GetCatalogAsync(CancellationToken ct); }
public interface IAlertRepository
{
    Task<Alert?> GetAsync(AlertId id, CancellationToken ct);
    Task<IReadOnlyList<Alert>> GetOpenByRoutineAsync(RoutineId routineId, AlertType? type, CancellationToken ct);
    void Add(Alert alert);
}
public interface IOdometerReadingLog { void Append(OdometerReadingLogEntry entry); }                     // ADR-0025 IMP-001
public interface IOdometerReadingQueries { Task<IReadOnlyList<OdometerReadingDto>> GetRecentAsync(VehicleId id, int limit, CancellationToken ct); }
public interface IMaintenancePlanQueries
{
    Task<MaintenancePlanDetailDto?> GetDetailAsync(VehicleId id, CancellationToken ct);
    Task<IReadOnlyList<MaintenancePlanSummaryDto>> GetSummariesAsync(CustomerId? customerId, CancellationToken ct);
}
public interface IAlertQueries { Task<PagedResult<AlertDto>> SearchAsync(AlertSearchCriteria criteria, CancellationToken ct); }
public interface IRoutineTypeQueries { Task<IReadOnlyList<RoutineTypeDto>> GetAllAsync(CancellationToken ct); }
public interface IUnitOfWorkScopeFactory { IUnitOfWorkScope Create(); }  // un plan por unidad de trabajo en la revisión por lotes
```

`IUnitOfWork`, `IClock` e `IIntegrationEventPublisher` vienen de BuildingBlocks.

### D-07 Commands, queries y responsabilidades

| Command / Query | Handler: responsabilidad única |
|---|---|
| `IngestTelemetryReadingCommand(Plate, OdometerKm, ReadAt)` | ACL: `TelemetryReadingTranslator` normaliza y resuelve placa → `VehicleId`; desconocida → `Ignored/UnknownVehicle` + log; si no, delega en `RecordOdometerReadingCommand` |
| `RecordOdometerReadingCommand(VehicleId, Kilometers, ReadAt)` | Carga plan y catálogo, `plan.RecordOdometerReading`, escribe historial, encola `OdometerReadingRecordedV1`, guarda; `SingleRetryOnConcurrencyConflict` (política de un reintento, solo para este command) |
| `ReevaluateMaintenancePlansCommand(AsOfDate, VehicleId?)` | Pagina IDs de 100 en 100, un `IUnitOfWorkScope` por plan, captura y registra `TimeEvaluationFailed`, acumula `ReevaluationSummary(EvaluatedPlans, DueSoon, Overdue, OrdersRequested)`. Lo usan scheduler, `time-evaluations` y `reevaluations` (fleet-maintenance-ui CR-08: mismo resumen) |
| `UpdateRoutineTypeParametersCommand` | Actualiza el tipo; la reevaluación la hace el handler de dominio |
| `RegisterRoutinesBulkCommand(Rows, DryRun)` | `BulkRoutineRowValidator` (formato) → `BulkDuplicateDetector` (`Bulk.DuplicateInBatch` por `plate + routineTypeCode`) → `plan.RegisterRoutine` por fila, que devuelve los errores de catálogo `MaintenancePlan.NotFound`, `RoutineType.NotFound`, `Routine.LastServiceKmAboveOdometer`, `Routine.IncompleteLastService` o `Routine.AlreadyExists` (→ `Skipped`) → `BulkRowResult`; con `DryRun` no llama a `SaveChangesAsync`. Los códigos son constantes de `MaintenanceErrors` (CN-03) |
| `UpdateRoutineParametersCommand`, `ChangeRoutineStateCommand` | Delegan en el plan |
| `CreateMaintenancePlanCommand`, `ChangePlanStatusCommand`, `RestartRoutineCommand` | Delegan en el plan |
| `AcknowledgeAlertCommand(AlertId, AcknowledgedBy)` | `alert.Acknowledge` |
| `GetMaintenancePlanQuery`, `GetMaintenancePlanSummariesQuery(CustomerId?)`, `GetOdometerReadingsQuery(VehicleId, Limit)`, `SearchAlertsQuery(Status?, VehicleId?, CustomerId?, Page, PageSize)`, `GetRoutineTypesQuery` | Delegan en los puertos de consulta |

DTOs de consulta relevantes para el CRM:

- `MaintenancePlanSummaryDto(VehicleId, Plate, PlanStatus, OdometerKm, WorstDueStatus?, MaxConsumedPercent, UpToDateCount, DueSoonCount, OverdueCount, InactiveCount)` (fleet-maintenance-ui CR-01). Peor estado y conteos solo sobre rutinas activas; `InactiveCount` aparte.
- `MaintenancePlanDetailDto(..., Routines: RoutineDetailDto[])`, con `RoutineDetailDto(RoutineId, RoutineTypeCode, Name, State, DueStatus, TriggeredBy?, CycleNumber, CycleStartKm, CycleStartDate, IntervalKm?, IntervalKmSource, IntervalMonths?, IntervalMonthsSource, DueSoonPercent, DueSoonPercentSource, NextDueKm?, NextDueDate?, ConsumedPercent, DistanceTrack?, TimeTrack?)` y `RoutineTrackDto(Kind: "km"|"time", ConsumedPercent, DueSoonPercent, Remaining, Status)` (fleet-maintenance-ui CR-02, espejo de `RoutineTrackVm` del handoff). `Source` ∈ `RoutineType`, `Override`.
- `AlertDto(AlertId, VehicleId, Plate, CustomerId, RoutineId, RoutineName, CycleNumber, Type, TriggeredBy, Status, RaisedAt, AcknowledgedAt?, AcknowledgedBy?, ResolvedAt?, ResolutionReason?)`; filtros `status`, `vehicleId`, `customerId` (fleet-maintenance-ui CR-03).

### D-08 Persistencia (schema `maintenance`)

- `RoutineTypes`, `MaintenancePlans` (PK `VehicleId`, `UX_MaintenancePlans_Plate`), `MaintenanceRoutines` (FK a plan con `ON DELETE CASCADE`, FK a tipo, **`UX_MaintenanceRoutines_VehicleId_RoutineTypeId`**), `Alerts` (**con `Plate`, `CustomerId` y `RoutineName` copiados al crear**, `UX_Alerts_OnePerCycleAndType`, `IX_Alerts_Status_RaisedAt`, `IX_Alerts_CustomerId_Status`), `OdometerReadings` (ADR-0025), `OutboxMessages`, `EventCheckpoints`.
- Enums por nombre con `CHECK` (E-02, E-03). `RoutineOverrides` y `MaintenanceCycle` como *owned types* en columnas de `MaintenanceRoutines` (nombres del modelo de datos: `IntervalKm`, `IntervalMonths`, `DueSoonThresholdPercentOverride`, `CycleNumber`, `CycleStartKm`, `CycleStartDate`).
- Semilla de `RoutineTypes` con `HasData` e IDs fijos (constantes en `RoutineTypeSeed`).
- `OdometerReadingLogEntry` es una clase de persistencia en Infrastructure, sin comportamiento (ADR-0025 IMP-002).

### D-09 Scheduler

`DailyTimeEvaluationScheduler : BackgroundService` con `TimeEvaluationScheduleOptions` (`RunAt = 00:05`, `TimeZoneId = "America/Bogota"` como constantes por defecto). Al arrancar despacha con hoy; luego espera hasta la próxima ocurrencia con `IClock` y `PeriodicTimer`. Crea un *scope* de DI por ejecución y solo invoca el command (sin lógica propia).

## Patrones aplicados

| Patrón | Dónde | Por qué |
|---|---|---|
| Aggregate, Value Object, Domain Event | D-02 | Invariantes RN-1/2/5/6 en un solo límite de consistencia |
| Strategy | `IDueDimensionEvaluator` | Km y tiempo intercambiables; una nueva dimensión (horas de motor) no toca las existentes (OCP) |
| Policy (Specification) | `DueStatusPolicy`, `RoutineTransitionPolicy` | Reglas de combinación y transición declarativas y probables por separado |
| Anti-Corruption Layer | `TelemetryReadingTranslator` + request HTTP | El formato externo afecta 2 archivos (ADR-0014 POS-003) |
| Template de reintento acotado | `SingleRetryOnConcurrencyConflict` | Concurrencia de lecturas sin acoplar la política al handler |
| CQRS | Queries con proyecciones `AsNoTracking` | El read model no carga agregados; reutiliza las políticas puras |
| Domain Event Handler (in-process) | Alertas y reevaluación por tipo | Efectos internos atómicos sin acoplar `MaintenancePlan` a `Alert` |

## Antipatrones evitados

| Antipatrón | Cómo se evita |
|---|---|
| Modelo anémico | `MaintenancePlan` y `Alert` deciden sus transiciones; los handlers solo orquestan |
| Objeto dios | La evaluación vive en estrategias y políticas; el plan solo las coordina. Alertas en su propio agregado |
| Números mágicos | 100 (lote), 50/200 (límites), 500 (bulk), 00:05 y `America/Bogota` como constantes con nombre u opciones |
| Copy-paste | Una función de evaluación para todos los disparadores y para la consulta |
| Lava flow | Sin «modos» de evaluación ni banderas muertas: la monotonía hace innecesario distinguirlos |
| Comparar enums por orden | `Severity` explícito (E-06) |

## Risks / Trade-offs

- [Cambiar un tipo de rutina reevalúa N planes en una sola transacción (ADR-0019)] → Aceptable con el volumen de la demo (≤ 5.000 vehículos por la cota de DataImport). Evolución: reevaluación asíncrona por lotes.
- [Dos agregados (`MaintenancePlan` y `Alert`) en una transacción] → Excepción consciente limitada al servicio (ADR-0011 NEG-001).
- [Los resultados esperados de la carga dependen de la fecha] → Las pruebas fijan `today = 2026-11-12` con `FakeClock`; la demo se ensaya con esa fecha.
- [Reinicio duplicado si se reprocesa `ServiceOrderCompleted` con checkpoint reiniciado] → El checkpoint de foundation garantiza un efecto; reiniciar el checkpoint de Maintenance no es una operación soportada.
- [Un vehículo registrado justo antes de su primera lectura] → La lectura se ignora como `UnknownVehicle` y la siguiente corrige (ADR-0014 NEG-002).

## Contratos nuevos requeridos

Contratos que no están en el registro de `foundation-platform/design.md` y que el orquestador debe agregar allí antes de la ola que los use:

| # | Contrato | Motivo |
|---|---|---|
| CN-01 | Endpoint `GET /api/v1/maintenance/plans?customerId=` (resumen por vehículo) | Lista de flota del CRM sin N llamadas (fleet-maintenance-ui CR-01) |
| CN-02 | Firma de `IIntegrationEventPublisher`: `void Enqueue(IIntegrationEvent integrationEvent, string subject)` que escribe en el Outbox de la unidad de trabajo actual | Publicar `OdometerReadingRecordedV1` sin evento de dominio. Está nombrado en el proposal de foundation pero falta en su D-03 |
| CN-03 | Códigos de error estables del bulk de rutinas (acordados con `data-import`): `MaintenancePlan.NotFound` (DataImport reintenta la simulación hasta 10 s mientras se propaga `VehicleRegistered`), `RoutineType.NotFound`, `Routine.LastServiceKmAboveOdometer`, `Routine.IncompleteLastService`, `Bulk.DuplicateInBatch` | Contrato observable con DataImport (ADR-0022) |
| CN-04 | Columnas `Plate`, `CustomerId`, `RoutineName` en `maintenance.Alerts` e índice `UX_MaintenanceRoutines_VehicleId_RoutineTypeId` | Bandeja por cliente sin `JOIN` y regla «rutina ya existe». Ajuste a `data-model.md` (tarea 8.2 de foundation) |
| CN-05 | `RoutineBulkRow { rowNumber, plate, routineTypeCode, lastServiceKm?, lastServiceDate?, intervalKm?, intervalMonths?, dueSoonThresholdPercent? }`; vacíos = valores del tipo; sin km ni fecha = vehículo nuevo (0 km, fecha de carga, A-01); `Skipped` por clave natural `plate + routineTypeCode` | Contrato con DataImport |

## Open Questions

- Ninguna que cambie el alcance.
