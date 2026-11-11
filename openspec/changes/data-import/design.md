## Context

Motivación en `proposal.md`; requisitos en `specs/data-import-validation` y `specs/data-import-workflow`. Decisión de base: ADR-0022. Este change consume de `foundation-platform` los BuildingBlocks (D-02 a D-08), las convenciones de API y el **contrato del lote masivo** (`design.md#contrato-del-lote-masivo-bulkdryrun`): request `{ rows: [{ rowNumber, ... }] }` con máximo 500 filas y response `[{ rowNumber, outcome: Valid | Invalid | Skipped, errors: [{ code, message }] }]` (`BulkRowResult`/`BulkRowOutcome` en `RutaViva.IntegrationContracts`).

DataImport es la **única excepción documentada** a «sin HTTP entre servicios» (ADR-0010, ADR-0022 NEG-001): no es un bounded context de negocio sino un cliente de las APIs públicas de los dueños (Open Host Service), igual que la UI. No escribe en schemas ajenos ni publica eventos de integración.

## Goals / Non-Goals

**Goals:**
- Que agregar un tipo de archivo nuevo sea agregar una `IImportDefinition` y, si el dueño es nuevo, un adaptador `IOwnerServiceClient`, sin tocar el motor (OCP, ADR-0022 POS-005).
- Cero reglas de negocio de los dueños duplicadas: DataImport solo valida forma (niveles 1 y 2); la autoridad sobre el negocio es el dueño vía `dryRun` (nivel 3).
- Que la vista previa refleje fielmente lo que pasará al confirmar.

**Non-Goals:**
- Ejecución asíncrona con cola o *jobs* en segundo plano (la carga es síncrona dentro de los límites de 5 MB y 5.000 filas).
- Reintento automático de un trabajo `Failed`: el usuario vuelve a subir los archivos (la idempotencia lo hace seguro).

## Decisions

### D-01 Modelo de dominio (`RutaViva.DataImport.Domain`)

```
ImportJob (AggregateRoot<ImportJobId>)
 ├─ Status: ImportJobStatus { Validating = 1, PendingConfirmation, Importing, Completed, CompletedWithErrors, Failed }
 ├─ RequestedBy, CreatedAt, ConfirmedAt?, CompletedAt?
 └─ Stages: IReadOnlyList<ImportStage>            (1..5, una por ImportEntityType)
      ImportStage (Entity<ImportStageId>)
       ├─ EntityType: ImportEntityType { Workshops = 1, Customers, Approvers, Vehicles, Routines }
       ├─ StageOrder (derivado por ImportStageOrder.Of(entityType), nunca recibido)
       ├─ SourceFile: ImportFileDescriptor (VO: FileName, Delimiter, FileEncoding)
       ├─ Status: ImportStageStatus { Pending = 1, Validated, Imported, Failed }
       ├─ FailureReason?: Error
       ├─ Totals (calculados desde las filas, no almacenados a mano)
       └─ Rows: IReadOnlyList<ImportRow>
            ImportRow (Entity<long>): RowNumber, RawData, Outcome: ImportRowOutcome { Valid = 1, Invalid, Skipped, Imported, Failed }, Errors: IReadOnlyList<ImportRowError>
            ImportRowError (VO): Code, Message, Level: ImportErrorLevel { Format = 1, Business }
FileEncoding { Utf8 = 1, Utf8Bom, Windows1252 }
```

Comportamiento (modelo rico; ninguna transición se hace desde fuera con *setters*):

| Método | Invariante |
|---|---|
| `ImportJob.Start(requestedBy, files, now)` | 1 a 5 archivos, sin `ImportEntityType` repetido; etapas ordenadas por `ImportStageOrder` |
| `ImportStage.FailFile(Error)` | Solo en `Pending`; no admite filas |
| `ImportStage.RecordFormatResults(rows)` / `ApplySimulation(results)` | `RowNumber` único por etapa; un resultado por fila enviada; `Invalid` exige al menos un error |
| `ImportStage.ResolveInJobReferences(knownKeys, definition)` | Solo descarta errores de referencia cuya clave esté en `knownKeys` |
| `ImportJob.CompleteValidation()` | `Validating → PendingConfirmation`, o `→ Failed` si alguna etapa está `Failed` |
| `ImportJob.Confirm(now)` | Solo en `PendingConfirmation` (`ImportJob.InvalidStatus`) y con al menos una fila `Valid` (`ImportJob.NothingToImport`) |
| `ImportStage.ApplyImportResults(results)` | Solo filas `Valid` cambian a `Imported`, `Skipped` o `Failed` |
| `ImportJob.Complete(now)` / `Fail(stage, Error, now)` | `Completed` si ninguna fila `Failed`; si no, `CompletedWithErrors`; `Fail` detiene las etapas siguientes |

