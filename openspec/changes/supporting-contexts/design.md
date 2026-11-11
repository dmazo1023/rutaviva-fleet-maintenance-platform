## Context

Motivación en `proposal.md`; requisitos en `specs/`. Este change construye tres servicios de soporte sobre la base de `foundation-platform` (BuildingBlocks, Contracts, EventBus) y usa sin redefinir su registro de contratos: streams (`foundation-platform/design.md#streams`), eventos (`#eventos-de-integración`), endpoints (`#endpoints-públicos`) y el contrato del lote masivo (`#contrato-del-lote-masivo-bulkdryrun`).

Los tres servicios son **soporte**: modelo pequeño, sin sagas ni políticas complejas, pero con sus invariantes dentro de los agregados (ADR-0010 POS-004). El riesgo principal no es la complejidad, sino repetir el mismo código tres veces.

## Goals / Non-Goals

**Goals:**
- Agregados con comportamiento: registrar, agregar aprobador, activar, inactivar y asignar responsable son métodos del agregado que protegen invariantes y emiten eventos de dominio.
- Validación de referencias externas en Fleet solo con réplicas locales.
- Un único camino de registro por entidad: el endpoint individual y la fila de la carga masiva ejecutan el mismo command.

**Non-Goals:**
- Edición de datos descriptivos, reasignación de taller y cascadas por inactivar clientes o talleres (ver `proposal.md`, Fuera de alcance).

## Decisions

### D-01 Modelo por servicio

| Servicio | Raíz | Entidades / VOs | Métodos de negocio | Eventos de dominio |
|---|---|---|---|---|
| Customers | `Customer` (`IHasRowVersion`) | `Approver` (entidad interna), `CustomerId`, `ApproverId`, `TaxId`, `EmailAddress`, `PhoneNumber`, `PersonName` | `Register(...)`, `AddApprover(name, email)`, `Activate()`, `Deactivate()`, `ActivateApprover(id)`, `DeactivateApprover(id)` | `CustomerRegistered`, `CustomerStatusChanged`, `ApproverRegistered`, `ApproverStatusChanged` |
| Workshops | `Workshop` (`IHasRowVersion`) | `WorkshopId`, `TaxId`, `EmailAddress`, `PhoneNumber`, `PostalAddress` (dirección y ciudad) | `Register(...)`, `Activate()`, `Deactivate()` | `WorkshopRegistered`, `WorkshopStatusChanged` |
| Fleet | `Vehicle` (`IHasRowVersion`) | `VehicleId`, `Plate`, `Vin`, `VehicleDescription` (marca, modelo, año), `Kilometers` | `Register(..., CustomerReference owner, WorkshopReference workshop, ApproverReference? responsible, DateTimeOffset now)`, `AssignResponsible(ApproverReference?)`, `Deactivate(now)`, `Reactivate(now)` | `VehicleRegistered`, `VehicleStatusChanged`, `VehicleResponsibleAssigned` |

- `TaxId`, `EmailAddress` y `PhoneNumber` se repiten por servicio **a propósito**: ADR-0010 prohíbe compartir tipos de dominio. Son VOs de 15 a 30 líneas cuyo formato puede divergir (un taller puede tener reglas de NIT distintas en el futuro). La duplicación es de forma, no de conocimiento de negocio compartido.
- Enums con valores desde 1 (data-model E-01): `CustomerStatus`, `ApproverStatus`, `WorkshopStatus`, `VehicleStatus` (`Active = 1`, `Inactive = 2`).
- `Activate`/`Deactivate`/`Reactivate` son **idempotentes en el dominio**: si el estado ya es el pedido, devuelven `Result.Success()` sin emitir evento (ADR-0020). La regla vive en el agregado, no en el handler.
- `Customer.AddApprover` verifica el correo único dentro del cliente (invariante del agregado, reforzada por el índice `UX_Approvers_CustomerId_Email`).
- Unicidad global (NIT, placa, VIN) no es invariante de un agregado: la comprueba Application con un puerto de consulta (`ICustomerUniquenessChecker.IsTaxIdTaken`, etc.) y la garantiza el índice único; la violación del índice (2601/2627) se traduce al mismo `Error` de catálogo (doble red, igual que ADR-0011 IMP-003).

