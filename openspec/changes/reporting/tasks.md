## 1. Proyectos y contratos internos

- [ ] 1.1 Crear `src/Services/Reporting/RutaViva.Reporting.Application`, `.Infrastructure` y `.Api`, y `tests/Services/Reporting/RutaViva.Reporting.Application.UnitTests` y `.IntegrationTests`, agregarlos a `RutaViva.slnx` y registrar Reporting (sin capa Domain) en la lista de servicios de `tests/RutaViva.Architecture.Tests`. Verificar con `dotnet build RutaViva.slnx` y `dotnet test tests/RutaViva.Architecture.Tests`
- [ ] 1.2 Abstracciones de proyección en `RutaViva.Reporting.Application/Projections/Abstractions/`: `ProjectionRow` (`LastEventOffset`, `LastEventAt`, `TryAdvance(EventMetadata)`), `IProjectionStore<TRow>` e `IProjectionMaintenance` (design D-03, D-04, D-05). Verificar con pruebas en `Application.UnitTests/Projections/ProjectionRowTests.cs`: offset mayor avanza, igual o menor no avanza
- [ ] 1.3 Filas de proyección en `Application/Projections/Rows/`: `CustomerDimensionRow`, `WorkshopDimensionRow`, `VehicleDimensionRow`, `VehicleRoutineStatusRow`, `ServiceOrderFactsRow` (con `ServiceOrderPhaseMap` y `RecordPhase` que solo llena fechas vacías) y `AlertFactsRow`, con métodos de intención que llaman a `TryAdvance` (design D-02). Verificar con pruebas unitarias de `ServiceOrderFactsRow`: `OnSite` repetido conserva la primera fecha y `Quoted`/`Approved` con el mismo `OccurredAt` llenan ambas fechas
- [ ] 1.4 Abstracciones de reportes en `Application/Reports/Abstractions/`: `ReportCode`, `ReportFormat`, `ColumnFormat`, `ReportColumn`, `ReportDescriptor`, `ReportFilter`, `PageRequest`, `ReportRow`, `ReportPage`, `ReportParameter`, `ReportDocument`, `IReportDefinition`, `IReportWriter`, `IReportRowSource<TFilter, TRow>`, `ReportErrors` y `ReportingOptions` con las constantes 50/200/50.000 (design D-06, D-09). Verificar con `dotnet build` y la prueba de arquitectura (Application sin ClosedXML ni EF)

## 2. Persistencia

- [ ] 2.1 `RutaViva.Reporting.Infrastructure/Persistence/ReportingDbContext.cs` con `HasDefaultSchema("reporting")`, `ApplyRutaVivaConventions`, configuraciones de las 6 tablas sin `CHECK` de enums ni FK (con `Plate` y `CustomerId` en `VehicleRoutineStatus`, `LastEventOffset` en todas), índices del script de referencia, `EventCheckpointConfiguration` y migración `ReportingInitial`. Verificar con `dotnet ef migrations script` sin errores y prueba de modelo que confirma que no hay `CHECK` en columnas de estado
- [ ] 2.2 `EfProjectionStore<TRow>` y `EfProjectionMaintenance` (vaciado de 6 tablas + checkpoint `reporting` a 0 en una transacción). Verificar con prueba de integración (BD desechable `SqlServerTestDatabase`, ADR-0027): tras `ResetAsync` las tablas están vacías y `LastOffset = 0`

## 3. Proyecciones

- [ ] 3.1 `CustomerDimensionProjection`, `WorkshopDimensionProjection` y `VehicleDimensionProjection` en `Application/Projections/Handlers/` (incluye `OdometerReadingRecorded` → `CurrentOdometerKm`). Verificar con pruebas unitarias con `IProjectionStore` en memoria: lectura ignorada no cambia el odómetro; vehículo desactivado queda `Inactive`
- [ ] 3.2 `VehicleRoutineStatusProjection` para los 7 eventos de rutina con `RoutineSnapshot`. Verificar con pruebas unitarias: `MaintenanceRoutineOverdue` deja `Overdue` y consumo 108; `MaintenanceRoutineRestarted` deja ciclo nuevo y `UpToDate`; evento con offset menor no cambia la fila
- [ ] 3.3 `ServiceOrderFactsProjection` para los 6 eventos de `serviceorders.service-order`. Verificar con pruebas unitarias: creación → cotización → aprobación automática con fechas iguales; `ServiceOrderCompleted` llena `CompletedAt`, `ServicedKm` y `ServicedAt`; `ToStatus` desconocido se guarda sin error
- [ ] 3.4 `AlertFactsProjection` para `AlertRaised` y `AlertStatusChanged`, con registro `ProjectionRowMissing` si la fila no existe. Verificar con pruebas unitarias: generada → atendida → resuelta con las tres fechas y el motivo
- [ ] 3.5 `ReportingSubscriptions.Register` con `AddEventBusSubscriber("reporting", StartPosition.Checkpoint)` y un `Handle<TEvent, THandler>` por evento de los 6 streams; `RebuildProjectionsCommand` y `ProjectionRebuildStartupTask` registrado antes del suscriptor (design D-05). Verificar con prueba de integración (BD desechable `SqlServerTestDatabase` + EventBus en proceso, ADR-0027): 3 entregas del mismo evento = 1 efecto; reconstrucción produce proyecciones idénticas a las previas

