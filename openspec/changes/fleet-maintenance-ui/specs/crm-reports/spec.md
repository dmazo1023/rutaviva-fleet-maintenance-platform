## Purpose

Permitir consultar y descargar los reportes operativos de RutaViva (R-01 a R-04) en Excel y CSV desde el CRM (ADR-0023, RQ-09). Versión básica, como en el mockup.

## ADDED Requirements

### Requirement: Catálogo de reportes
El CRM SHALL mostrar el catálogo de reportes devuelto por `GET /api/v1/reports`, con su nombre, descripción y filtros disponibles.

#### Scenario: Catálogo
- **WHEN** abro `/reportes`
- **THEN** veo los reportes disponibles, entre ellos «Estado de mantenimiento de la flota» y «Órdenes de servicio»

### Requirement: Filtros y vista previa paginada
Al elegir un reporte, el CRM SHALL mostrar sus filtros (con el cliente activo como valor inicial del filtro de cliente) y una vista previa paginada de los datos según los filtros.

#### Scenario: Vista previa de R-01
- **WHEN** elijo R-01 con el filtro «Vencida»
- **THEN** la vista previa muestra solo las rutinas vencidas del cliente, en páginas

#### Scenario: Sin datos
- **WHEN** los filtros no devuelven filas
- **THEN** la vista previa muestra «Sin datos para estos filtros» y la descarga sigue disponible

### Requirement: Descarga en Excel y CSV
El CRM SHALL permitir descargar el reporte con los mismos filtros en `.xlsx` o `.csv`, usando el nombre de archivo que entrega el servidor.

#### Scenario: Descarga en Excel
- **WHEN** pulso «Excel» en R-02
- **THEN** se descarga el archivo de `GET /api/v1/reports/ServiceOrders/download?format=xlsx` con los filtros activos
