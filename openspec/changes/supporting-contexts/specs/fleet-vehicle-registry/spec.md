## Purpose

Registrar los vehículos de la flota con su dueño, su taller asignado, su responsable y su estado de activación, y propagar esos datos y sus cambios a los contextos que los necesitan (Maintenance, ServiceOrders, Reporting), validando contra réplicas locales sin llamadas HTTP entre servicios (ADR-0010, ADR-0020).

## ADDED Requirements

### Requirement: Réplicas locales de clientes, talleres y aprobadores
Fleet SHALL mantener réplicas locales de clientes, talleres y aprobadores a partir de `CustomerRegistered`, `CustomerStatusChanged`, `WorkshopRegistered`, `WorkshopStatusChanged`, `ApproverRegistered` y `ApproverStatusChanged`. Aplicar el mismo evento más de una vez MUST dejar la réplica igual que aplicarlo una vez.

#### Scenario: Cliente replicado
- **WHEN** Customers publica `CustomerRegistered` para el NIT `900000001`
- **THEN** Fleet puede registrar vehículos de ese cliente segundos después, sin consultar a Customers

#### Scenario: Evento reentregado
- **WHEN** el mismo `ApproverStatusChanged` se entrega dos veces
- **THEN** la réplica del aprobador queda con el estado del evento y no hay errores

### Requirement: Registro de vehículo validado contra las réplicas
Fleet SHALL registrar un vehículo con placa, VIN opcional, marca, modelo, año, dueño, taller, responsable opcional y odómetro inicial, en estado `Active`, y SHALL publicar `VehicleRegistered` en `fleet.vehicle`. El dueño y el taller MUST existir en las réplicas y estar `Active`; si no existen, la respuesta MUST ser `422` con `code = "Vehicle.OwnerNotFound"` o `"Vehicle.WorkshopNotFound"`, y si están inactivos, `"Vehicle.OwnerInactive"` o `"Vehicle.WorkshopInactive"`.

#### Scenario: Vehículo nuevo
- **WHEN** se envía `POST /api/v1/fleet/vehicles` con placa `RVA101`, VIN `9FB4SRC9AKM100101`, dueño y taller activos y odómetro 52.300
- **THEN** la respuesta es `201`, el vehículo queda `Active` y se publica `VehicleRegistered` con `Plate = RVA101` e `InitialOdometerKm = 52300`

#### Scenario: Dueño aún no replicado
- **WHEN** se registra un vehículo cuyo dueño no existe en la réplica
- **THEN** la respuesta es `422` con `code = "Vehicle.OwnerNotFound"` y no se publica ningún evento

#### Scenario: Taller inactivo
- **WHEN** se registra un vehículo con un taller inactivo
- **THEN** la respuesta es `422` con `code = "Vehicle.WorkshopInactive"`

### Requirement: Identificación del vehículo
La placa MUST normalizarse a mayúsculas sin espacios y MUST cumplir el formato `^[A-Z]{3}[0-9]{2}[0-9A-Z]$` (incluye motos como `EVA30D`). El VIN, si se informa, MUST tener 17 caracteres alfanuméricos sin `I`, `O` ni `Q`. La placa y el VIN MUST ser únicos: un duplicado MUST responder `409` con `code = "Vehicle.PlateAlreadyRegistered"` o `"Vehicle.VinAlreadyRegistered"`. El año MUST estar entre 1950 y el año actual más uno, y el odómetro inicial MUST ser mayor o igual a 0.

#### Scenario: Placa con guion
- **WHEN** se registra la placa `RV-107`
- **THEN** la respuesta es `400` con `code = "Vehicle.InvalidPlate"`

#### Scenario: Placa en minúsculas
- **WHEN** se registra la placa ` eva30d `
- **THEN** el vehículo queda con la placa `EVA30D`

#### Scenario: VIN corto
- **WHEN** se registra el VIN `9GAJM52348B`
- **THEN** la respuesta es `400` con `code = "Vehicle.InvalidVin"`

#### Scenario: Año fuera de rango
- **WHEN** se registra un vehículo del año 1890
- **THEN** la respuesta es `400` con el campo del año nombrado en `errors`

#### Scenario: Placa repetida
- **WHEN** se registra de nuevo la placa `RVA101`
- **THEN** la respuesta es `409` con `code = "Vehicle.PlateAlreadyRegistered"`

### Requirement: Responsable del vehículo
El responsable de un vehículo, si se asigna, MUST ser un aprobador `Active` de cualquier cliente registrado; puede pertenecer a un cliente distinto del dueño (A-11: el responsable aprueba además de los aprobadores del dueño). Fleet SHALL permitir asignarlo o quitarlo con `PUT /api/v1/fleet/vehicles/{id}/responsible` y SHALL publicar `VehicleResponsibleAssigned` solo si el responsable cambia.

#### Scenario: Responsable de otro cliente
- **WHEN** se asigna como responsable de un vehículo de `Transportes Andinos S.A.` a un aprobador activo de `RutaViva S.A.S.`
- **THEN** la respuesta es `204` y se publica `VehicleResponsibleAssigned` con ese `ResponsibleApproverId`

#### Scenario: Responsable inexistente
- **WHEN** se asigna un aprobador que no existe en la réplica
- **THEN** la respuesta es `422` con `code = "Vehicle.ResponsibleNotFound"`

