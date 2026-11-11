## Context

Motivación en `proposal.md`. Reporting depende de `foundation-platform`: usa `IIntegrationEventHandler<T>`, `EventMetadata` (con `Offset`), el *builder* `AddEventBusSubscriber`, `EventCheckpoints`, `ApplyRutaVivaConventions`, `IClock` y `ResultExtensions.ToHttpResult`. Los nombres de streams y eventos son los del registro de contratos (`foundation-platform/design.md#streams` y `#eventos-de-integración`); los endpoints, los de `#endpoints-públicos`.

## Goals / Non-Goals

**Goals:**
- Cada reporte es una consulta sobre tablas locales del schema `reporting`, sin `JOIN` entre schemas ni HTTP a otros servicios.
- Agregar un reporte = una definición nueva y su fuente de filas; agregar un formato = un escritor nuevo. Ninguno toca al otro (OCP).
- Proyecciones idempotentes, tolerantes al orden y reconstruibles.

**Non-Goals:**
- Modelo de dominio: Reporting no tiene invariantes de negocio (ver D-01).
- Paginación por cursor: los volúmenes de la demo no la justifican; se usa `OFFSET/FETCH` con índice.

## Decisions

### D-01 Sin proyecto Domain (read model)

Reporting tiene `Application`, `Infrastructure` y `Api` (análisis §9). Sus filas son copias de hechos ya validados por los dueños; no hay reglas que proteger. Un dominio rico aquí sería **ceremonia sin invariantes** (*speculative generality*). Las filas de proyección sí encapsulan la única regla que tienen: «solo se aplica un evento posterior» (D-03), para no repetirla en cada handler. Alternativa descartada: reutilizar tipos de dominio de los servicios dueños; prohibido por ADR-0010 (ningún tipo de dominio se comparte).

Las pruebas de arquitectura de `foundation-platform` se parametrizan por servicio; Reporting se registra sin capa `Domain`.

### D-02 Proyecciones: evento → tabla

| Tabla (`reporting.*`) | Clave | Eventos | Columnas que llena |
|---|---|---|---|
| `Customers` | `CustomerId` | `CustomerRegistered`, `CustomerStatusChanged` | `Name`, `TaxId`, `Status` |
| `Workshops` | `WorkshopId` | `WorkshopRegistered`, `WorkshopStatusChanged` | `Name`, `City`, `Status` |
| `Vehicles` | `VehicleId` | `VehicleRegistered` (`OdometerKm = InitialOdometerKm`), `VehicleStatusChanged`, `OdometerReadingRecorded` (`OdometerKm = CurrentOdometerKm`) | `Plate`, `CustomerId`, `WorkshopId`, `Status`, `OdometerKm` |
| `VehicleRoutineStatus` | `RoutineId` | Todo evento de `maintenance.routine` que trae `RoutineSnapshot` (`MaintenanceRoutineRegistered`, `…DueSoon`, `…Overdue`, `…DueStatusReverted`, `…Restarted`, `…StateChanged`, `…ParametersChanged`) | Snapshot completo: `VehicleId`, `Plate`, `CustomerId`, `RoutineTypeCode`, `RoutineName`, `State`, `DueStatus`, `CycleNumber`, `CycleStartKm`, `CycleStartDate`, `IntervalKm`, `IntervalMonths`, `EffectiveDueSoonPercent`, `NextDueKm`, `NextDueDate`, `OdometerKm`, `ConsumedPercent` |
| `ServiceOrderFacts` | `ServiceOrderId` | `ServiceOrderCreated`, `ServiceOrderStatusChanged`, `ServiceOrderApprovalRequested`, `ServiceOrderLinesChanged`, `ServiceOrderAppointmentScheduled`, `ServiceOrderCompleted` | `ServiceOrderSnapshot` (`Number`, `Origin`, `VehicleId`, `Plate`, `RoutineName`, `CustomerId`, `WorkshopId`, `Status`, `RoutineAmount`, `AdditionalAmount`, `TotalAmount`, `Currency`, `AutoApproved`, `AppointmentAt`, `CancellationReason`, `CreatedAt`) + fechas de fase + `ServicedKm`, `ServicedAt` |
| `AlertFacts` | `AlertId` | `AlertRaised`, `AlertStatusChanged` | `VehicleId`, `Plate`, `CustomerId`, `RoutineId`, `RoutineName` (solo `AlertRaised`), `Type`, `TriggeredBy`, `Status`, `RaisedAt`, `AcknowledgedAt`, `ResolvedAt`, `ResolutionReason` |

