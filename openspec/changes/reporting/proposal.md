## Why

El jefe de flota y el equipo de RutaViva necesitan reportes descargables del estado de mantenimiento, de las órdenes de servicio, de los costos y de la atención de alertas (RQ-09). Los datos viven repartidos en cinco schemas sin `JOIN` entre ellos (ADR-0013, data-model P-01), así que consultar los servicios de negocio para cada reporte acoplaría la lectura a sus modelos y a su disponibilidad. Un *read model* alimentado por el EventBus (ADR-0023, ADR-0024) resuelve los reportes con consultas sobre una sola tabla y sin tocar los agregados.

Corresponde al corte **S4c** del análisis (§10). R-01 y R-02 en `.xlsx` son **imprescindibles para la demo**.

## What Changes

- Servicio **`Reporting`** (`RutaViva.Reporting.{Application, Infrastructure, Api}`), sin proyecto `Domain`: es un *read model* de CQRS a nivel de sistema, sin invariantes de negocio que proteger (análisis §9, ADR-0023).
- Suscriptor `reporting` del EventBus, desde su checkpoint, sobre los 6 streams del registro de contratos (`customers.customer`, `workshops.workshop`, `fleet.vehicle`, `maintenance.routine`, `maintenance.alert`, `serviceorders.service-order`).
- Proyecciones desnormalizadas en el schema `reporting`: dimensiones `Customers`, `Workshops` y `Vehicles`, y hechos `VehicleRoutineStatus`, `ServiceOrderFacts` (fechas de cada fase) y `AlertFacts`. Actualización por *upsert* con descarte de eventos obsoletos y sin `CHECK` de enums (*tolerant reader*, data-model D-18, D-19).
- Reconstrucción completa de las proyecciones reiniciando el checkpoint a 0 (ADR-0024 POS-005).
- Catálogo de reportes R-01 a R-04 con filtros, vista previa paginada en JSON y descarga síncrona hasta 50.000 filas.
- Formatos: **Excel `.xlsx`** (encabezados, formato de moneda, fecha y porcentaje, filtros automáticos, hoja «Parámetros») y **CSV** (UTF-8 con BOM, separador `;`).
- Patrón **Bridge**: 4 definiciones de reporte × 2 escritores de formato = 6 clases.

## Capabilities

### New Capabilities

- `reporting-projections`: mantenimiento de las proyecciones de lectura a partir de los eventos de integración, idempotente, tolerante al orden y reconstruible.
- `operational-reports`: catálogo, filtros, vista previa paginada y descarga en `.xlsx` y `.csv` de los reportes R-01 a R-04.

### Modified Capabilities

(ninguna)

## Impact

- Código nuevo: `src/Services/Reporting/*`, `tests/Services/Reporting/*`.
- Base de datos: tablas del schema `reporting` creadas por migración EF Core (más `reporting.EventCheckpoints` de BuildingBlocks; se descarta `reporting.InboxMessages` del script de referencia, ADR-0024 §5).
- Dependencias NuGet: ClosedXML (MIT), solo en Infrastructure.
- Depende de `foundation-platform` (BuildingBlocks, Contracts, cliente del EventBus). Lo consumen `edge-gateway-notifications` (ruta `/api/v1/reports/**`) y `fleet-maintenance-ui` (pantalla de reportes).

## Fuera de alcance

- Reportes asíncronos con enlace de descarga para más de 50.000 filas (ADR-0023 IMP-003, YAGNI).
- Reportes programados o enviados por correo.
- Gráficos dentro del `.xlsx`.
- Edición de datos desde Reporting: es de solo lectura.

**Recortable** (orden del análisis §10): R-03 y R-04 (quedan R-01 y R-02), y luego el formato CSV (queda `.xlsx`).
