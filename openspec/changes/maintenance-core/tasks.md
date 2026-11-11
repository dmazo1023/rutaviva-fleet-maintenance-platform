> Plan de ejecución en paralelo: [parallel-execution.md](parallel-execution.md)

> Rutas abreviadas: `{D}` = `src/Services/Maintenance/RutaViva.Maintenance.Domain`, `{A}` = `…Application`, `{I}` = `…Infrastructure`, `{W}` = `…Api`, `{TD}` = `tests/Services/Maintenance/RutaViva.Maintenance.Domain.UnitTests`, `{TA}` = `…Application.UnitTests`, `{TI}` = `…IntegrationTests`. Depende de `foundation-platform` (BuildingBlocks y `RutaViva.IntegrationContracts` compilando). Antes de la ola que los use, el orquestador incorpora CN-01 a CN-05 de `design.md` al registro de foundation.

## 1. Proyectos

- [x] 1.1 Crear `{D}`, `{A}`, `{I}`, `{W}`, `{TD}`, `{TA}`, `{TI}` con sus referencias permitidas, agregarlos a `RutaViva.slnx` y agregar `Maintenance` a la lista de servicios de `tests/RutaViva.Architecture.Tests`. Verificar con `dotnet build RutaViva.slnx` y `dotnet test tests/RutaViva.Architecture.Tests` en verde

## 2. Contratos de dominio

- [x] 2.1 Enums (`RoutineTypeStatus`, `MaintenancePlanStatus`, `RoutineState`, `RoutineDueStatus`, `DueDimension`, `AlertType`, `AlertTrigger`, `AlertStatus`, `AlertResolutionReason`, `OdometerReadingOutcome`, `OdometerReadingIgnoredReason`, `ParameterSource`), IDs fuertemente tipados, `MaintenanceTimeZone` y catálogo `MaintenanceErrors` con los códigos de CN-03, en `{D}/Common/`, `{D}/Plans/`, `{D}/Alerts/`, `{D}/RoutineTypes/`. Verificar con `dotnet build` y la prueba de arquitectura de enums que empiezan en 1
- [x] 2.2 Objetos de valor `LicensePlate`, `Odometer`, `OdometerReading`, `MaintenanceInterval`, `DueSoonThreshold`, `RoutineOverrides`, `EffectiveRoutineParameters`, `MaintenanceCycle`, `NextDue`, `ServiceRecord`, `EvaluationContext` en `{D}/Plans/ValueObjects/` (design D-02). Verificar con pruebas en `{TD}/ValueObjects/` (normalización de placa, `IsAdvancedBy` con igual y menor, intervalos sin valores, `0 < % < 100`, `MaintenanceCycle.Next`)
- [x] 2.3 Eventos de dominio y `RoutineStatusSnapshot` en `{D}/Plans/Events/`, `{D}/Alerts/Events/`, `{D}/RoutineTypes/Events/` (design D-05). Verificar con `dotnet build`

## 3. Evaluación (núcleo de RN-1 y RN-2)

- [x] 3.1 `IDueDimensionEvaluator`, `DistanceDueEvaluator`, `TimeDueEvaluator` y `DimensionAssessment` en `{D}/Plans/Evaluation/` (design D-03). Verificar con pruebas en `{TD}/Evaluation/`: 8.999 → `UpToDate`, 9.000 → `DueSoon`, 10.000 → `Overdue`; 184 días con 10 % → ventana 18 y 13 días restantes = `DueSoon`; 365 días con 33 restantes = `DueSoon`; fecha igual al vencimiento = `Overdue`; consumo > 100 %
- [x] 3.2 `DueStatusPolicy`, `RoutineAssessment`, `RoutineTransitionPolicy` y `RoutineTransition` en `{D}/Plans/Evaluation/`. Verificar con pruebas parametrizadas de la tabla de transiciones (A-03 salto directo, A-04 `DistanceAndTime`, A-16 `Overdue` no retrocede, `DueSoon → UpToDate` revierte) y `ConsumedPercent` = máximo de dimensiones
- [x] 3.3 Agregado `RoutineType`, `RoutineTypeParameters` y `RoutineParameterCatalog` en `{D}/RoutineTypes/`. Verificar con pruebas en `{TD}/RoutineTypes/`: validaciones, `RoutineTypeParametersChanged` emitido, búsqueda por código inexistente

## 4. Agregados del plan y de alertas

> 4.1 a 4.3 modifican `MaintenancePlan.cs` y `MaintenanceRoutine.cs`: un solo agente, en secuencia.

