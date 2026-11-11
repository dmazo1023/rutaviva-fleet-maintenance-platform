> Rutas base: `src/Services/DataImport/RutaViva.DataImport.{Domain,Application,Infrastructure,Api}/` (abreviadas `Domain/`, `Application/`, `Infrastructure/`, `Api/`) y `tests/Services/DataImport/RutaViva.DataImport.{Domain.UnitTests,Application.UnitTests,IntegrationTests}/`. Requiere `foundation-platform` completo.

## 1. Proyectos y contratos compartidos

- [ ] 1.1 Crear los 4 proyectos de DataImport y los 3 de pruebas, con referencias permitidas (Domain → BuildingBlocks.Domain; Application → Domain, BuildingBlocks.Application, RutaViva.IntegrationContracts; Infrastructure → Application, BuildingBlocks.Infrastructure, CsvHelper, Microsoft.Extensions.Http.Resilience; Api → Infrastructure, BuildingBlocks.Api) y agregarlos a `RutaViva.slnx`. Verificar con `dotnet build RutaViva.slnx` y `dotnet test tests/RutaViva.Architecture.Tests` en verde incluyendo los ensamblados de DataImport
- [ ] 1.2 Agregar a `src/Contracts/RutaViva.IntegrationContracts/Bulk/` los payloads `BulkRequest<TRow>`, `WorkshopBulkRow`, `CustomerBulkRow`, `ApproverBulkRow`, `VehicleBulkRow`, `RoutineBulkRow` (campos de design D-07) y los códigos de referencia de design D-06 como constantes en `BulkReferenceErrorCodes`; registrar ambos en `openspec/changes/foundation-platform/design.md` § Contrato del lote masivo. Verificar con un *snapshot* `ContractSnapshot` (ADR-0027) por payload en `tests/RutaViva.IntegrationContracts.Tests`

## 2. Dominio

- [ ] 2.1 `Domain/Imports/`: enums `ImportJobStatus`, `ImportEntityType`, `ImportStageStatus`, `ImportRowOutcome`, `FileEncoding`, `ImportErrorLevel` (valores desde 1), VOs `ImportRowError` e `ImportFileDescriptor`, IDs `ImportJobId`/`ImportStageId`, política `ImportStageOrder`, constantes `ImportLimits` y catálogo `ImportErrors` (`ImportJob.InvalidStatus`, `ImportJob.NothingToImport`, `ImportJob.DuplicateEntityType`, `ImportFile.TooLarge`, `ImportFile.TooManyRows`, `ImportFile.UnknownDelimiter`, `ImportFile.InvalidHeaders`, `ImportRow.DuplicateInFile`, `ImportOwner.Unavailable`, `ImportOwner.UnexpectedResponse`). Verificar con pruebas de `ImportStageOrder` (Workshops = 1 … Routines = 5) y de igualdad de VOs
- [ ] 2.2 `Domain/Imports/ImportRow.cs` e `ImportStage.cs`: `FailFile`, `RecordFormatResults`, `ApplySimulation`, `ResolveInJobReferences`, `ApplyImportResults` y totales calculados (design D-01). Verificar con pruebas `Method_Should_Result_When_Condition`: `Invalid` sin errores es rechazado, `RowNumber` repetido es rechazado, `ApplyImportResults` ignora filas no `Valid`, totales coherentes
- [ ] 2.3 `Domain/Imports/ImportJob.cs`: `Start`, `CompleteValidation`, `Confirm`, `Complete`, `Fail` y sus invariantes. Verificar con pruebas: 0 o 6 archivos rechazados, entidad repetida rechazada, etapas ordenadas aunque lleguen desordenadas, `Confirm` fuera de `PendingConfirmation` → `ImportJob.InvalidStatus`, sin filas válidas → `ImportJob.NothingToImport`, `Complete` con una fila `Failed` → `CompletedWithErrors`

## 3. Puertos y definiciones de importación