### D-02 Responsable: aprobador activo de cualquier cliente

A-11 dice que aprueba «el dueño (por cualquiera de sus aprobadores activos) **o** el responsable asignado». Si el responsable tuviera que pertenecer al dueño, el «o» sería redundante. Por eso el responsable puede ser un aprobador de **cualquier** cliente (por ejemplo, un coordinador de RutaViva responsable de vehículos de un tercero). La regla de Fleet es solo «existe y está activo». Alternativa descartada: restringirlo al dueño, que vacía de sentido A-11.

### D-03 Réplicas de Fleet

| Entidad (Fleet.Domain, solo lectura) | Columnas | Alimentada por |
|---|---|---|
| `CustomerReference` | `CustomerId` PK, `TaxId` (índice único), `Name`, `Status`, `UpdatedAt` | `CustomerRegistered`, `CustomerStatusChanged` |
| `WorkshopReference` | `WorkshopId` PK, `TaxId` (índice único), `Name`, `Status`, `UpdatedAt` | `WorkshopRegistered`, `WorkshopStatusChanged` |
| `ApproverReference` | `ApproverId` PK, `CustomerId`, `FullName`, `Email` (índice no único), `Status`, `UpdatedAt` | `ApproverRegistered`, `ApproverStatusChanged` |

- `TaxId` y `Email` se agregan respecto de `data-model.md` para resolver las claves naturales de la carga masiva. La sincronización del documento la hace la tarea 8.2 de `foundation-platform` (fuera de este change): se reporta como hallazgo.
- Las réplicas exponen solo consultas (`IsActive`) para el dominio. Las actualiza únicamente su handler (ADR-0010 IMP-003) con *upsert* idempotente: el evento lleva el estado completo, así que aplicarlo dos veces da el mismo resultado. El orden global por offset del EventBus garantiza que un `*StatusChanged` nunca se aplica antes que su `*Registered`; si aun así llega un cambio de estado de una entidad desconocida, se ignora con el log `ReferenceUpdateIgnored`.
- `StatusChanged` de clientes y talleres no traen el nombre: el handler actualiza solo `Status` y `UpdatedAt`.

### D-04 Commands, queries y puertos (Application)

| Servicio | Commands | Queries (puerto `I{Service}Queries`) | Puertos de escritura |
|---|---|---|---|
| Customers | `RegisterCustomerCommand`, `AddApproverCommand`, `ChangeCustomerStatusCommand`, `ChangeApproverStatusCommand` | `ListCustomersQuery(status?)`, `GetCustomerDetailQuery(id)` | `ICustomerRepository` (`GetByIdAsync`, `GetByTaxIdAsync`, `Add`), `ICustomerUniquenessChecker` |
| Workshops | `RegisterWorkshopCommand`, `ChangeWorkshopStatusCommand` | `ListWorkshopsQuery(status?, city?)`, `GetWorkshopDetailQuery(id)` | `IWorkshopRepository`, `IWorkshopUniquenessChecker` |
| Fleet | `RegisterVehicleCommand`, `AssignResponsibleCommand`, `ChangeVehicleStatusCommand` | `ListVehiclesQuery(customerId?, status?, search?, page, pageSize)`, `GetVehicleDetailQuery(id)`, `GetVehicleByPlateQuery(plate)` | `IVehicleRepository`, `IVehicleUniquenessChecker`, `IFleetReferenceReader` (`FindCustomer(id)`, `FindCustomerByTaxId`, `FindWorkshop(id)`, `FindWorkshopByTaxId`, `FindApprover(id)`, `FindApproversByEmail`), `IFleetReferenceWriter` (*upserts*) |

- `ChangeVehicleStatusCommand(VehicleId, VehicleStatus target)` delega en `Deactivate` o `Reactivate` mediante un diccionario `target → acción` del agregado (sin `if/else` por estado).
- El detalle de vehículo es una proyección `AsNoTracking` que une `Vehicles` con las réplicas **dentro del schema `fleet`** (permitido: es el mismo servicio).
- Paginación: `page` desde 1, `pageSize` por defecto 50 y máximo 200 (constantes `VehicleListDefaults`).

