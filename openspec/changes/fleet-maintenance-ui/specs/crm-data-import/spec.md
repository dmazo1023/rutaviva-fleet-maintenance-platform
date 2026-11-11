## Purpose

Ofrecer el punto de partida de la demo: un asistente para migrar la flota desde los archivos de Excel (CSV) con vista previa de errores por fila antes de confirmar (ADR-0022, RQ-08). Versión básica, como en el mockup.

## ADDED Requirements

### Requirement: Descarga de plantillas
El CRM SHALL ofrecer la descarga de la plantilla CSV de cada una de las 5 etapas (talleres, clientes, aprobadores, vehículos y rutinas), en el orden de carga.

#### Scenario: Plantilla de vehículos
- **WHEN** pulso la plantilla de vehículos
- **THEN** se descarga `GET /api/v1/imports/templates/Vehicles.csv`

### Requirement: Subida y vista previa
El CRM SHALL permitir adjuntar entre 1 y 5 archivos, uno por etapa, enviarlos y mostrar la vista previa por etapa con los totales de filas válidas, inválidas y omitidas, y la lista de filas rechazadas con su número de fila y sus errores.

#### Scenario: Archivo con errores
- **WHEN** subo `samples/import/con-errores/vehiculos.csv`
- **THEN** la vista previa muestra RV-107 como inválida por «Formato de placa inválido» y RVA101 como omitida

#### Scenario: Archivo con encabezados incorrectos
- **WHEN** subo un archivo cuyos encabezados no coinciden con la plantilla
- **THEN** veo el error del archivo y la acción Confirmar queda deshabilitada

### Requirement: Confirmación y CSV de errores
Tras la vista previa el CRM SHALL permitir confirmar la importación de las filas válidas y mostrar el resultado por etapa. Si hubo filas rechazadas MUST ofrecer la descarga del CSV de errores.

#### Scenario: Confirmar
- **WHEN** confirmo una carga con filas válidas
- **THEN** el CRM envía `POST /api/v1/imports/{id}/confirm` y muestra los totales importados por etapa

#### Scenario: Descargar errores
- **WHEN** la carga tiene filas rechazadas y pulso «Descargar errores»
- **THEN** se descarga `GET /api/v1/imports/{id}/errors.csv`