- [x] 4.1 `MaintenanceRoutine` y `MaintenancePlan` con `Create`, `RegisterRoutine` (A-01, `Routine.AlreadyExists`, `Routine.LastServiceKmAboveOdometer`, `Routine.IncompleteLastService`) y construcción de snapshots, en `{D}/Plans/`. Verificar con pruebas en `{TD}/Plans/RegisterRoutineTests.cs` usando `MaintenancePlanBuilder`: alta vencida emite `RoutineRegistered` y luego `RoutineBecameOverdue`; EVA30D con 20 % queda `DueSoon` y con 10 % `UpToDate`
- [x] 4.2 `RecordOdometerReading` y `Reevaluate` (fecha efectiva de design D-04, `CanBeEvaluated`, rutinas inactivas omitidas). Verificar con pruebas en `{TD}/Plans/EvaluationScenariosTests.cs` que reproducen la tabla de carga de `samples/README.md` (4 `DueSoon`, 4 `Overdue`), los casos de telemetría 1 a 6 y las fechas 2027-01-20 y 2027-02-10
- [ ] 4.3 `UpdateRoutineOverrides`, `ChangeRoutineState`, `RestartRoutine` (A-08), `Suspend` y `Resume` (idempotentes). Verificar con pruebas en `{TD}/Plans/`: A-16 (reversión y aviso no repetido), A-17 (desactivar no toca la orden y emite `RoutineStateChanged`), A-18 (suspendido registra odómetro sin evaluar; reanudar reevalúa), RN-5 (ciclo 2, odómetro actualizado)
- [x] 4.4 Agregado `Alert` (`Raise`, `Acknowledge`, `Resolve` idempotente) en `{D}/Alerts/`. Verificar con pruebas en `{TD}/Alerts/AlertTests.cs` de las 4 transiciones válidas y `Alert.InvalidTransition`

## 5. Contratos de Application

- [x] 5.1 Puertos de design D-06 en `{A}/Abstractions/`, commands, queries y DTOs de design D-07 (incluidos `MaintenancePlanSummaryDto`, `MaintenancePlanDetailDto`, `RoutineTrackDto`, `AlertDto`, `ReevaluationSummary`, `RoutineBulkRow`) en sus carpetas por feature. Verificar con `dotnet build` y la prueba de arquitectura de Application

## 6. Handlers de Application

- [ ] 6.1 `TelemetryReadingTranslator`, `IngestTelemetryReadingHandler`, `RecordOdometerReadingHandler` y `SingleRetryOnConcurrencyConflict` en `{A}/Telemetry/`. Verificar con pruebas en `{TA}/Telemetry/` con NSubstitute: placa desconocida → `UnknownVehicle` sin historial; lectura ignorada → historial `Ignored` y `OdometerReadingRecordedV1` con `CurrentOdometerKm` previo; conflicto doble → `Concurrency.Conflict`
- [ ] 6.2 `ReevaluateMaintenancePlansHandler` y su validator en `{A}/Evaluations/`. Verificar con pruebas en `{TA}/Evaluations/`: paginación de 100, un plan que lanza error no detiene el lote y se registra `TimeEvaluationFailed`, resumen correcto
- [ ] 6.3 `UpdateRoutineTypeParametersHandler`, validator y `ReevaluatePlansOnRoutineTypeChangedHandler` en `{A}/RoutineTypes/`. Verificar con pruebas en `{TA}/RoutineTypes/`: solo se reevalúan planes activos con rutinas del tipo
- [ ] 6.4 `UpdateRoutineParametersHandler`, `ChangeRoutineStateHandler` y validators en `{A}/Routines/`. Verificar con pruebas en `{TA}/Routines/`: plan inexistente → `NotFound`; valores inválidos → `Validation`
- [ ] 6.5 `RegisterRoutinesBulkHandler`, `BulkRoutineRowValidator` y `BulkDuplicateDetector` en `{A}/Routines/Bulk/`. Verificar con pruebas en `{TA}/Routines/Bulk/`: 501 filas → `Validation`; `dryRun` no llama a `SaveChangesAsync`; códigos de CN-03 por fila; `Routine.AlreadyExists` → `Skipped`
- [ ] 6.6 Handlers de alertas (`RaiseDueSoonAlertHandler`, `RaiseOverdueAlertHandler`, `ResolveAlertsOnRevertHandler`, `ResolveAlertsOnRestartHandler`, `ResolveAlertsOnDeactivationHandler`) y `AcknowledgeAlertHandler` en `{A}/Alerts/`. Verificar con pruebas en `{TA}/Alerts/`: `DueSoon` sin primer aviso no crea alerta; escalamiento resuelve antes de crear
- [ ] 6.7 `CreateMaintenancePlanHandler`, `ChangePlanStatusHandler`, `RestartRoutineHandler` en `{A}/Plans/` y handlers de integración `VehicleRegisteredHandler`, `VehicleStatusChangedHandler`, `ServiceOrderCompletedHandler` en `{A}/IntegrationEvents/Inbound/`. Verificar con pruebas en `{TA}/IntegrationEvents/`: plan existente no se duplica; rutina desconocida en `ServiceOrderCompleted` → éxito con log
- [x] 6.8 Mappers de dominio a `V1` en `{A}/IntegrationEvents/Outbound/` (uno por evento de la tabla de design D-05). Verificar con pruebas en `{TA}/IntegrationEvents/Outbound/` que cada mapper copia el snapshot completo y el stream correcto
- [ ] 6.9 Query handlers de plan (detalle con pistas, resúmenes), lecturas, alertas y tipos en `{A}/Plans/Queries/`, `{A}/Telemetry/Queries/`, `{A}/Alerts/Queries/`, `{A}/RoutineTypes/Queries/`. Verificar con pruebas en `{TA}/Queries/`: `limit` > 200 → `Validation`, `pageSize` > 100 → `Validation`