## 4. Reportes imprescindibles (R-01, R-02)

- [ ] 4.1 `ReportDefinition<TFilter, TRow>` (Template Method), `ReportCatalog`, `ReportWriterResolver` y `ReportLabels` (etiquetas en español con valor original si es desconocido) en `Application/Reports/`. Verificar con pruebas unitarias: filtro desconocido y período invertido → `Report.InvalidFilter`; `pageSize = 500` → `Report.InvalidPageSize`; más de 50.000 filas → `Report.TooManyRows`
- [ ] 4.2 R-01: `FleetMaintenanceStatusReport` (columnas, filtros, km y días restantes con `IClock`) y `FleetMaintenanceStatusRowSource` en Infrastructure con orden por severidad y consumo. Verificar con prueba de integración sembrando las proyecciones con los 15 casos de `samples/README.md` §1: filtro `Overdue` devuelve RVA104 frenos, TAN201, TAN202 y TAN203 aceite, y el orden por severidad se cumple
- [ ] 4.3 R-02: `ServiceOrdersReport` (formato `OS-000123`, tiempos de ciclo en horas) y `ServiceOrdersRowSource`. Verificar con prueba de integración: orden 08:00 → 10:00 → 14:00 → 18:00 muestra 2, 4, 4 y 10 horas; orden cancelada sin tiempos posteriores
- [ ] 4.4 Queries `GetReportCatalogQuery`, `PreviewReportQuery` y `DownloadReportQuery` con sus handlers y DTOs (design D-09). Verificar con pruebas unitarias con dobles: código inexistente → `Report.NotFound`; formato no registrado → `Report.UnsupportedFormat`; nombre de archivo `R-02_ordenes_2026-11-12.xlsx` con `FakeClock`

## 5. Formato Excel

- [ ] 5.1 `XlsxReportWriter` con ClosedXML en `Infrastructure/Reports/Writers/` (hoja de datos con autofiltro, panel inmovilizado y formatos por `ColumnFormat`; hoja «Parámetros» con filtros y fecha en `America/Bogota`). Verificar con prueba que abre el archivo generado con ClosedXML y comprueba hojas, encabezados, formato de moneda de una celda `Money` y el valor de un filtro

## 6. API

- [ ] 6.1 `ReportEndpoints : IEndpointModule` (`GET /api/v1/reports`, `GET /api/v1/reports/{code}`, `GET /api/v1/reports/{code}/download?format=`) y `Program.cs` con persistencia, suscriptor, escritores y definiciones registradas. Verificar con pruebas `WebApplicationFactory`: catálogo con R-01 y sus 3 filtros; `404` para código inexistente; descarga `xlsx` con `Content-Disposition` correcto; `422` con `code = "Report.TooManyRows"`
- [ ] 6.2 Archivo `samples/http/reports.http` con catálogo, vista previa de R-01 filtrada por `Overdue` y descarga de R-02. Verificar ejecutándolo contra el servicio local con datos de la carga de ejemplo

## 7. Recortables

- [ ] 7.1 (Recortable) R-03: `MaintenanceCostsReport` y `MaintenanceCostsRowSource` (solo `Completed`, agrupado por cliente, taller y placa, separando aprobadas automáticamente y por el cliente). Verificar con prueba de integración del escenario de 1.200.000 automática + 2.000.000 por el cliente con 300.000 de adicionales, y órdenes canceladas excluidas
- [ ] 7.2 (Recortable) R-04: `AlertsAttentionReport` y `AlertsAttentionRowSource` (agrupado por tipo y origen, medias solo sobre alertas que llegaron a la fase). Verificar con prueba de integración: 3 generadas, 2 atendidas a las 2 y 4 horas → media 3 horas
- [ ] 7.3 (Recortable) `CsvReportWriter` (UTF-8 con BOM, `;`, cultura `es-CO`, comillas RFC 4180) registrado en `ReportWriterResolver`. Verificar con prueba unitaria: los 3 primeros bytes son el BOM, un valor con `;` sale entre comillas y un decimal sale con coma
