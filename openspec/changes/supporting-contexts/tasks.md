## 1. Prerrequisitos y estructura

- [x] 1.1 Confirmar con el orquestador la decisión sobre los contratos C-N1 y C-N2 de `design.md` (BuildingBlocks o local por servicio) y registrar la respuesta en `design.md`. Verificar con la sección "Contratos nuevos requeridos" actualizada
- [x] 1.2 Crear los 12 proyectos `src/Services/{Customers,Workshops,Fleet}/RutaViva.{Service}.{Domain,Application,Infrastructure,Api}` y los proyectos de prueba `tests/Services/{Service}/RutaViva.{Service}.{Domain.UnitTests,IntegrationTests}` con sus referencias permitidas, y agregarlos a `RutaViva.slnx`. Verificar con `dotnet build RutaViva.slnx` y con `tests/RutaViva.Architecture.Tests` en verde

## 2. Contratos de dominio (antes que las implementaciones)

- [ ] 2.1 Customers Domain: `CustomerId`, `ApproverId`, `TaxId`, `EmailAddress`, `PhoneNumber`, `PersonName`, enums `CustomerStatus`/`ApproverStatus`, `CustomerErrors` y eventos de dominio en `src/Services/Customers/RutaViva.Customers.Domain/`. Verificar con pruebas de VOs en `RutaViva.Customers.Domain.UnitTests` (formatos válidos e inválidos)
- [ ] 2.2 Workshops Domain: `WorkshopId`, `TaxId`, `EmailAddress`, `PhoneNumber`, `PostalAddress`, enum `WorkshopStatus`, `WorkshopErrors` y eventos de dominio en `src/Services/Workshops/RutaViva.Workshops.Domain/`. Verificar con pruebas de VOs en `RutaViva.Workshops.Domain.UnitTests`
- [ ] 2.3 Fleet Domain: `VehicleId`, `Plate` (normalización y `^[A-Z]{3}[0-9]{2}[0-9A-Z]$`), `Vin` (17, sin I/O/Q), `VehicleDescription` (año 1950..actual+1), `Kilometers`, enum `VehicleStatus`, `VehicleErrors`, eventos de dominio y las réplicas `CustomerReference`, `WorkshopReference`, `ApproverReference` en `src/Services/Fleet/RutaViva.Fleet.Domain/`. Verificar con pruebas en `RutaViva.Fleet.Domain.UnitTests`: `RV-107` inválida, ` eva30d ` → `EVA30D`, VIN de 11 caracteres inválido, año 1890 inválido

## 3. Customers

- [ ] 3.1 Agregado `Customer` con `Approver` interno: `Register`, `AddApprover` (correo único en el cliente), `Activate`/`Deactivate`, `ActivateApprover`/`DeactivateApprover` idempotentes (`Customer.cs`, `Approver.cs`). Verificar con pruebas unitarias: correo repetido devuelve `Customer.ApproverEmailAlreadyRegistered`; inactivar dos veces emite un solo evento
- [ ] 3.2 Application: commands `RegisterCustomerCommand`, `AddApproverCommand`, `ChangeCustomerStatusCommand`, `ChangeApproverStatusCommand` con validadores; puertos `ICustomerRepository`, `ICustomerUniquenessChecker`, `ICustomerQueries`; queries de listado y detalle; `CustomerIntegrationEventMapper`. Verificar con pruebas unitarias de handlers (NSubstitute): NIT tomado devuelve `Customer.TaxIdAlreadyRegistered`
- [ ] 3.3 Infrastructure: `CustomersDbContext` (schema `customers`), configuraciones, índices `UX_Customers_TaxId` y `UX_Approvers_CustomerId_Email`, traducción de 2601/2627, repositorio, queries y migración inicial. Verificar con prueba de integración (BD desechable `SqlServerTestDatabase`, ADR-0027): NIT duplicado concurrente devuelve el error de catálogo y el Outbox recibe `CustomerRegistered`
- [ ] 3.4 Api: `CustomerEndpoints` y `ApproverEndpoints` (`POST/GET /api/v1/customers`, `GET /{id}`, `POST /{id}/approvers`) y `Program.cs` con BuildingBlocks y relay del Outbox. Verificar con pruebas `WebApplicationFactory`: `201` con `Location`, `409` con `code`, `404` de cliente inexistente
- [ ] 3.5 Carga masiva: `CustomerBulkRegistrationHandler`, `ApproverBulkRegistrationHandler` y endpoints `POST /api/v1/customers/bulk` y `/api/v1/customers/approvers/bulk` (design D-05). Verificar con prueba de integración usando `samples/import/clientes.csv` y `aprobadores.csv`: simulación sin filas persistidas, carga real con 3 + 5 `Valid`, recarga con todo `Skipped`, duplicado en el lote `Bulk.DuplicateInBatch`, 501 filas = `400`