`ImportStageOrder` es una política de dominio (Workshops 1 … Routines 5) expresada como tabla constante, no como orden numérico del enum (data-model E-06).

### D-02 Persistencia (`imports` schema)

Tablas del modelo de datos (`ImportJobs`, `ImportStages`, `ImportRows`) con una columna adicional propia `ImportStages.FailureReason nvarchar(500) NULL` y el `Level` dentro del JSON de `ImportRows.Errors` (`[{ code, message, level }]`). `ImportJob` es la raíz con `rowversion`; las filas se cargan con el trabajo solo en los commands (`Include`), y las queries leen con proyecciones `AsNoTracking` paginadas (CQRS). `ImportRows.RawData` guarda la línea original tal cual para el CSV de errores (data-model D-17).

### D-03 Puertos de Application

```csharp
public interface IFileReader
{ Task<Result<CsvDocument>> ReadAsync(Stream content, long length, CancellationToken cancellationToken); }
public sealed record CsvDocument(char Delimiter, FileEncoding Encoding, IReadOnlyList<string> Headers, IReadOnlyList<CsvRecord> Records);
public sealed record CsvRecord(int RowNumber, string RawLine, IReadOnlyDictionary<string, string> Values);

public interface IImportDefinition
{
    ImportEntityType EntityType { get; }
    string TemplateFileName { get; }
    IReadOnlyList<string> Headers { get; }
    IReadOnlyList<string> NaturalKey(CsvRecord record);
    IReadOnlyList<ImportRowError> ValidateFormat(CsvRecord record);
    BulkRowPayload ToBulkRow(CsvRecord record);
    IReadOnlyDictionary<string, ReferenceDependency> ReferenceErrors { get; }   // código del dueño → (columna, etapa referenciada)
}

public interface IOwnerServiceClient
{
    IReadOnlySet<ImportEntityType> SupportedEntities { get; }
    Task<Result<IReadOnlyList<BulkRowResult>>> SubmitAsync(ImportEntityType entityType, IReadOnlyList<BulkRowPayload> rows, bool dryRun, CancellationToken cancellationToken);
}

public interface IImportJobRepository { Task<ImportJob?> GetAsync(ImportJobId id, CancellationToken ct); void Add(ImportJob job); }
public interface IImportJobQueries { Task<ImportJobDto?> GetAsync(ImportJobId id, ImportRowFilter filter, CancellationToken ct); Task<ImportErrorRows?> GetErrorRowsAsync(ImportJobId id, ImportEntityType entityType, CancellationToken ct); }
public interface ICsvWriter { byte[] Write(char delimiter, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows); }
```

- `ImportDefinitionRegistry` y `OwnerServiceClientRegistry` resuelven por `ImportEntityType` desde las colecciones inyectadas (patrón **Registry**; sin `switch`).
- La espera usa `TimeProvider` (`Task.Delay(TimeSpan, TimeProvider)`), sustituible por `FakeTimeProvider` en pruebas. `IClock` da «hoy» para la regla de fecha no futura.
- `CsvHelper` vive solo en `CsvFileReader : IFileReader` y `CsvHelperWriter : ICsvWriter` (Infrastructure, ADR-0022 IMP-001).

### D-04 Detección de formato del archivo (nivel 1)

`CsvFileReader`, en este orden y cortando en el primer error (guard clauses):
1. `length > 5 MB` → `ImportFile.TooLarge`.
2. Codificación: BOM UTF-8 → `Utf8Bom`; si no, decodificación UTF-8 estricta (`throwOnInvalidBytes`) → `Utf8`; si falla → `Windows1252` (`CodePagesEncodingProvider`).
3. Separador: el de mayor conteo entre `;` y `,` en la primera línea; ninguno → `ImportFile.UnknownDelimiter`.
4. Más de 5.000 registros → `ImportFile.TooManyRows`.

La comparación de encabezados contra la plantilla (`ImportFile.InvalidHeaders`) la hace Application con `IImportDefinition.Headers`, no el lector: el lector no conoce las plantillas (SRP).

### D-05 Pipelines (patrón Template Method)

