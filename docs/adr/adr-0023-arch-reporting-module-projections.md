---
title: "[ADR-0023] Crear un servicio Reporting con proyecciones de lectura alimentadas por el EventBus y reportes descargables en Excel y CSV"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "architecture"
  - "reporting"
  - "cqrs"
  - "read-model"
  - "event-bus"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: ""
---

## Contexto

- El equipo considera **sumamente importante** un **módulo aparte de reportería**, con reportes **descargables**.
- Los datos de un reporte típico cruzan varios servicios. Por ejemplo, «costos por cliente y taller» necesita las órdenes (ServiceOrders), los nombres de clientes (Customers), los de talleres (Workshops) y las placas (Fleet).
- Cada servicio es dueño exclusivo de su schema y no hay `JOIN` entre schemas (ADR-0013).
- Todos los cambios relevantes se publican como eventos en el EventBus, que conserva el log completo (ADR-0024).
- Los usuarios del negocio trabajan hoy con Excel.
- Las consultas de reportes (agregaciones por período) no deben afectar el rendimiento de las operaciones transaccionales.

## Decisión

Crearemos el servicio **`Reporting`** (schema `reporting`) como un **read model de CQRS a nivel de sistema**:

1. **Proyecciones**: un suscriptor idempotente (checkpoint `reporting.EventCheckpoints`) de los streams `customers.customer`, `workshops.workshop`, `fleet.vehicle`, `maintenance.routine`, `maintenance.alert` y `serviceorders.service-order` mantiene **tablas desnormalizadas y optimizadas para consulta**: `VehicleRoutineStatus`, `ServiceOrderFacts`, `AlertFacts` y las dimensiones `Customers`, `Workshops` y `Vehicles`.
2. **Catálogo de reportes**:

| Código | Reporte | Filtros | Contenido clave |
|---|---|---|---|
| R-01 | Estado de mantenimiento de la flota | cliente, estado del vehículo, estado de vencimiento | placa, rutina, estado, km y fecha del próximo mantenimiento, % consumido |
| R-02 | Órdenes de servicio | período, estado, taller, cliente, origen | número, placa, rutina, estado, fechas de cada fase y tiempos de ciclo (creación → aprobación → en sitio → finalizada) |
| R-03 | Costos de mantenimiento | período, cliente, taller, vehículo | total por rutina y por adicionales, aprobadas automáticamente frente a aprobadas por el cliente |
| R-04 | Alertas y tiempos de atención | período, cliente, tipo, origen (km o tiempo) | alertas generadas, atendidas y resueltas; tiempo medio hasta la atención y hasta la resolución |

3. **Formatos descargables**: **Excel `.xlsx`** (principal, con encabezados, formato de moneda y fecha, filtros automáticos y una hoja «Parámetros» con los filtros usados y la fecha de generación) y **CSV** (UTF-8 con BOM, separador `;`).
4. **Pantalla de reportes** en el CRM: filtros, vista previa paginada y botones de descarga.
5. Patrón **Bridge**: la abstracción `IReportDefinition` (qué datos) es independiente de la implementación `IReportWriter` (`XlsxReportWriter`, `CsvReportWriter`). Hay 4 reportes × 2 formatos con 6 clases, no 8.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Los reportes cruzan 4 servicios sin `JOIN` entre schemas ni llamadas HTTP: cada reporte es una consulta a tablas locales | Positive |
| POS-002 | Generar reportes no carga las bases transaccionales: las proyecciones están en su propio schema | Positive |
| POS-003 | Las proyecciones son **reconstruibles**: se vacían las tablas, se reinicia el checkpoint a 0 y se rehacen desde el log del EventBus | Positive |
| POS-004 | Agregar un reporte implica 1 definición y 0 cambios en los formatos. Agregar un formato (por ejemplo PDF) implica 1 *writer* y 0 cambios en los reportes | Positive |
| NEG-001 | Los reportes son eventualmente consistentes: reflejan los eventos procesados, con un retraso típico de menos de 1 s | Negative |
| NEG-002 | Los eventos de integración deben **llevar los datos que los reportes necesitan** (montos, fechas de cada fase, `CustomerId`, `WorkshopId`). Esto amplía algunos contratos de integración (ADR-0024) | Negative |
| NEG-003 | Para reconstruir las proyecciones, el log del EventBus debe conservar todo el historial. Hoy no se borra nada; una retención futura limitaría la reconstrucción (ADR-0024, NEG-005) | Negative |
| NEG-004 | Unas 5 a 6 horas de esfuerzo en el plazo | Negative |