## 4. Workshops

- [ ] 4.1 Agregado `Workshop` con `Register`, `Activate`/`Deactivate` idempotentes (`Workshop.cs`). Verificar con pruebas unitarias: inactivar dos veces emite un solo evento
- [ ] 4.2 Application: `RegisterWorkshopCommand`, `ChangeWorkshopStatusCommand`, validadores, puertos `IWorkshopRepository`, `IWorkshopUniquenessChecker`, `IWorkshopQueries`, queries de listado (estado, ciudad) y detalle, `WorkshopIntegrationEventMapper`. Verificar con pruebas unitarias de handlers
- [ ] 4.3 Infrastructure: `WorkshopsDbContext` (schema `workshops`), índice `UX_Workshops_TaxId`, repositorio, queries y migración inicial. Verificar con prueba de integración: Outbox recibe `WorkshopRegistered` y el NIT duplicado devuelve el error de catálogo
- [ ] 4.4 Api y carga masiva: `WorkshopEndpoints` (`POST/GET /api/v1/workshops`, `GET /{id}`), `WorkshopBulkRegistrationHandler`, `POST /api/v1/workshops/bulk` y `Program.cs`. Verificar con pruebas `WebApplicationFactory` usando `samples/import/talleres.csv`: 3 `Valid`, luego 3 `Skipped`; filtro por ciudad

## 5. Fleet: réplicas

- [ ] 5.1 Application: puertos `IFleetReferenceReader` e `IFleetReferenceWriter` y los 6 handlers `IIntegrationEventHandler<T>` de `customers.customer` y `workshops.workshop` con *upsert* idempotente (design D-03). Verificar con pruebas unitarias: aplicar dos veces el mismo evento deja el mismo estado; un cambio de estado de una entidad desconocida se ignora con log
- [ ] 5.2 Infrastructure: `FleetDbContext` (schema `fleet`) con `Vehicles`, las 3 réplicas (con `TaxId` y `Email`), `EventCheckpoints`, índices de D-08, implementación de los puertos de réplica, suscripción `AddEventBusSubscriber("fleet", StartPosition.Checkpoint)` y migración inicial. Verificar con prueba de integración con EventBus real en proceso: publicar `CustomerRegistered` dos veces deja una fila en `CustomerReferences`

## 6. Fleet: vehículo

