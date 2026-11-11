## Purpose

Registrar y consultar a los clientes dueños de vehículos (incluida la propia RutaViva) y a sus aprobadores, que son quienes autorizan las cotizaciones de las órdenes de servicio (A-11).

## ADDED Requirements

### Requirement: Registro de cliente con NIT único
El servicio SHALL registrar un cliente con nombre, NIT, correo y teléfono opcional, en estado `Active`, y SHALL publicar `CustomerRegistered` en el stream `customers.customer`. El NIT MUST ser único: un segundo registro con el mismo NIT MUST responder `409` con `code = "Customer.TaxIdAlreadyRegistered"`.

#### Scenario: Cliente nuevo
- **WHEN** se envía `POST /api/v1/customers` con NIT `900000001`, nombre `RutaViva S.A.S.` y correo `flota@rutaviva.example`
- **THEN** la respuesta es `201` con el identificador del cliente, su estado es `Active` y se publica un `CustomerRegistered` con esos datos

#### Scenario: NIT repetido
- **WHEN** se registra un segundo cliente con NIT `900000001`
- **THEN** la respuesta es `409` con `code = "Customer.TaxIdAlreadyRegistered"` y no se publica ningún evento

#### Scenario: Datos inválidos
- **WHEN** se envía un cliente sin nombre o con un correo sin formato válido
- **THEN** la respuesta es `400` con el campo nombrado en `errors`

### Requirement: Aprobadores dentro del cliente
El servicio SHALL registrar aprobadores como parte de un cliente existente, con nombre completo y correo, en estado `Active`, y SHALL publicar `ApproverRegistered`. El correo de un aprobador MUST ser único dentro de su cliente.

#### Scenario: Aprobador nuevo
- **WHEN** se envía `POST /api/v1/customers/{id}/approvers` con `Laura Gómez` y `Laura_Gomez@rutaviva.com`
- **THEN** la respuesta es `201`, el detalle del cliente lista al aprobador como `Active` y se publica `ApproverRegistered` con el `CustomerId`

#### Scenario: Correo repetido en el mismo cliente
- **WHEN** se registra otro aprobador con `Laura_Gomez@rutaviva.com` en el mismo cliente
- **THEN** la respuesta es `409` con `code = "Customer.ApproverEmailAlreadyRegistered"`

#### Scenario: Cliente inexistente
- **WHEN** se registra un aprobador para un cliente que no existe
- **THEN** la respuesta es `404` con `code = "Customer.NotFound"`

### Requirement: Cambios de estado idempotentes (Recortable)
El servicio SHALL activar o inactivar un cliente (`PATCH /api/v1/customers/{id}/status`) y un aprobador (`PATCH /api/v1/customers/{id}/approvers/{approverId}/status`). Solo un cambio real MUST publicar `CustomerStatusChanged` o `ApproverStatusChanged`; pedir el estado actual MUST responder con éxito sin publicar.

#### Scenario: Inactivar aprobador
- **WHEN** se inactiva un aprobador activo
- **THEN** la respuesta es `204` y se publica `ApproverStatusChanged` con `Status = Inactive`

#### Scenario: Inactivar un aprobador ya inactivo
- **WHEN** se inactiva de nuevo el mismo aprobador
- **THEN** la respuesta es `204` y no se publica ningún evento

### Requirement: Consulta de clientes y aprobadores
El servicio SHALL listar los clientes con filtro opcional por estado (`GET /api/v1/customers?status=`) y SHALL devolver el detalle de un cliente con sus aprobadores (`GET /api/v1/customers/{id}`).

#### Scenario: Detalle con aprobadores
- **WHEN** se pide el detalle de `RutaViva S.A.S.` con 2 aprobadores registrados
- **THEN** la respuesta incluye los 2 aprobadores con su nombre, correo y estado

#### Scenario: Cliente inexistente
- **WHEN** se pide el detalle de un identificador que no existe
- **THEN** la respuesta es `404`

### Requirement: Carga masiva de clientes con simulación
El servicio SHALL aceptar `POST /api/v1/customers/bulk?dryRun=` con hasta 500 filas (NIT, nombre, correo, teléfono) y SHALL responder un resultado por fila con `outcome` `Valid`, `Invalid` o `Skipped` y sus errores. Una fila cuyo NIT ya existe MUST resultar `Skipped`. Con `dryRun=true` MUST NOT persistir nada ni publicar eventos; con `dryRun=false` MUST registrar solo las filas válidas, cada una de forma independiente.

#### Scenario: Simulación
- **WHEN** se envían los 3 clientes de `samples/import/clientes.csv` con `dryRun=true` sobre una base vacía
- **THEN** las 3 filas resultan `Valid` y no existe ningún cliente registrado

#### Scenario: Carga repetida
- **WHEN** se envían con `dryRun=false` los mismos 3 clientes ya registrados
- **THEN** las 3 filas resultan `Skipped` y no se publica ningún evento

#### Scenario: NIT repetido dentro del lote
- **WHEN** un lote contiene dos filas con el mismo NIT nuevo
- **THEN** la primera resulta `Valid` y la segunda `Invalid` con `code = "Bulk.DuplicateInBatch"`

#### Scenario: Lote demasiado grande
- **WHEN** se envía un lote de 501 filas
- **THEN** la respuesta es `400`

### Requirement: Carga masiva de aprobadores con simulación
El servicio SHALL aceptar `POST /api/v1/customers/approvers/bulk?dryRun=` con filas (NIT del cliente, nombre, correo). Una fila cuyo cliente no existe MUST resultar `Invalid` con `code = "Customer.NotFound"`, y una cuyo correo ya existe en ese cliente MUST resultar `Skipped`.

#### Scenario: Aprobadores de ejemplo
- **WHEN** se envían los 5 aprobadores de `samples/import/aprobadores.csv` con `dryRun=false` después de cargar los clientes
- **THEN** las 5 filas resultan `Valid`, quedan registradas y se publican 5 `ApproverRegistered`

#### Scenario: Cliente inexistente
- **WHEN** una fila trae el NIT de cliente `999999999`, que no existe
- **THEN** esa fila resulta `Invalid` con `code = "Customer.NotFound"`