### D-05 Carga masiva: un solo camino de registro

El endpoint individual y cada fila del lote ejecutan el **mismo** `Register*Command`. La carga masiva agrega solo tres cosas: traducción de claves naturales, detección de duplicados dentro del lote y el modo simulación.

- **Template Method** `BulkRegistrationHandler<TRow, TCommand>` (Application de cada servicio, o de BuildingBlocks si se acepta el contrato nuevo C-N1): para cada fila → `ResolveCommand(row)` (abstracto: traduce NIT/correo a IDs con el lector de réplicas) → `NaturalKey(row)` (abstracto) → si la clave ya existe en BD → `Skipped`; si ya apareció en el lote → `Invalid Bulk.DuplicateInBatch`; si no → ejecutar el command.
- **Simulación**: `IBulkExecutionScope` decide si se confirma. En `dryRun=true` el handler del command se ejecuta contra un `IUnitOfWork` que **no** guarda (`DryRunUnitOfWork`, patrón **Null Object**); las validaciones de dominio y de unicidad corren igual, y los eventos nunca llegan al Outbox porque no hay `SaveChanges`. Con `dryRun=false` cada fila se confirma en su propia transacción (un agregado por transacción, Vernon).
- Los códigos de error por fila son los mismos `Error` de catálogo del registro individual: la UI de DataImport muestra el mismo mensaje en ambos caminos.
- Filas de aprobadores: el agregado es `Customer`; la fila resuelve el cliente por NIT y ejecuta `AddApproverCommand`. Clave natural: `(NIT del cliente, correo)`.
- Filas de vehículos: resuelven `nit_dueno → CustomerId`, `nit_taller → WorkshopId` y `correo_responsable → ApproverId` (0 coincidencias → `Vehicle.ResponsibleNotFound`; más de 1 → `Vehicle.ResponsibleAmbiguous`).
- Tope de 500 filas validado en el request (`400`).

### D-06 Integración

| Servicio | Produce (stream) | Mapper de dominio → integración | Consume |
|---|---|---|---|
| Customers | `customers.customer` | `CustomerIntegrationEventMapper` (4 eventos) | — |
| Workshops | `workshops.workshop` | `WorkshopIntegrationEventMapper` (2 eventos) | — |
| Fleet | `fleet.vehicle` | `VehicleIntegrationEventMapper` (3 eventos) | `customers.customer`, `workshops.workshop` con 6 handlers `IIntegrationEventHandler<T>` registrados con `AddEventBusSubscriber("fleet", StartPosition.Checkpoint)` |

Semántica: cada método del agregado emite **como máximo un** evento de dominio, y solo si hubo cambio. El mapper produce exactamente un evento de integración por evento de dominio. `VehicleStatusChanged` incluye `Plate` y `CustomerId` para que Maintenance, ServiceOrders y Notifications no necesiten otra consulta. Fleet no publica `VehicleWorkshopAssigned` en este change: no existe el caso de uso (ver Contratos nuevos requeridos).

### D-07 API

Un `IEndpointModule` por recurso (`CustomerEndpoints`, `ApproverEndpoints`, `WorkshopEndpoints`, `VehicleEndpoints`, `BulkEndpoints` por servicio). Los endpoints solo traducen request → command y `Result` → HTTP con `ToHttpResult()`. `POST` responde `201` con `Location`; cambios de estado y de responsable responden `204`. Validación de entrada con FluentValidation por request (el decorador de foundation la ejecuta).

### D-08 Persistencia

Un `DbContext` por servicio (`CustomersDbContext`, `WorkshopsDbContext`, `FleetDbContext`) con `HasDefaultSchema`, `ApplyRutaVivaConventions()`, Outbox y, en Fleet, `EventCheckpoints`. Índices: `UX_Customers_TaxId`, `UX_Approvers_CustomerId_Email`, `UX_Workshops_TaxId`, `UX_Vehicles_Plate`, `UX_Vehicles_Vin` (filtrado), `IX_Vehicles_CustomerId_Status`, `UX_CustomerReferences_TaxId`, `UX_WorkshopReferences_TaxId`, `IX_ApproverReferences_Email`. `CHECK` de estados con los nombres de los enums.