```csharp
public abstract class ImportStagePipeline
{
    public async Task ExecuteAsync(ImportStageContext context, CancellationToken ct)
    {
        if (!CanRun(context)) return;
        await RunStepsAsync(context, ct);
        Complete(context);
    }
    protected abstract bool CanRun(ImportStageContext context);
    protected abstract Task RunStepsAsync(ImportStageContext context, CancellationToken ct);
    protected abstract void Complete(ImportStageContext context);
}
```

| Pipeline | Pasos |
|---|---|
| `PreviewStagePipeline` | leer → encabezados → formato y duplicados → simular (`dryRun=true`, lotes de 500) → resolver referencias del mismo trabajo → `Validated` |
| `ConfirmationStagePipeline` | esperar propagación (solo `StageOrder > 1`) → enviar filas `Valid` (`dryRun=false`, lotes de 500) → aplicar resultados → `Imported` |

`ImportStageContext` lleva la etapa, su `IImportDefinition`, su `IOwnerServiceClient` y las claves conocidas de las etapas anteriores (`InJobKeyIndex`). Los handlers `CreateImportJobCommandHandler` y `ConfirmImportJobCommandHandler` solo recorren las etapas en orden y delegan en el pipeline; ninguno supera unas pocas líneas.

### D-06 Referencias dentro del mismo trabajo

Cada definición declara qué códigos de error del dueño significan «referencia no encontrada» y a qué columna y etapa apuntan (`ReferenceErrors`). Durante la vista previa, `InJobKeyIndex` contiene las claves de las filas `Valid` de las etapas anteriores (`nit` de talleres y clientes, `correo` de aprobadores, `placa` de vehículos). Un error de referencia cuyo valor está en el índice se descarta. Durante la confirmación, los mismos códigos activan la **espera de propagación** (`PropagationWaitPolicy`: reintentos cada 500 ms hasta 10 s, constantes con nombre).

| Definición | Código del dueño | Columna | Etapa referenciada |
|---|---|---|---|
| `ApproverImportDefinition` | `Customer.NotFound` | `nit_cliente` | `Customers` |
| `VehicleImportDefinition` | `Vehicle.OwnerNotFound` | `nit_dueno` | `Customers` |
| `VehicleImportDefinition` | `Vehicle.WorkshopNotFound` | `nit_taller` | `Workshops` |
| `VehicleImportDefinition` | `Vehicle.ResponsibleNotFound` | `correo_responsable` | `Approvers` |
| `RoutineImportDefinition` | `MaintenancePlan.NotFound` | `placa` | `Vehicles` |

Limitación aceptada: si una rutina referencia un vehículo del mismo trabajo, la regla «km del último servicio ≤ odómetro» solo la puede aplicar Maintenance al confirmar; si falla, la fila queda `Failed` (riesgo R-02).

### D-07 Mapeo columna CSV → campo del lote masivo

Valores vacíos → `null`. El dueño aplica los valores por defecto de negocio (A-01: vehículo nuevo = 0 km y fecha de carga; intervalos y % del `RoutineType`).

| Entidad · endpoint | Columna CSV → campo bulk |
|---|---|
| `Workshops` · `POST /api/v1/workshops/bulk` | `nit`→`taxId`, `nombre`→`name`, `correo`→`email`, `telefono`→`phone`, `direccion`→`address`, `ciudad`→`city` |
| `Customers` · `POST /api/v1/customers/bulk` | `nit`→`taxId`, `nombre`→`name`, `correo`→`email`, `telefono`→`phone` |
| `Approvers` · `POST /api/v1/customers/approvers/bulk` | `nit_cliente`→`customerTaxId`, `nombre`→`fullName`, `correo`→`email` |
| `Vehicles` · `POST /api/v1/fleet/vehicles/bulk` | `placa`→`plate`, `vin`→`vin`, `marca`→`brand`, `modelo`→`model`, `anio`→`modelYear`, `nit_dueno`→`ownerTaxId`, `nit_taller`→`workshopTaxId`, `correo_responsable`→`responsibleApproverEmail`, `odometro_km`→`initialOdometerKm` |
| `Routines` · `POST /api/v1/maintenance/routines/bulk` | `placa`→`plate`, `codigo_rutina`→`routineTypeCode`, `km_ultimo_servicio`→`lastServiceKm`, `fecha_ultimo_servicio`→`lastServiceDate` (`yyyy-MM-dd`), `intervalo_km`→`intervalKm`, `intervalo_meses`→`intervalMonths`, `porcentaje_por_vencer`→`dueSoonThresholdPercent` |