- [ ] 6.1 Agregado `Vehicle`: `Register` (dueño y taller activos, responsable activo opcional), `AssignResponsible` (sin evento si no cambia), `Deactivate`/`Reactivate` idempotentes con `StatusChangedAt` (`Vehicle.cs`). Verificar con pruebas unitarias: dueño inactivo devuelve `Vehicle.OwnerInactive`; responsable de otro cliente aceptado; desactivar dos veces emite un evento
- [ ] 6.2 Application: `RegisterVehicleCommand`, `AssignResponsibleCommand`, `ChangeVehicleStatusCommand` (diccionario estado → acción), validadores, `IVehicleRepository`, `IVehicleUniquenessChecker`, `VehicleIntegrationEventMapper`. Verificar con pruebas unitarias: réplica de dueño ausente devuelve `Vehicle.OwnerNotFound`; placa tomada devuelve `Vehicle.PlateAlreadyRegistered`
- [ ] 6.3 Application e Infrastructure de lectura: `IVehicleQueries` con `ListVehiclesQuery` (cliente, estado, texto, paginación), `GetVehicleDetailQuery` y `GetVehicleByPlateQuery` con nombres de las réplicas. Verificar con prueba de integración: filtro por cliente y estado con total; búsqueda `kangoo` sin distinguir mayúsculas; placa `rva101` encontrada
- [ ] 6.4 Infrastructure de escritura: `VehicleRepository`, `VehicleUniquenessChecker`, traducción de 2601/2627 de placa y VIN. Verificar con prueba de integración: dos registros concurrentes de la misma placa dejan una fila y el segundo devuelve `Vehicle.PlateAlreadyRegistered`
- [ ] 6.5 Api: `VehicleEndpoints` (`POST/GET /api/v1/fleet/vehicles`, `GET /{id}`, `GET /by-plate/{plate}`, `PATCH /{id}/status`, `PUT /{id}/responsible`) y `Program.cs` con suscriptor y relay. Verificar con pruebas `WebApplicationFactory`: `201`, `400` por placa inválida, `409` por placa repetida, `422` por dueño inexistente, `204` idempotente en estado y `409 Concurrency.Conflict`

## 7. Fleet: carga masiva

- [ ] 7.1 `VehicleBulkRegistrationHandler` (NIT → dueño y taller, correo → responsable con `Vehicle.ResponsibleNotFound` y `Vehicle.ResponsibleAmbiguous`) y `POST /api/v1/fleet/vehicles/bulk`. Verificar con prueba de integración: los 10 vehículos de `samples/import/vehiculos.csv` resultan `Valid` y publican 10 `VehicleRegistered`
- [ ] 7.2 (Recortada por plazo; fuera del alcance aprobado) Escenarios de `samples/import/con-errores/vehiculos.csv` como prueba de integración en simulación: `RVA105` y la primera `RVA106` `Valid`; segunda `RVA106` `Bulk.DuplicateInBatch`; `TAN204` `Vehicle.OwnerNotFound`; `TAN205` `Vehicle.WorkshopNotFound`; `EVA308` `Vehicle.ResponsibleNotFound`; `RVA101` `Skipped`; `RV-107`, 1890, −500, marca vacía y VIN corto `Invalid`. Verificar con `dotnet test --filter Category=Integration` y sin filas nuevas en `fleet.Vehicles`

## 8. Verificación de punta a punta del change

- [ ] 8.1 (Recortada por plazo; fuera del alcance aprobado) Prueba de integración con Customers, Workshops, Fleet y EventBus en proceso: cargar talleres, clientes, aprobadores y vehículos de `samples/import/` en ese orden y comprobar que Fleet tiene las 3 réplicas pobladas y 10 vehículos `Active`. Verificar con `dotnet test tests/Services/Fleet/RutaViva.Fleet.Api.IntegrationTests`
- [ ] 8.2 (Recortada por plazo; fuera del alcance aprobado) Desactivar `RVA104` dos veces y reactivarlo: exactamente dos `VehicleStatusChanged` en `eventbus.Events` (Inactive, Active). Verificar con la misma suite

## 9. Recortables

- [ ] 9.1 (Recortada por plazo; fuera del alcance aprobado) Endpoints de estado de Customers: `PATCH /api/v1/customers/{id}/status` y `PATCH /api/v1/customers/{id}/approvers/{approverId}/status`. Verificar con pruebas `WebApplicationFactory`: `204` y un solo `ApproverStatusChanged` al inactivar dos veces
- [ ] 9.2 (Recortada por plazo; fuera del alcance aprobado) Endpoint de estado de Workshops: `PATCH /api/v1/workshops/{id}/status`. Verificar con prueba `WebApplicationFactory`: `204` y un solo `WorkshopStatusChanged` al inactivar dos veces
