## Purpose

Ofrecer al usuario del CRM un catálogo de reportes operativos de mantenimiento con filtros, vista previa paginada y descarga en Excel y CSV (RQ-09, ADR-0023).

## ADDED Requirements

### Requirement: Catálogo de reportes
`GET /api/v1/reports` SHALL devolver los reportes disponibles con su código (`FleetMaintenanceStatus`, `ServiceOrders`, `MaintenanceCosts`, `AlertsAttention`), su identificador visible (R-01 a R-04), su título en español, sus filtros (nombre, tipo, si es obligatorio y valores permitidos cuando aplica) y los formatos de descarga (`Xlsx`, `Csv`).

#### Scenario: Consulta del catálogo
- **WHEN** el CRM pide el catálogo
- **THEN** recibe los reportes habilitados con sus filtros y formatos, y R-01 declara los filtros cliente, estado del vehículo y estado de vencimiento

### Requirement: Validación de filtros
Cada reporte SHALL validar sus filtros antes de consultar. Un período con fecha inicial posterior a la final, un valor de enumeración no permitido o un filtro desconocido MUST responder `400` con `ProblemDetails`. Un código de reporte inexistente MUST responder `404`.

#### Scenario: Período invertido
- **WHEN** se pide R-02 con `from = 2026-11-30` y `to = 2026-11-01`
- **THEN** la respuesta es `400` y nombra el filtro `from`

#### Scenario: Reporte inexistente
- **WHEN** se pide `GET /api/v1/reports/Unknown`
- **THEN** la respuesta es `404`

### Requirement: Vista previa paginada
`GET /api/v1/reports/{code}` SHALL devolver las columnas (clave, encabezado y formato), la página de filas pedida, el número de página, el tamaño de página y el total de filas. El tamaño de página por defecto MUST ser 50 y el máximo 200.

#### Scenario: Segunda página
- **WHEN** se pide R-01 con `page = 2` y `pageSize = 10` y hay 15 filas
- **THEN** la respuesta trae 5 filas, `page = 2` y `totalCount = 15`

#### Scenario: Tamaño excesivo
- **WHEN** se pide `pageSize = 500`
- **THEN** la respuesta es `400`

### Requirement: R-01 Estado de mantenimiento de la flota
R-01 SHALL listar una fila por rutina con cliente, placa, estado del vehículo, rutina, estado de activación, estado de vencimiento, odómetro, próximo mantenimiento en km y fecha, km y días restantes (negativos si está excedida, calculados respecto de la fecha de generación) y porcentaje consumido. MUST filtrar por cliente, estado del vehículo y estado de vencimiento, y MUST ordenar por vencidas, luego por vencer, luego al día y, dentro de cada grupo, por porcentaje consumido descendente.

#### Scenario: Filtro por vencidas tras la carga de ejemplo
- **WHEN** se pide R-01 con `dueStatus = Overdue` después de cargar `samples/import/` (evaluado el 2026-11-12)
- **THEN** las filas son RVA104 frenos, TAN201 aceite, TAN202 aceite y TAN203 aceite

#### Scenario: Orden por severidad
- **WHEN** se pide R-01 sin filtros
- **THEN** ninguna fila `DueSoon` aparece antes de una `Overdue` y ninguna `UpToDate` antes de una `DueSoon`

### Requirement: R-02 Órdenes de servicio
R-02 SHALL listar una fila por orden con número en formato `OS-000123`, placa, rutina, cliente, taller, origen, estado, fechas de creación, cotización, aprobación, en sitio, finalizada y cancelada, total y los tiempos de ciclo en horas creación → aprobación, aprobación → en sitio, en sitio → finalizada y creación → finalizada (vacíos si la fase no ocurrió). MUST filtrar por período de creación, estado, taller, cliente y origen.

#### Scenario: Orden finalizada
- **WHEN** una orden se creó a las 08:00, se aprobó a las 10:00, llegó a sitio a las 14:00 y se finalizó a las 18:00 del mismo día
- **THEN** su fila muestra 2, 4, 4 y 10 horas en los tiempos de ciclo