## 7. Infraestructura

- [ ] 7.1 `MaintenanceDbContext`, configuraciones de las 7 tablas (design D-08, CN-04), `RoutineTypeSeed` y migración inicial en `{I}/Persistence/`. Verificar con `dotnet ef migrations script` sin errores y con prueba en `{TI}/Persistence/SchemaTests.cs` (BD desechable `SqlServerTestDatabase`, ADR-0027) que compara `Enum.GetNames<T>()` con cada `CHECK` (E-03) y que existen los índices únicos
- [ ] 7.2 Repositorios (`MaintenancePlanRepository`, `RoutineTypeRepository`, `AlertRepository`), `OdometerReadingLog` y `UnitOfWorkScopeFactory` en `{I}/Persistence/Repositories/`. Verificar con pruebas de integración en `{TI}/Persistence/`: `UX_Alerts_OnePerCycleAndType` rechaza el duplicado; `rowversion` produce conflicto
- [ ] 7.3 Implementaciones de consulta (`MaintenancePlanQueries` con las políticas puras de design D-03, `OdometerReadingQueries`, `AlertQueries`, `RoutineTypeQueries`) en `{I}/Persistence/Queries/`. Verificar con pruebas de integración: pista km de TAN201 con `consumedPercent = 108` y `remaining = -800`; resumen de RVA104 con conteos por estado
- [ ] 7.4 `DailyTimeEvaluationScheduler` con `TimeEvaluationScheduleOptions` en `{I}/Scheduling/` y `DependencyInjection.cs` (DbContext, interceptor, Outbox relay, suscriptor `"maintenance"` con sus 3 handlers, decoradores). Verificar con prueba unitaria del cálculo de la próxima ejecución con `FakeClock` (23:59 y 00:06 en Bogotá) y con `dotnet build`

## 8. API

- [ ] 8.1 `TelemetryEndpoints` (request `TelemetryOdometerReadingRequest`, respuesta `202` con `outcome`/`reason`) y `TimeEvaluationEndpoints` (`time-evaluations` y `reevaluations`) en `{W}/Endpoints/`. Verificar con pruebas `WebApplicationFactory` en `{TI}/Api/`: `202` en lectura ignorada, `400` en km negativo, mismo resumen en ambos endpoints
- [ ] 8.2 `PlanEndpoints`, `RoutineEndpoints` (ajuste, estado, bulk), `RoutineTypeEndpoints` y `AlertEndpoints` en `{W}/Endpoints/` y `Program.cs`. Verificar con pruebas `WebApplicationFactory`: `404` de plan inexistente, `422` con `Alert.InvalidTransition`, `400` con bulk de 501 filas

## 9. Pruebas de integración de punta a punta del servicio

- [ ] 9.1 `{TI}/Scenarios/DemoLoadScenarioTests.cs`: publica `VehicleRegisteredV1` de los 10 vehículos en un EventBus en proceso, carga las 15 rutinas por el endpoint bulk con `FakeClock` en 2026-11-12 y ejecuta los 7 casos de telemetría y las 2 fechas. Verificar con `dotnet test --filter Category=Integration`: 4 `DueSoon`, 4 `Overdue`, resultados por caso y eventos en el orden de design D-05
- [ ] 9.2 `{TI}/Scenarios/RestartIdempotencyTests.cs`: `ServiceOrderCompletedV1` entregado 5 veces reinicia la rutina una vez (RN-5) y `VehicleStatusChangedV1(Inactive)` repetido publica un solo `MaintenancePlanStatusChanged`. Verificar con `dotnet test --filter Category=Integration`

## 10. Muestras de la demo

- [x] 10.1 Crear `samples/http/odometer-readings.http` (los 7 casos con el resultado esperado en el nombre de cada petición) y `samples/http/time-evaluations.http` (2027-01-20 y 2027-02-10), con la URL base del Gateway como variable. Verificar ejecutándolos contra el servicio local después de la carga y comparando con `samples/README.md`
- [x] 10.2 Actualizar la sección 3 de `samples/README.md` de Kafka a HTTP y retirar `samples/kafka/` (C-01 de foundation). Verificar con una búsqueda de `kafka` en `samples/` sin resultados

## 11. Recortables

- [ ] 11.1 (Recortada por plazo; fuera del alcance aprobado) `tests/RutaViva.Maintenance.AcceptanceTests` con Reqnroll: un `.feature` por regla (RN-1, RN-2, RN-5, RN-6) sobre el dominio y la API en proceso. Verificar con `dotnet test tests/RutaViva.Maintenance.AcceptanceTests` en verde