## Alternativas consideradas

### ALT-001 — Vistas SQL entre schemas con un usuario de solo lectura

**Descripción**: `CREATE VIEW reporting.vw_ServiceOrderCosts AS SELECT ... FROM serviceorders.ServiceOrders JOIN fleet.Vehicles ...`.

**Rejection rationale**: Es la opción más rápida de construir, pero acopla los reportes a las tablas internas de cada servicio (*shared database integration*). Cualquier migración de un servicio rompería los reportes, y además contradice el aislamiento por schema de ADR-0013.

### ALT-002 — Composición de APIs (Reporting consulta cada servicio por HTTP)

**Descripción**: Pedir los datos a cada servicio y combinarlos en memoria.

**Rejection rationale**: Las agregaciones por período sobre miles de órdenes requerirían paginar varias APIs y cruzar en memoria (N+1). Además, acopla temporalmente los reportes a la disponibilidad de cada servicio.

### ALT-003 — Herramienta de BI externa (Power BI)

**Descripción**: Conectar Power BI a las bases.

**Rejection rationale**: Requiere licencias y un entorno externo, no forma parte del código evaluado y tendría el mismo acoplamiento que ALT-001.

### ALT-004 — PDF con QuestPDF

**Descripción**: Reportes en PDF.

**Rejection rationale**: Los usuarios trabajan en Excel y necesitan filtrar y reprocesar los datos. Además, la licencia Community de QuestPDF tiene restricciones por ingresos de la empresa. Queda como un *writer* futuro (POS-004).

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Generación de `.xlsx` con **ClosedXML** (MIT), encapsulada en `XlsxReportWriter`. Application no conoce ClosedXML |
| IMP-002 | Endpoints: `GET /api/v1/reports` (catálogo con filtros disponibles), `GET /api/v1/reports/{code}?…&page=` (vista previa en JSON) y `GET /api/v1/reports/{code}/download?format=xlsx|csv&…` (`Content-Disposition: attachment; filename="R-02_ordenes_2026-11-12.xlsx"`) |
| IMP-003 | La descarga es síncrona y escribe directamente en la respuesta, con un límite de 50.000 filas. Por encima de ese límite, la evolución es un job asíncrono con enlace de descarga (YAGNI hoy) |
| IMP-004 | Las proyecciones se actualizan con **upsert** por la clave del agregado, y los eventos se ignoran si su versión o fecha es anterior a la registrada (eventos fuera de orden) |
| IMP-005 | Los contratos que se amplían son `ServiceOrderStatusChanged` (con `TotalAmount`, `AdditionalAmount`, `WorkshopId`, `CustomerId`, `Plate` y `OccurredAt`) y `AlertRaised` / `AlertStatusChanged` (con `CustomerId` y `Plate`) |
| IMP-006 | Enums nuevos: `ReportCode` (`FleetMaintenanceStatus`, `ServiceOrders`, `MaintenanceCosts`, `AlertsAttention`) y `ReportFormat` (`Xlsx`, `Csv`) |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Fowler, M. *CQRS* (martinfowler.com) |
| REF-002 | Gamma et al. (1994). *Design Patterns*: Bridge (estructural) |
| REF-003 | adr-0024-int-eventbus-signalr-sql-log · adr-0013-data-sqlserver-schema-per-service |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0023` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
