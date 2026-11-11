## Purpose

Mantener el catálogo de talleres externos que reciben las órdenes de servicio, para que cada vehículo tenga asignado un taller válido (el reto excluye elegir o agendar el taller).

## ADDED Requirements

### Requirement: Registro de taller con NIT único
El servicio SHALL registrar un taller con nombre, NIT, correo, teléfono opcional, dirección y ciudad, en estado `Active`, y SHALL publicar `WorkshopRegistered` en el stream `workshops.workshop`. Un NIT repetido MUST responder `409` con `code = "Workshop.TaxIdAlreadyRegistered"`.

#### Scenario: Taller nuevo
- **WHEN** se envía `POST /api/v1/workshops` con NIT `900100001`, nombre `Autofrenos Medellín` y ciudad `Medellín`
- **THEN** la respuesta es `201`, el taller queda `Active` y se publica `WorkshopRegistered` con NIT, nombre y ciudad

#### Scenario: NIT repetido
- **WHEN** se registra otro taller con NIT `900100001`
- **THEN** la respuesta es `409` con `code = "Workshop.TaxIdAlreadyRegistered"`

#### Scenario: Dirección vacía
- **WHEN** se envía un taller sin dirección
- **THEN** la respuesta es `400` con el campo nombrado en `errors`

### Requirement: Cambio de estado idempotente (Recortable)
El servicio SHALL activar o inactivar un taller con `PATCH /api/v1/workshops/{id}/status`. Solo un cambio real MUST publicar `WorkshopStatusChanged`.

#### Scenario: Inactivar taller activo
- **WHEN** se inactiva un taller activo
- **THEN** la respuesta es `204` y se publica `WorkshopStatusChanged` con `Status = Inactive`

#### Scenario: Repetir el estado actual
- **WHEN** se inactiva un taller ya inactivo
- **THEN** la respuesta es `204` y no se publica ningún evento

### Requirement: Consulta de talleres
El servicio SHALL listar los talleres con filtro opcional por estado y ciudad (`GET /api/v1/workshops?status=&city=`) y SHALL devolver el detalle de uno (`GET /api/v1/workshops/{id}`), con `404` si no existe.

#### Scenario: Filtro por ciudad
- **WHEN** se piden los talleres de `Medellín` con los 3 talleres de ejemplo cargados
- **THEN** la respuesta contiene solo los talleres cuya ciudad es `Medellín`

### Requirement: Carga masiva de talleres con simulación
El servicio SHALL aceptar `POST /api/v1/workshops/bulk?dryRun=` con hasta 500 filas y SHALL responder un resultado por fila. Una fila cuyo NIT ya existe MUST resultar `Skipped`; con `dryRun=true` MUST NOT persistir ni publicar.

#### Scenario: Talleres de ejemplo
- **WHEN** se envían los 3 talleres de `samples/import/talleres.csv` con `dryRun=false` sobre una base vacía
- **THEN** las 3 filas resultan `Valid`, quedan registradas y se publican 3 `WorkshopRegistered`

#### Scenario: Carga repetida
- **WHEN** se envían de nuevo los mismos 3 talleres
- **THEN** las 3 filas resultan `Skipped`