Todas las filas llevan además `rowNumber`. Los textos se recortan; placa y código de rutina se pasan a mayúsculas; NIT sin espacios.

### D-08 Adaptadores de los dueños (patrón Adapter)

`WorkshopsServiceClient`, `CustomersServiceClient` (`Customers` y `Approvers`), `FleetServiceClient` y `MaintenanceServiceClient`: `HttpClient` tipado con *service discovery* (`https+http://workshops`), `AddStandardResilienceHandler` (3 reintentos de transporte, *timeout* de 30 s por lote) y traducción de fallos a `ImportOwner.Unavailable` o `ImportOwner.UnexpectedResponse`. El particionado en lotes de 500 (`BulkBatchSize`) lo hace el pipeline, no el adaptador. Las rutas viven como constantes en cada adaptador.

### D-09 API (`RutaViva.DataImport.Api`)

| Endpoint | Detalle |
|---|---|
| `POST /api/v1/imports` | `multipart/form-data`: un campo por archivo cuyo **nombre** es el `ImportEntityType` (`workshops`, `customers`, `approvers`, `vehicles`, `routines`) y un campo `requestedBy` opcional (por defecto `crm`, sin autenticación). Límite del request 26 MB. `201` + `Location` + resumen del trabajo |
| `POST /api/v1/imports/{id}/confirm` | `200` con el resumen final; `409`/`422` según D-01 |
| `GET /api/v1/imports/{id}?entity=&outcome=&page=&pageSize=` | Resumen + filas paginadas (máx. 200) |
| `GET /api/v1/imports/{id}/errors.csv?entity=` | `text/csv`, `filename="{plantilla}_errores.csv"` |
| `GET /api/v1/imports/templates/{entity}.csv` | `text/csv`, `filename="{plantilla}.csv"` (Recortable). `{entity}` es el valor de `ImportEntityType` (`Workshops`, `Customers`, `Approvers`, `Vehicles`, `Routines`), sin distinguir mayúsculas; otro valor responde `404` |

El endpoint traduce `IFormFile` a `ImportFileUpload(ImportEntityType, FileName, Stream, Length)`; Application no conoce `IFormFile`.

DTO de resumen: `{ id, status, requestedBy, createdAt, confirmedAt, completedAt, stages: [{ entityType, stageOrder, fileName, delimiter, fileEncoding, status, failureReason, totalRows, validRows, invalidRows, skippedRows, importedRows }], rows: { page, pageSize, total, items: [{ entityType, rowNumber, rawData, outcome, errors: [{ code, message, level }] }] } }`.

### D-10 Registro de contratos de este change

| Contrato | Tipo | Productor | Consumidor |
|---|---|---|---|
| Endpoints de D-09 | HTTP | DataImport | `fleet-maintenance-ui` (`/carga`) |
| `bulk?dryRun=` de los 5 dueños (D-07) | HTTP | `supporting-contexts`, `maintenance-core` | DataImport |
| Códigos de referencia de D-06 | Catálogo de errores | `supporting-contexts`, `maintenance-core` | DataImport |
| `IFileReader`, `IImportDefinition`, `IOwnerServiceClient`, `ICsvWriter`, `IImportJobRepository`, `IImportJobQueries` | Puertos internos | Application | Infrastructure |

DataImport no produce ni consume eventos de integración.

## Contratos nuevos requeridos

Pendientes de agregar al registro de `foundation-platform/design.md` y de aceptar por los changes dueños:

1. **Payloads del lote masivo** en `RutaViva.IntegrationContracts.Bulk`: `WorkshopBulkRow`, `CustomerBulkRow`, `ApproverBulkRow`, `VehicleBulkRow` y `RoutineBulkRow` con los campos de D-07 (más `RowNumber`), y `BulkRequest<TRow>(IReadOnlyList<TRow> Rows)`. Hoy el registro solo dice «campos de la entidad».
2. **Errores de referencia con código estable** devueltos por los dueños en `bulk`: `Customer.NotFound`, `Vehicle.OwnerNotFound`, `Vehicle.WorkshopNotFound`, `Vehicle.ResponsibleNotFound` y `MaintenancePlan.NotFound` (D-06).
3. **Resolución por clave natural en los dueños**: Fleet necesita `TaxId` en `fleet.CustomerReferences` y `fleet.WorkshopReferences`, y `Email` en `fleet.ApproverReferences`, para traducir `ownerTaxId`, `workshopTaxId` y `responsibleApproverEmail` (los eventos `CustomerRegistered`, `WorkshopRegistered` y `ApproverRegistered` ya llevan esos campos). Customers resuelve `customerTaxId` sobre sus propios datos. Maintenance resuelve `plate` con `MaintenancePlans.Plate` y `routineTypeCode` con `RoutineTypes.Code`.
4. **Idempotencia `Skipped`** por clave natural en cada dueño: `taxId` (talleres, clientes), `customerTaxId + email` (aprobadores), `plate` (vehículos) y `plate + routineTypeCode` (rutinas).
5. **Parámetro `entity`** en `GET /api/v1/imports/{id}/errors.csv` (el registro no lo tiene; sin él no se sabe qué encabezado usar cuando hay varias etapas).
6. **Columna propia** `imports.ImportStages.FailureReason` y el campo `level` en `ImportRows.Errors` (afectan solo al schema `imports`; actualizar `data-model.md` en la tarea 8.2 de `foundation-platform`).

## Patrones aplicados

| Patrón | Dónde | Por qué |
|---|---|---|
| Strategy | `IImportDefinition` por entidad (5 clases) | Encabezados, formato, clave, mapeo y referencias varían por archivo; el motor no cambia (OCP) |
| Template Method | `ImportStagePipeline` con `PreviewStagePipeline` y `ConfirmationStagePipeline` | Esqueleto fijo de cada etapa con pasos que varían por fase |
| Adapter | `IOwnerServiceClient` por servicio | Aísla HTTP, rutas y resiliencia de Application (DIP) |
| Registry | `ImportDefinitionRegistry`, `OwnerServiceClientRegistry` | Resolución por `ImportEntityType` sin `switch` |
| Policy (DDD) | `ImportStageOrder`, `PropagationWaitPolicy` | Reglas explícitas y con nombre en lugar de números sueltos |
| Aggregate | `ImportJob` | Las transiciones y totales del trabajo son consistentes en una sola unidad |

## Antipatrones evitados

| Antipatrón | Cómo se evita |
|---|---|
| Modelo anémico | Estados, totales y reglas de confirmación viven en `ImportJob` e `ImportStage`; los handlers solo orquestan |
| Objeto dios | Lector, validador por entidad, pipeline, adaptadores y escritor de CSV son piezas separadas; ningún handler conoce CsvHelper ni HTTP |
| Copy-paste | Una sola implementación de lotes, espera y paginación; las 5 entidades solo aportan su definición |
| Números mágicos | `ImportLimits` (5 MB, 5.000 filas, 500 por lote, 200 por página, 10 s, 500 ms) como constantes con nombre |
| Martillo dorado | DataImport no usa el EventBus para algo que exige respuesta síncrona por fila (ADR-0022 ALT-003) |
| Duplicar reglas del dueño | El nivel 2 solo valida forma; el negocio lo decide el dueño con `dryRun` |

## Risks / Trade-offs

- [R-01 La carga no es atómica entre etapas] → Idempotencia por clave natural (`Skipped`) y recarga del mismo archivo (ADR-0022 NEG-003).
- [R-02 Una fila `Valid` en vista previa puede fallar al confirmar (reglas que dependen de datos del mismo trabajo, o cambios entre vista previa y confirmación)] → La fila queda `Failed` con el error del dueño, el trabajo `CompletedWithErrors` y el CSV de errores la incluye.
- [R-03 Confirmación síncrona larga (5 etapas, esperas de hasta 10 s)] → Límites de tamaño, lotes de 500 y *timeout* del Gateway para `/api/v1/imports/**` ampliado a 120 s (a coordinar con `edge-gateway-notifications`).
- [R-04 Agregado grande (hasta 25.000 filas)] → Se carga completo solo en los dos commands; las consultas proyectan y paginan.
- [R-05 Dependencia de los códigos de error de otros changes] → Prueba de integración con dueños simulados que fija el catálogo; los códigos se agregan al registro de contratos.
- [R-06 Detección de codificación ambigua (un archivo ASCII puro es válido en las tres)] → Se reporta `Utf8`; el contenido es idéntico en ese caso.

## Migration Plan

Migración inicial de EF Core del schema `imports` con el usuario administrativo. Sin datos previos.

## Open Questions

- Si el CRM necesita listar los trabajos anteriores (`GET /api/v1/imports`). No está en el registro ni en el mockup; se agregaría sin cambiar el modelo.