## Patrones aplicados

| Patrón | Dónde | Por qué |
|---|---|---|
| Aggregate, Value Object, Domain Event | D-01 | Invariantes en el modelo, no en los handlers |
| Event-Carried State Transfer | D-03 | Fleet valida sin HTTP a Customers ni Workshops (ADR-0010 ALT-003) |
| Template Method | D-05 `BulkRegistrationHandler` | Cuatro cargas masivas con el mismo algoritmo y pasos variables |
| Null Object | D-05 `DryRunUnitOfWork` | Simulación sin `if (dryRun)` repartidos por los handlers |
| Repository, Specification ligera (consultas de unicidad) | D-04 | Dominio libre de EF Core |
| Strategy por diccionario | D-04 cambio de estado | Sin `if/else` por estado |

## Antipatrones evitados

| Antipatrón | Cómo se evita |
|---|---|
| Modelo anémico | Estados, responsable y aprobadores cambian solo por métodos del agregado que validan y emiten eventos; sin *setters* públicos (prueba de arquitectura de foundation) |
| Copy-paste entre servicios | Outbox, checkpoints, despacho, mapeo HTTP, convenciones EF y decoradores vienen de BuildingBlocks; el algoritmo de carga masiva es un Template Method; solo se repiten VOs pequeños por la regla de ADR-0010 |
| Dos caminos de registro | Individual y masivo usan el mismo command |
| Entity services | Approver vive dentro de Customer, no es un servicio (ADR-0010 ALT-001) |
| Números mágicos | Rangos (año 1950, 500 filas, 200 por página) como constantes con nombre en el VO o en `*Defaults` |
| Ifs anidados | Guard clauses que devuelven `Error` del catálogo |

## Contratos nuevos requeridos

| # | Contrato | Dónde | Motivo |
|---|---|---|---|
| C-N1 | `BulkRegistrationHandler<TRow, TCommand>`, `IBulkExecutionScope` y `DryRunUnitOfWork` | `RutaViva.BuildingBlocks.Application` (`foundation-platform`) | Maintenance (`routines/bulk`) repite el mismo algoritmo; en BuildingBlocks evita un quinto copy-paste. Si no se acepta, cada servicio lo implementa en su Application y se acepta la duplicación |
| C-N2 | Código de error `Bulk.DuplicateInBatch` | `RutaViva.IntegrationContracts` o `BuildingBlocks.Application` | Lo usan los 5 endpoints masivos y DataImport lo muestra |
| C-N3 | `VehicleWorkshopAssigned` no tiene productor | Registro de `foundation-platform` | Ningún caso de uso reasigna el taller (reto: fuera de alcance). **Resuelto por el orquestador:** se quitó del registro y de sus consumidores (YAGNI; elegir el taller está fuera del alcance del reto) |

## Risks / Trade-offs

- [Ventana de propagación: un vehículo registrado justo después de su cliente recibe `422`] → Esperado (ADR-0010 NEG-002); DataImport reintenta la simulación hasta 10 s (ADR-0022) y el CRM muestra el `ProblemDetails`.
- [Correo de responsable ambiguo entre clientes] → Error explícito `Vehicle.ResponsibleAmbiguous` en la carga masiva; el endpoint individual usa IDs y no tiene el problema.
- [Un aprobador inactivado sigue como responsable de sus vehículos] → Se acepta: la autorización de ServiceOrders exige aprobador activo (ADR-0003, elemento 5), así que no puede aprobar.
- [Réplica desactualizada (estado inactivo aún no propagado)] → Se acepta (ADR-0011 NEG-003).

## Migration Plan

No aplica (*greenfield*). Migración inicial por servicio aplicada con el usuario administrativo (ADR-0013).

## Open Questions

(ninguna que cambie el alcance)