- [ ] 3.1 `Application/Abstractions/`: `IFileReader`, `CsvDocument`, `CsvRecord`, `IImportDefinition`, `ReferenceDependency`, `IOwnerServiceClient`, `ICsvWriter`, `IImportJobRepository`, `IImportJobQueries` y los DTOs `ImportJobDto`, `ImportStageDto`, `ImportRowDto`, `ImportRowFilter`, `ImportErrorRows` (design D-03 y D-09). Verificar con `dotnet build` y la prueba de arquitectura (Application sin CsvHelper ni AspNetCore)
- [ ] 3.2 `Application/Definitions/FieldRules.cs` y `FieldParsers.cs`: NIT, correo, placa, VIN, enteros, porcentaje, fechas `yyyy-MM-dd` y `dd/MM/yyyy` y fecha no futura con `IClock`, cada regla como método pequeño que devuelve `ImportRowError?`. Verificar con pruebas parametrizadas de cada regla, incluidos los límites (porcentaje 0 y 100 inválidos, año actual + 1 válido)
- [ ] 3.3 `Application/Definitions/WorkshopImportDefinition.cs`, `CustomerImportDefinition.cs` y `ApproverImportDefinition.cs` (encabezados, clave natural, formato, `ToBulkRow` y `ReferenceErrors`). Verificar con pruebas sobre las filas de `samples/import/talleres.csv`, `clientes.csv` y `aprobadores.csv` (sin errores de formato) y una fila con NIT de 3 dígitos
- [ ] 3.4 `Application/Definitions/VehicleImportDefinition.cs`. Verificar con pruebas sobre `samples/import/con-errores/vehiculos.csv`: errores de formato exactamente en `RV-107`, `EVA304`, `EVA305`, `EVA306` y `EVA307`, y mapeo de `nit_dueno` → `ownerTaxId`
- [ ] 3.5 `Application/Definitions/RoutineImportDefinition.cs`. Verificar con pruebas sobre `samples/import/con-errores/rutinas.csv` con `FakeClock` en 2026-11-12: errores de formato exactamente en `TAN202 · TIRES`, `EVA302 · OIL_CHANGE`, `EVA30D · BRAKES` y `RVA104 · TIRES`; `EVA301;OIL_CHANGE;;;;;` sin errores y con `null` en los campos vacíos
- [ ] 3.6 `Application/Definitions/ImportDefinitionRegistry.cs`, `Application/Owners/OwnerServiceClientRegistry.cs` y la detección de duplicados en archivo `DuplicateKeyDetector.cs`. Verificar con pruebas: la segunda `RVA106` queda con `ImportRow.DuplicateInFile` y la primera no; entidad sin definición → error explícito al arrancar

## 4. Pipelines

- [ ] 4.1 `Application/Pipelines/ImportStagePipeline.cs` (Template Method), `ImportStageContext.cs`, `InJobKeyIndex.cs` y `PropagationWaitPolicy.cs` (500 ms, 10 s, con `TimeProvider`). Verificar con pruebas de `PropagationWaitPolicy` con `FakeTimeProvider`: corta a los 10 s y termina antes si la condición se cumple
- [ ] 4.2 `Application/Pipelines/PreviewStagePipeline.cs`: encabezados, formato, duplicados, simulación por lotes de 500 y resolución de referencias del mismo trabajo. Verificar con pruebas con `IOwnerServiceClient` sustituido (NSubstitute): 1.200 filas → 3 llamadas con `dryRun=true`; error `Vehicle.OwnerNotFound` con NIT presente en la etapa `Customers` del trabajo → fila `Valid`; con NIT ausente → `Invalid` nivel `Business`
- [ ] 4.3 `Application/Pipelines/ConfirmationStagePipeline.cs`: espera de propagación, envío de filas `Valid` con `dryRun=false` y aplicación de resultados. Verificar con pruebas: solo se envían las filas `Valid`; error de referencia durante 2 s y luego éxito → filas `Imported`; error persistente 10 s → filas `Failed`; `ImportOwner.Unavailable` → etapa `Failed`

## 5. Casos de uso

- [ ] 5.1 `Application/Imports/CreateImportJob/` (`CreateImportJobCommand` con `ImportFileUpload`, validador FluentValidation y handler que recorre las etapas con `PreviewStagePipeline` y llama a `CompleteValidation`). Verificar con pruebas: carga parcial de 2 archivos → 2 etapas; encabezado inválido → trabajo `Failed`
- [ ] 5.2 `Application/Imports/ConfirmImportJob/` (handler que confirma, ejecuta `ConfirmationStagePipeline` por etapa, detiene en la primera etapa `Failed` y completa). Verificar con pruebas: etapa 4 `Failed` → etapa 5 no se ejecuta y el trabajo queda `Failed`; confirmación repetida → `ImportJob.InvalidStatus`
- [ ] 5.3 `Application/Imports/GetImportJob/` y `Application/Imports/GetImportErrorsCsv/` (query paginada con filtros y CSV de errores con columna `error` y mensajes unidos por ` | `, usando `ICsvWriter`). Verificar con pruebas de la query de errores: solo filas `Invalid` y `Failed`, `RawData` sin modificar y separador de la etapa