Fechas de fase de `ServiceOrderFacts`, desde `ServiceOrderStatusChanged.ToStatus` y `OccurredAt`, **solo si aún están vacías** (primera vez): `Quoted → QuotedAt`, `Approved → ApprovedAt`, `OnSite → OnSiteAt`, `InProgress → StartedAt`, `Completed → CompletedAt`, `Cancelled → CancelledAt`. El mapa `ToStatus → columna` es una tabla estática (`ServiceOrderPhaseMap`), no un `switch` repartido.

Verificación de suficiencia del contrato: todas las columnas de R-01 a R-04 se llenan con los campos del registro; ver «Contratos nuevos requeridos» para las semánticas que deben fijar los productores.

`VehicleRoutineStatus` agrega `Plate` y `CustomerId` respecto al script de referencia para filtrar R-01 por cliente sin `JOIN` adicional. Las tablas no tienen `CHECK` de enums ni FK (data-model §2, *tolerant reader*).

### D-03 Descarte de obsoletos por offset (ajuste de D-19)

Cada fila guarda `LastEventOffset bigint` y `LastEventAt`. Se aplica un evento solo si `metadata.Offset > LastEventOffset`. **Se usa el offset y no `OccurredAt`** porque varios eventos de una misma transacción comparten `OccurredAt` (por ejemplo `Quoted` y `Approved` en una aprobación automática, A-14): con la fecha, el segundo se descartaría. El offset es estrictamente creciente y global (ADR-0024 POS-004). `LastEventAt` se conserva para auditoría.

Implementación: clase base `ProjectionRow` con `LastEventOffset`, `LastEventAt` y `bool TryAdvance(EventMetadata metadata)`. Cada fila concreta expone métodos con intención (`ApplySnapshot(...)`, `RecordPhase(...)`) que primero llaman a `TryAdvance`. Los handlers no repiten la comparación (DRY).

### D-04 Handlers de proyección

Un handler por proyección, que implementa `IIntegrationEventHandler<T>` para cada evento de su familia (cohesión por tabla): `CustomerDimensionProjection`, `WorkshopDimensionProjection`, `VehicleDimensionProjection`, `VehicleRoutineStatusProjection`, `ServiceOrderFactsProjection`, `AlertFactsProjection`. Cada uno usa un puerto de Application `IProjectionStore<TRow>` (`FindAsync(Guid key)`, `Add(TRow row)`) implementado genéricamente con EF Core (`EfProjectionStore<TRow>`). El `IntegrationEventDispatcher` de `foundation-platform` confirma la fila y el checkpoint en la misma transacción; los handlers no llaman a `SaveChanges`.

Si llega un evento de actualización para una fila inexistente (solo posible si el productor publicó fuera de su orden), el handler devuelve `Result.Success` y registra `ProjectionRowMissing` con la clave; no crea filas parciales.

Registro: `AddEventBusSubscriber("reporting", StartPosition.Checkpoint)` con un `Handle<TEvent, THandler>(stream)` por evento, agrupado en `ReportingSubscriptions.Register(builder)`.

### D-05 Reconstrucción