#### Scenario: Orden cancelada
- **WHEN** una orden se canceló antes de aprobarse
- **THEN** su fila muestra la fecha de cancelación y los tiempos de ciclo posteriores vacíos

### Requirement: R-03 Costos de mantenimiento (Recortable)
R-03 SHALL agrupar las órdenes finalizadas por cliente, taller y vehículo, con el total por rutina, el total por servicios adicionales, el total general, y el número y monto de las aprobadas automáticamente frente a las aprobadas por el cliente. MUST filtrar por período de finalización, cliente, taller y vehículo.

#### Scenario: Mezcla de aprobaciones
- **WHEN** un vehículo tiene una orden finalizada de 1.200.000 COP aprobada automáticamente y otra de 2.000.000 COP aprobada por el cliente con 300.000 COP de adicionales
- **THEN** su fila muestra total general 3.200.000, adicionales 300.000, una aprobada automáticamente por 1.200.000 y una aprobada por el cliente por 2.000.000

#### Scenario: Órdenes no finalizadas
- **WHEN** existen órdenes canceladas o en proceso en el período
- **THEN** no suman en R-03

### Requirement: R-04 Alertas y tiempos de atención (Recortable)
R-04 SHALL agrupar las alertas por tipo y origen, con el número de alertas generadas, atendidas y resueltas, y el tiempo medio en horas hasta la atención y hasta la resolución (solo sobre las alertas que llegaron a esa fase). MUST filtrar por período de generación, cliente, tipo y origen.

#### Scenario: Tiempo medio de atención
- **WHEN** hay dos alertas `Overdue` por `Distance`, atendidas a las 2 y 4 horas de generarse, y una tercera sin atender
- **THEN** la fila `Overdue` · `Distance` muestra 3 generadas, 2 atendidas y 3 horas de tiempo medio hasta la atención

### Requirement: Descarga en Excel
`GET /api/v1/reports/{code}/download?format=xlsx` SHALL responder un archivo `.xlsx` con una hoja de datos (encabezados en negrita, filtros automáticos, panel inmovilizado en la primera fila, montos en formato de moneda COP, fechas en `yyyy-MM-dd` o `yyyy-MM-dd HH:mm` y porcentajes con dos decimales) y una hoja «Parámetros» con el reporte, los filtros aplicados y la fecha y hora de generación en `America/Bogota`.

#### Scenario: Descarga de R-02
- **WHEN** se descarga R-02 en `xlsx` el 2026-11-12
- **THEN** la respuesta tiene `Content-Type` de hoja de cálculo OpenXML y `Content-Disposition: attachment; filename="R-02_ordenes_2026-11-12.xlsx"`, y el libro contiene las hojas de datos y «Parámetros»

### Requirement: Descarga en CSV (Recortable)
`GET /api/v1/reports/{code}/download?format=csv` SHALL responder un archivo `.csv` en UTF-8 con BOM, separador `;`, una fila de encabezados, fechas ISO y números con coma decimal, escapando entre comillas los valores que contienen `;`, comillas o saltos de línea.

#### Scenario: Valor con separador
- **WHEN** un nombre de taller contiene `;`
- **THEN** ese valor sale entre comillas dobles y el archivo se abre en Excel con las columnas correctas

### Requirement: Límite de filas por descarga
Una descarga SHALL rechazarse con `422` y `code = "Report.TooManyRows"` cuando el resultado supera 50.000 filas, indicando que se acoten los filtros, sin generar el archivo.

#### Scenario: Resultado excesivo
- **WHEN** los filtros de una descarga producen 50.001 filas
- **THEN** la respuesta es `422` con `code = "Report.TooManyRows"`

### Requirement: Etiquetas en español
Los encabezados y los valores de enumeración conocidos SHALL mostrarse en español en la vista previa y en los archivos (por ejemplo, `Overdue` → «Vencida», `OnSite` → «En sitio», `Completed` → «Finalizada»). Un valor desconocido MUST mostrarse tal como llegó.

#### Scenario: Valor desconocido
- **WHEN** una fila de R-02 tiene un estado que no está en el mapa de etiquetas
- **THEN** la celda muestra el valor original sin error