#### Scenario: Responsable inactivo
- **WHEN** se asigna un aprobador inactivo
- **THEN** la respuesta es `422` con `code = "Vehicle.ResponsibleInactive"`

#### Scenario: Quitar el responsable
- **WHEN** se envía el responsable vacío para un vehículo que tenía uno
- **THEN** la respuesta es `204` y se publica `VehicleResponsibleAssigned` sin `ResponsibleApproverId`

#### Scenario: Reasignar el mismo responsable
- **WHEN** se asigna el responsable que el vehículo ya tiene
- **THEN** la respuesta es `204` y no se publica ningún evento

### Requirement: Activación y desactivación idempotentes
Fleet SHALL cambiar el estado de un vehículo con `PATCH /api/v1/fleet/vehicles/{id}/status` a `Active` o `Inactive`, registrando la fecha del cambio, y SHALL publicar `VehicleStatusChanged` con placa, dueño y nuevo estado. Pedir el estado actual MUST responder con éxito sin publicar (ADR-0020: las reentregas y los reintentos son seguros).

#### Scenario: Desactivar vehículo activo
- **WHEN** se desactiva el vehículo `RVA104`
- **THEN** la respuesta es `204` y se publica `VehicleStatusChanged` con `Status = Inactive`

#### Scenario: Desactivar vehículo ya inactivo
- **WHEN** se desactiva de nuevo `RVA104`
- **THEN** la respuesta es `204` y no se publica ningún evento

#### Scenario: Reactivar
- **WHEN** se reactiva `RVA104`
- **THEN** se publica `VehicleStatusChanged` con `Status = Active`

#### Scenario: Concurrencia
- **WHEN** dos usuarios cambian el estado del mismo vehículo a la vez
- **THEN** el segundo en guardar recibe `409` con `code = "Concurrency.Conflict"`

### Requirement: Consulta de vehículos
Fleet SHALL listar vehículos paginados con filtros opcionales por cliente, estado y texto (coincidencia parcial en placa o modelo) en `GET /api/v1/fleet/vehicles`, y SHALL devolver el detalle por identificador (`GET /api/v1/fleet/vehicles/{id}`) y por placa (`GET /api/v1/fleet/vehicles/by-plate/{plate}`), con el nombre del dueño, del taller y del responsable tomados de las réplicas.

#### Scenario: Filtro por cliente y estado
- **WHEN** se piden los vehículos activos de `RutaViva S.A.S.`
- **THEN** la respuesta contiene solo vehículos activos de ese cliente, con el total para la paginación

#### Scenario: Búsqueda por texto
- **WHEN** se busca `kangoo`
- **THEN** la respuesta contiene los vehículos cuyo modelo contiene `Kangoo`, sin distinguir mayúsculas

#### Scenario: Detalle por placa
- **WHEN** se pide `GET /api/v1/fleet/vehicles/by-plate/rva101`
- **THEN** la respuesta es el detalle de `RVA101` con el nombre del dueño, del taller y del responsable

#### Scenario: Placa inexistente
- **WHEN** se pide el detalle de la placa `ZZZ999`
- **THEN** la respuesta es `404`

### Requirement: Carga masiva de vehículos con simulación
Fleet SHALL aceptar `POST /api/v1/fleet/vehicles/bulk?dryRun=` con hasta 500 filas que identifican al dueño y al taller por NIT y al responsable por correo, y SHALL responder un resultado por fila con las mismas reglas del registro individual. Una placa ya registrada MUST resultar `Skipped`. Un correo de responsable que coincide con aprobadores de varios clientes MUST resultar `Invalid` con `code = "Vehicle.ResponsibleAmbiguous"`. Con `dryRun=true` MUST NOT persistir ni publicar.

#### Scenario: Vehículos de ejemplo
- **WHEN** se envían los 10 vehículos de `samples/import/vehiculos.csv` con `dryRun=false` después de cargar talleres, clientes y aprobadores
- **THEN** las 10 filas resultan `Valid` y se publican 10 `VehicleRegistered`

#### Scenario: Validación de negocio de los archivos con errores
- **WHEN** se envían en simulación las filas de `samples/import/con-errores/vehiculos.csv` después de la carga válida
- **THEN** `RVA105` y la primera `RVA106` resultan `Valid`; `TAN204` resulta `Invalid` con `Vehicle.OwnerNotFound`; `TAN205` con `Vehicle.WorkshopNotFound`; `EVA308` con `Vehicle.ResponsibleNotFound`; y `RVA101` resulta `Skipped`

#### Scenario: Errores de formato también rechazados por el dueño
- **WHEN** el mismo lote incluye `RV-107`, el año 1890, el odómetro −500, la marca vacía y el VIN de 11 caracteres
- **THEN** esas filas resultan `Invalid` con el código de cada regla, aunque DataImport ya las haya filtrado antes

#### Scenario: Placa repetida dentro del lote
- **WHEN** el lote trae dos veces `RVA106`
- **THEN** la primera resulta `Valid` y la segunda `Invalid` con `code = "Bulk.DuplicateInBatch"`

#### Scenario: Simulación sin efectos
- **WHEN** se envía un lote válido con `dryRun=true`
- **THEN** no se registra ningún vehículo y no se publica ningún evento