`RebuildProjectionsCommand` (Application) con el puerto `IProjectionMaintenance.ResetAsync()` (Infrastructure: `DELETE` de las 6 tablas y `LastOffset = 0` del checkpoint `reporting`, en una transacción). Lo ejecuta `ProjectionRebuildStartupTask : IHostedService`, registrado **antes** del suscriptor, solo si `Reporting:Projections:RebuildOnStartup = true`. No se expone endpoint público (no hay autenticación, y un reinicio accidental vaciaría los reportes).

### D-06 Bridge: definiciones × escritores

```csharp
public interface IReportDefinition
{
    ReportCode Code { get; }
    ReportDescriptor Descriptor { get; }                 // R-0n, título, filtros, columnas, slug del archivo
    Result<ReportFilter> ParseFilter(IReadOnlyDictionary<string, string?> values);
    Task<ReportPage> GetPageAsync(ReportFilter filter, PageRequest page, CancellationToken cancellationToken);
    Task<Result<ReportDocument>> GetDocumentAsync(ReportFilter filter, int maxRows, CancellationToken cancellationToken);
}
public interface IReportWriter
{
    ReportFormat Format { get; }
    string ContentType { get; }
    string FileExtension { get; }
    Task WriteAsync(ReportDocument document, Stream output, CancellationToken cancellationToken);
}
public sealed record ReportColumn(string Key, string Header, ColumnFormat Format);
public enum ColumnFormat { Text = 1, Integer = 2, Decimal = 3, Money = 4, Date = 5, DateTime = 6, Percent = 7, Hours = 8 }
public sealed record ReportDocument(ReportDescriptor Descriptor, IReadOnlyList<ReportRow> Rows, IReadOnlyList<ReportParameter> Parameters, DateTimeOffset GeneratedAt);
```