## 6. Infraestructura

- [ ] 6.1 `Infrastructure/Persistence/ImportsDbContext.cs` (schema `imports`), configuraciones de `ImportJob`, `ImportStage`, `ImportRow` (errores como JSON, `FailureReason`, enums por nombre, `CHECK` de cada enum) y migración inicial. Verificar con prueba de integración (BD desechable `SqlServerTestDatabase`, ADR-0027) que aplica la migración y compara `Enum.GetNames<T>()` con los `CHECK` de `sys.check_constraints` (data-model E-03)
- [ ] 6.2 `Infrastructure/Persistence/ImportJobRepository.cs` e `ImportJobQueries.cs` (proyección `AsNoTracking` paginada, máximo 200). Verificar con prueba de integración: guardar un trabajo con 2 etapas y 30 filas y leerlo filtrado por `entity=Vehicles&outcome=Invalid`
- [ ] 6.3 `Infrastructure/Files/CsvFileReader.cs` (design D-04) y `Infrastructure/Files/CsvHelperWriter.cs`. Verificar con pruebas sobre copias en memoria de `samples/import/talleres.csv` convertidas a Windows-1252 con `;`, a UTF-8 con BOM con `,` y a UTF-8 sin BOM: separador y codificación detectados y «Medellín» intacto; archivo de 5 MB + 1 byte → `ImportFile.TooLarge`; 5.001 filas → `ImportFile.TooManyRows`
- [ ] 6.4 `Infrastructure/Owners/WorkshopsServiceClient.cs`, `CustomersServiceClient.cs`, `FleetServiceClient.cs` y `MaintenanceServiceClient.cs` con `HttpClient` tipado, *service discovery* y `AddStandardResilienceHandler`. Verificar con pruebas con `HttpMessageHandler` simulado: ruta y `dryRun` correctos por entidad, `503` persistente → `ImportOwner.Unavailable`, cuerpo inválido → `ImportOwner.UnexpectedResponse`

## 7. API

- [ ] 7.1 `Api/Imports/ImportsEndpointModule.cs`: `POST /api/v1/imports` (multipart con un campo por entidad y `requestedBy`, límite 26 MB), `POST /api/v1/imports/{id}/confirm` y `GET /api/v1/imports/{id}`, más `Api/Program.cs` y `DependencyInjection` por capa. Verificar con pruebas `WebApplicationFactory`: entidad repetida → `400`; trabajo inexistente → `404`; carga válida → `201` con `Location`
- [ ] 7.2 `GET /api/v1/imports/{id}/errors.csv?entity=` en el mismo módulo. Verificar con prueba `WebApplicationFactory`: `text/csv`, nombre `vehiculos_errores.csv` y encabezado original más `error`

## 8. Pruebas de integración de punta a punta

- [ ] 8.1 `IntegrationTests/InitialLoadTests.cs`: dueños simulados en memoria (stub HTTP que aplica idempotencia y referencias por clave natural) y los 5 archivos de `samples/import/` leídos desde el repositorio. Verificar con `dotnet test --filter InitialLoadTests`: vista previa con 36 filas `Valid`, confirmación `Completed` con 3/3/5/10/15 `Imported`, y recarga con 36 `Skipped` + `422 ImportJob.NothingToImport`
- [ ] 8.2 `IntegrationTests/ErrorFilesTests.cs` con `samples/import/con-errores/` sobre la carga válida. Verificar con `dotnet test --filter ErrorFilesTests` que cada fila obtiene el resultado y el nivel de `samples/README.md` §2 y que `errors.csv?entity=Vehicles` tiene 9 líneas de datos
- [ ] 8.3 `IntegrationTests/PropagationAndFailureTests.cs`. Verificar con `dotnet test --filter PropagationAndFailureTests`: réplica que aparece a los 2 s → `Completed`; réplica que nunca aparece → `CompletedWithErrors`; dueño de vehículos caído → trabajo `Failed`, rutinas sin ejecutar y etapas anteriores conservadas

## 9. Recortables

- [ ] 9.1 (Recortable) `Application/Imports/GetImportTemplate/` y `GET /api/v1/imports/templates/{entity}.csv` en `ImportsEndpointModule`. Verificar con prueba `WebApplicationFactory`: `Vehicles.csv` devuelve `filename="vehiculos.csv"` con una sola línea de encabezados en UTF-8 con BOM; `drivers.csv` → `404`
