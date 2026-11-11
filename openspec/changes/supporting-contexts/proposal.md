## Why

Los dominios core (Maintenance y ServiceOrders) necesitan saber quién es el dueño de cada vehículo, qué taller tiene asignado, quién puede aprobar y si el vehículo está activo (A-11, RN-3, ADR-0020). Esos datos tienen dueños propios: Customers, Workshops y Fleet (ADR-0010). Sin ellos no hay carga inicial (ADR-0022, RQ-08), ni autorización de aprobaciones, ni suspensión en cascada (RQ-05). Son subdominios de soporte: deliberadamente delgados, pero con sus invariantes dentro del modelo (ADR-0010 POS-004).

Corresponde al corte **S3** del análisis (§10).

## What Changes

- Servicio **Customers** (schema `customers`): agregado `Customer` con `Approver` interno; NIT único, correo de aprobador único dentro del cliente, estados `Active`/`Inactive` idempotentes. Publica en `customers.customer` (A-11: RutaViva también es un `Customer`).
- Servicio **Workshops** (schema `workshops`): agregado `Workshop` con NIT único y estado. Publica en `workshops.workshop`.
- Servicio **Fleet** (schema `fleet`): agregado `Vehicle` con placa colombiana, VIN opcional, dueño, taller, responsable opcional y estado `Active`/`Inactive` con `Deactivate()`/`Reactivate()` idempotentes (ADR-0020). Publica en `fleet.vehicle`.
- Réplicas locales de Fleet (`CustomerReferences`, `WorkshopReferences`, `ApproverReferences`) alimentadas por eventos (*event-carried state transfer*, ADR-0010). Registrar un vehículo cuyo dueño, taller o responsable no está en la réplica responde `422` (ADR-0010 NEG-002).
- Consultas de listado y detalle que necesita el CRM: vehículos por cliente, estado y texto (placa o modelo), y detalle por placa para la ruta `/flota/:plate` del mockup.
- Endpoints masivos `bulk?dryRun=` para talleres, clientes, aprobadores y vehículos, con resultado por fila `Valid`/`Invalid`/`Skipped` y `Skipped` por clave natural (ADR-0022 IMP-004), que cubren los escenarios de negocio de `samples/README.md` §2.

## Capabilities

### New Capabilities

- `customer-management`: registro y consulta de clientes y sus aprobadores, cambios de estado y carga masiva con simulación.
- `workshop-management`: registro y consulta de talleres, cambio de estado y carga masiva con simulación.
- `fleet-vehicle-registry`: registro y consulta de vehículos, asignación de responsable, activación y desactivación, réplicas de clientes, talleres y aprobadores, y carga masiva con simulación.

### Modified Capabilities

(ninguna)

## Impact

- Código nuevo: `src/Services/{Customers,Workshops,Fleet}/RutaViva.{Service}.{Domain,Application,Infrastructure,Api}` y sus pruebas en `tests/Services/{Service}/`.
- Base de datos: schemas `customers`, `workshops` y `fleet` con migraciones EF Core. Las réplicas de Fleet agregan `TaxId` (clientes y talleres) y `Email` (aprobadores) respecto del modelo de datos, para resolver las claves naturales de la carga masiva.
- Eventos: produce los de `customers.customer`, `workshops.workshop` y `fleet.vehicle` del registro de contratos de `foundation-platform`.
- Depende de `foundation-platform`. Lo consumen `maintenance-core`, `service-orders-core`, `data-import`, `reporting`, `edge-gateway-notifications` y `fleet-maintenance-ui`.

## Fuera de alcance

- Edición de los datos descriptivos (nombre, correo, teléfono, dirección) de clientes, talleres y vehículos: ningún flujo del reto la requiere (YAGNI).
- Reasignación del taller de un vehículo (el reto excluye elegir o agendar taller).
- Efectos en cascada de inactivar un cliente o un taller sobre vehículos u órdenes existentes.
- Autenticación (fuera del reto).
- Fusión de Workshops dentro de Fleet: es el último elemento del orden de recorte del análisis §10 y no se planifica.

**Recortable** (orden del análisis §10): los endpoints de cambio de estado de clientes, aprobadores y talleres; no participan en la demo.