- **Abstracción** (`IReportDefinition`): qué datos y con qué columnas. Base `ReportDefinition<TFilter, TRow>` (patrón **Template Method**: parsear filtros → contar → validar límite → leer filas → proyectar a `ReportRow` con los accesores de cada columna).
- **Implementación** (`IReportWriter`): cómo se escribe. `XlsxReportWriter` (ClosedXML) y `CsvReportWriter`. Ambos solo conocen `ReportColumn.Format`, nunca un reporte concreto.
- 4 definiciones + 2 escritores = **6 clases**, no 8 (ADR-0023 §5).
- Fuentes de filas: puerto `IReportRowSource<TFilter, TRow>` (`CountAsync`, `ReadAsync(filter, skip, take)`), una implementación EF Core por reporte en Infrastructure con `AsNoTracking` y proyección directa al `record` de fila.
- Resolución: `ReportCatalog` (diccionario `ReportCode → IReportDefinition`) y `ReportWriterResolver` (diccionario `ReportFormat → IReportWriter`), construidos desde DI (patrón **Registry**/**Strategy**). Un reporte recortado simplemente no se registra.

Alternativa descartada: una clase por combinación reporte × formato (8 clases, explosión combinatoria y duplicación del formateo).

### D-07 Definiciones de reporte

| Código | Archivo (slug) | Filtros | Fuente | Orden |
|---|---|---|---|---|
| `FleetMaintenanceStatus` (R-01) | `R-01_estado_flota` | `customerId?`, `vehicleStatus?`, `dueStatus?` | `VehicleRoutineStatus` ⋈ `Vehicles` ⋈ `Customers` | severidad (`Overdue`, `DueSoon`, `UpToDate`, otros) y `ConsumedPercent DESC` |
| `ServiceOrders` (R-02) | `R-02_ordenes` | `from?`, `to?` (sobre `CreatedAt`), `status?`, `workshopId?`, `customerId?`, `origin?` | `ServiceOrderFacts` ⋈ dimensiones | `CreatedAt DESC` |
| `MaintenanceCosts` (R-03, recortable) | `R-03_costos` | `from?`, `to?` (sobre `CompletedAt`), `customerId?`, `workshopId?`, `vehicleId?` | `ServiceOrderFacts` con `Status = Completed`, agrupado por cliente, taller y placa | total `DESC` |
| `AlertsAttention` (R-04, recortable) | `R-04_alertas` | `from?`, `to?` (sobre `RaisedAt`), `customerId?`, `type?`, `triggeredBy?` | `AlertFacts` agrupado por `Type` y `TriggeredBy` | `Type`, `TriggeredBy` |

- Los `JOIN` son entre tablas del mismo schema `reporting` (permitido: P-01 prohíbe `JOIN` entre schemas).
- km y días restantes de R-01: `NextDueKm - OdometerKm` y `NextDueDate - hoy`, con «hoy» de `IClock.TodayIn(America/Bogota)`, calculados en la definición (no se almacenan, cambian cada día).
- Tiempos de ciclo de R-02 y R-04 en horas con un decimal, calculados en la consulta; vacíos si falta una de las dos fechas.
- Número de orden: `OS-` + `Number` con 6 dígitos (constante de formato en la definición).
- Etiquetas en español: `ReportLabels` (un mapa por enum de contrato, en un solo archivo de constantes); un valor desconocido se devuelve tal cual.

### D-08 Formatos

- **xlsx**: hoja de datos con el título del reporte, encabezados en negrita, `SetAutoFilter`, `FreezeRows(1)`, anchos ajustados, formatos numéricos por `ColumnFormat` (`"$" #,##0` para `Money` en COP, `yyyy-mm-dd`, `yyyy-mm-dd hh:mm`, `0.00%`). Hoja «Parámetros»: reporte, cada filtro con su etiqueta y valor («Todos» si vacío) y fecha de generación en `America/Bogota`.
- **csv**: `UTF8Encoding(encoderShouldEmitUTF8Identifier: true)`, `;`, cultura `es-CO` para números (coma decimal, sin separador de miles), fechas ISO, comillas dobles según RFC 4180. Sin librería (no hace falta CsvHelper para escribir; KISS).
- Nombre: `{slug}_{yyyy-MM-dd}.{ext}` con la fecha de generación en `America/Bogota`.
- Descarga síncrona escrita directamente en el `Stream` de la respuesta; el límite de 50.000 filas (`ReportingOptions.MaxDownloadRows`) se valida con `CountAsync` **antes** de escribir, para poder responder `422` con `ProblemDetails`.

### D-09 Application (CQRS, solo queries)

| Query | Resultado |
|---|---|
| `GetReportCatalogQuery` | `IReadOnlyList<ReportDescriptorDto>` |
| `PreviewReportQuery(Code, Filters, Page, PageSize)` | `ReportPageDto` (`columns`, `items`, `page`, `pageSize`, `totalCount`) |
| `DownloadReportQuery(Code, Format, Filters)` | `ReportFileDto` (`FileName`, `ContentType`, `Func<Stream, CancellationToken, Task> WriteTo`) |

Errores de catálogo `ReportErrors`: `Report.NotFound` (404), `Report.InvalidFilter` (400), `Report.InvalidPageSize` (400), `Report.UnsupportedFormat` (400), `Report.TooManyRows` (422). Todos los límites (50, 200, 50.000) son constantes con nombre en `ReportingOptions`.

### D-10 Api

`ReportEndpoints : IEndpointModule` con los 3 endpoints del registro. El endpoint de descarga usa `Results.Stream` con `Content-Disposition: attachment`. Los filtros llegan por *query string* y se pasan como diccionario a la definición (cada definición sabe sus filtros; la API no los conoce).

### Contratos nuevos requeridos

No se requieren eventos ni campos nuevos: el registro de `foundation-platform` cubre todas las columnas. Se piden estas **semánticas** a los productores, para documentarlas en sus `design.md`:

| # | Productor (change) | Semántica que Reporting asume |
|---|---|---|
| CR-01 | `service-orders-core` | `ServiceOrderSnapshot.RoutineAmount` = suma de líneas `Routine` con precio; `AdditionalAmount` = suma de líneas `Additional` con precio y `ApprovalStatus = Approved`; `TotalAmount` = ambas. Las rechazadas no suman |
| CR-02 | `service-orders-core` | Cada cambio de estado emite un `ServiceOrderStatusChanged` con su `OccurredAt` real, incluido `Created → Quoted` antes de `Quoted → Approved/PendingApproval` (A-14) |
| CR-03 | `maintenance-core` | `RoutineSnapshot.ConsumedPercent` = el mayor de los consumos por km y por tiempo, puede superar 100 y es nulo solo si la rutina no tiene ningún intervalo |
| CR-04 | `maintenance-core` | `OdometerReadingRecorded.CurrentOdometerKm` = odómetro del plan después de procesar la lectura (igual al anterior si fue ignorada) |

Nota informativa: no existen `CustomerUpdated` ni `WorkshopUpdated`; si en el futuro se editan nombres, las dimensiones no se actualizarían. Hoy ningún endpoint edita nombres, así que no aplica (YAGNI).

## Patrones aplicados

| Patrón | Dónde | Por qué |
|---|---|---|
| CQRS (read model de sistema) | Todo el servicio | Consultas desnormalizadas sin tocar los agregados (ADR-0023) |
| Bridge | D-06 | Reportes y formatos varían de forma independiente: 6 clases en lugar de 8 |
| Template Method | `ReportDefinition<TFilter, TRow>` | Flujo común parsear → contar → limitar → leer → proyectar en un solo lugar |
| Registry / Strategy | `ReportCatalog`, `ReportWriterResolver`, suscripciones | Selección por código y formato sin `switch` |
| Idempotent Consumer + tolerant reader | D-02, D-03 | Entrega *at-least-once* y evolución de enums sin romper |
| Repository genérico de proyección | `IProjectionStore<TRow>` | Un solo adaptador EF para 6 tablas |

## Antipatrones evitados

| Antipatrón | Cómo se evita |
|---|---|
| Modelo anémico | No aplica a un read model; la única regla (orden por offset) vive en `ProjectionRow`, no en los handlers |
| Objeto dios | Un handler por proyección, una definición por reporte, un escritor por formato; ninguna clase conoce todos los reportes |
| Copy-paste | Comparación de offset, flujo de paginación y límite de filas en clases base; formateo por `ColumnFormat` en cada escritor una sola vez |
| Números mágicos | 50, 200, 50.000, formato `OS-000000` y nombres de hoja como constantes en `ReportingOptions` y en las definiciones |
| Martillo dorado | El CSV se escribe sin librería; ClosedXML solo para `.xlsx` |
| Optimización prematura | Sin cachés ni paginación por cursor; índices del script de referencia bastan |

## Risks / Trade-offs

- [Consistencia eventual: un reporte puede no incluir un evento de hace < 1 s] → Aceptado y documentado en la hoja «Parámetros» con la fecha de generación.
- [La reconstrucción deja los reportes vacíos mientras se reprocesa el log] → Solo bajo configuración explícita al arrancar; con el volumen de la demo tarda segundos.
- [La descarga síncrona bloquea una petición con resultados grandes] → Límite de 50.000 filas validado antes de escribir; evolución a job asíncrono (YAGNI).
- [Semántica de montos o consumo distinta en los productores] → CR-01 a CR-04 explícitos; pruebas de integración con eventos construidos desde los contratos.
- [ClosedXML carga el libro en memoria] → Con 50.000 filas × ~20 columnas cabe en memoria; se documenta el límite.

## Migration Plan

Migración EF Core inicial `ReportingInitial` en el schema `reporting`. Para poblar datos ya existentes basta arrancar por primera vez (checkpoint 0) o con `RebuildOnStartup = true`.

## Open Questions

- Si el CRM quiere ver el nombre del cliente en la fila de R-04 (hoy se agrupa por tipo y origen). No cambia las proyecciones.
