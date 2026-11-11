## Purpose

Llevar una carga inicial desde los archivos subidos hasta los registros creados en los servicios dueños: vista previa con las reglas de negocio de cada dueño sin persistir, confirmación por etapas en orden de dependencias, idempotencia y un CSV de errores para corregir y recargar (ADR-0022).

## ADDED Requirements

### Requirement: Trabajo de importación con etapas ordenadas
Cada carga SHALL crear un trabajo con una etapa por archivo. Las etapas MUST procesarse siempre en el orden `Workshops` (1), `Customers` (2), `Approvers` (3), `Vehicles` (4) y `Routines` (5), sin importar el orden en que se subieron. El trabajo SHALL pasar por los estados `Validating` → `PendingConfirmation` → `Importing` → `Completed` | `CompletedWithErrors`, o terminar en `Failed`.

#### Scenario: Archivos subidos en desorden
- **WHEN** el usuario sube `rutinas.csv`, `talleres.csv` y `vehiculos.csv` en ese orden
- **THEN** las etapas quedan con orden 1 (`Workshops`), 4 (`Vehicles`) y 5 (`Routines`)

#### Scenario: Vista previa lista
- **WHEN** se suben los 5 archivos de `samples/import/` y todas las etapas se validan
- **THEN** la respuesta es `201` con el trabajo en `PendingConfirmation` y los totales por etapa

#### Scenario: Una etapa falla a nivel de archivo
- **WHEN** una de las etapas queda `Failed` por tamaño, filas o encabezados
- **THEN** el trabajo queda `Failed`, la respuesta muestra el motivo de esa etapa y el trabajo no se puede confirmar

### Requirement: Vista previa con simulación en el servicio dueño
Las filas sin errores de formato SHALL enviarse al endpoint `bulk?dryRun=true` del servicio dueño, en lotes de hasta 500 filas, y cada fila MUST quedar con el resultado que devuelve el dueño: `Valid`, `Invalid` (con los errores de negocio) o `Skipped` (ya existe). La simulación MUST NOT persistir datos en ningún servicio.

#### Scenario: Errores de negocio en vehículos (con la carga válida ya hecha)
- **WHEN** después de importar `samples/import/` se sube `samples/import/con-errores/vehiculos.csv`
- **THEN** la vista previa muestra `RVA105` y la primera `RVA106` como `Valid`; `TAN204` `Invalid` (NIT del dueño 999999999 no existe), `TAN205` `Invalid` (NIT del taller 900199999 no existe) y `EVA308` `Invalid` (el responsable no es un aprobador registrado), todos por nivel de negocio; y `RVA101` `Skipped` (la placa ya existe)

#### Scenario: Errores de negocio en rutinas (con la carga válida ya hecha)
- **WHEN** en el mismo trabajo se sube `samples/import/con-errores/rutinas.csv`
- **THEN** la vista previa muestra `RVA102 · BRAKES` `Valid`; `RVA102 · ENGINE_X` `Invalid` (código de rutina inexistente), `RVA103 · BRAKES` `Invalid` (km del último servicio 35.000 mayor que el odómetro 30.200) y `ZZZ999` `Invalid` (vehículo no registrado), por nivel de negocio; y `RVA101 · OIL_CHANGE` `Skipped` (la rutina ya existe para el vehículo)

#### Scenario: Cada fila conserva su nivel de error
- **WHEN** se consulta la vista previa de `samples/import/con-errores/vehiculos.csv`
- **THEN** cada error indica si viene del nivel `Format` o del nivel `Business`

#### Scenario: Simulación sin efectos
- **WHEN** termina la vista previa de los 5 archivos de `samples/import/` sobre una base vacía
- **THEN** ningún servicio dueño tiene talleres, clientes, aprobadores, vehículos ni rutinas nuevos

### Requirement: Referencias resueltas dentro del mismo trabajo
Durante la vista previa, un error de «referencia no encontrada» devuelto por el dueño MUST ignorarse cuando la clave referenciada pertenece a una fila `Valid` de una etapa anterior del mismo trabajo, y la fila SHALL quedar `Valid`. Si la clave no está en el trabajo, el error se mantiene.

#### Scenario: Carga inicial sobre una base vacía
- **WHEN** se suben los 5 archivos de `samples/import/` sobre una base vacía
- **THEN** las 36 filas (3 talleres, 3 clientes, 5 aprobadores, 10 vehículos y 15 rutinas) quedan `Valid`, aunque los dueños aún no conozcan los NIT, correos y placas del mismo trabajo

#### Scenario: Referencia ajena al trabajo
- **WHEN** en un trabajo sin etapa `Vehicles` una rutina referencia la placa `ZZZ999`, que no existe
- **THEN** la fila queda `Invalid` con el error del dueño

### Requirement: Consulta de la vista previa por etapa y resultado
El sistema SHALL devolver el trabajo con su estado, sus etapas (orden, archivo, separador, codificación, estado, motivo de falla y totales `TotalRows`, `ValidRows`, `InvalidRows`, `SkippedRows`, `ImportedRows`) y sus filas paginadas, filtrables por tipo de entidad y por resultado, con un máximo de 200 filas por página. Cada fila MUST incluir su número de fila en el archivo, sus datos originales, su resultado y sus errores.

#### Scenario: Solo las filas inválidas de vehículos
- **WHEN** el usuario pide `GET /api/v1/imports/{id}?entity=Vehicles&outcome=Invalid`
- **THEN** recibe solo las filas `Invalid` de la etapa `Vehicles`, con su número de fila y sus errores

#### Scenario: Trabajo inexistente
- **WHEN** el usuario pide un trabajo que no existe
- **THEN** la respuesta es `404`

### Requirement: Confirmación que importa solo las filas válidas
La confirmación SHALL estar permitida solo en `PendingConfirmation` y con al menos una fila `Valid`. Al confirmar, el trabajo MUST pasar a `Importing` y enviar, etapa por etapa y en orden, **solo las filas `Valid`** al endpoint `bulk?dryRun=false` del dueño. Cada fila enviada MUST terminar `Imported`, `Skipped` (si entre tanto alguien la creó) o `Failed` (con el error del dueño). Las filas `Invalid` y `Skipped` de la vista previa no se envían.

#### Scenario: Carga inicial completa
- **WHEN** se confirma el trabajo con los 5 archivos de `samples/import/` sobre una base vacía
- **THEN** el trabajo queda `Completed` con `ImportedRows` = 3, 3, 5, 10 y 15 por etapa

#### Scenario: Carga con errores
- **WHEN** se confirma el trabajo de `samples/import/con-errores/` sobre la carga válida
- **THEN** solo se envían `RVA105`, `RVA106` y `RVA102 · BRAKES`, que quedan `Imported`, y el trabajo queda `Completed`

#### Scenario: Confirmar dos veces
- **WHEN** el usuario confirma un trabajo que ya está `Completed`
- **THEN** la respuesta es `409` con `code = "ImportJob.InvalidStatus"` y nada se reenvía

#### Scenario: Nada que importar
- **WHEN** el usuario confirma un trabajo cuyas filas son todas `Invalid` o `Skipped`
- **THEN** la respuesta es `422` con `code = "ImportJob.NothingToImport"`

#### Scenario: Fila que falla al confirmar
- **WHEN** el dueño rechaza al confirmar una fila que era `Valid` en la vista previa
- **THEN** esa fila queda `Failed` con el error del dueño y el trabajo termina `CompletedWithErrors`

### Requirement: Espera de propagación entre etapas dependientes
Antes de enviar una etapa con orden mayor que 1, el sistema SHALL repetir la simulación de sus filas válidas mientras haya errores de «referencia no encontrada» sobre claves de etapas anteriores del mismo trabajo, durante un máximo de 10 segundos. Al agotar el tiempo, las filas que siguen con ese error MUST quedar `Failed` y las demás se envían.

#### Scenario: Réplica que tarda en propagarse
- **WHEN** al confirmar, Fleet aún no conoce al cliente `900000001` durante 2 segundos después de importar la etapa `Customers`
- **THEN** la etapa `Vehicles` espera, vuelve a simular y luego importa sus filas sin errores

#### Scenario: Réplica que nunca llega
- **WHEN** Fleet no conoce al taller `900100001` después de 10 segundos
- **THEN** los vehículos que lo referencian quedan `Failed` con el error de referencia y el trabajo termina `CompletedWithErrors`

### Requirement: Idempotencia de la recarga
Recargar archivos cuyas entidades ya existen MUST NOT duplicar registros: el dueño las reporta como `Skipped` y la vista previa las muestra así.

#### Scenario: Recargar la carga inicial
- **WHEN** después de importar `samples/import/` se suben de nuevo los mismos 5 archivos
- **THEN** las 36 filas quedan `Skipped` y la confirmación responde `422` con `ImportJob.NothingToImport`

### Requirement: Falla del servicio dueño
Si un servicio dueño no responde después de los reintentos de transporte, la etapa en curso MUST quedar `Failed` con el motivo, las etapas siguientes MUST NOT ejecutarse y el trabajo MUST quedar `Failed`. Las etapas ya importadas se conservan.

#### Scenario: Fleet caído al confirmar
- **WHEN** Fleet no está disponible durante la etapa `Vehicles`
- **THEN** la etapa `Vehicles` queda `Failed` con `ImportOwner.Unavailable`, la etapa `Routines` no se ejecuta, el trabajo queda `Failed` y los talleres, clientes y aprobadores ya importados siguen existiendo

### Requirement: CSV de errores descargable por etapa
El sistema SHALL generar, para una etapa, un CSV con el encabezado original más la columna `error` y una línea por cada fila `Invalid` o `Failed`, con sus datos originales sin modificar y los mensajes de error unidos por ` | `. El archivo MUST usar el separador de la etapa y UTF-8 con BOM.

#### Scenario: Descargar errores de vehículos
- **WHEN** el usuario pide `GET /api/v1/imports/{id}/errors.csv?entity=Vehicles` del trabajo de `samples/import/con-errores/`
- **THEN** recibe un CSV con el encabezado de `Vehicles` más `error` y 9 líneas de datos (la segunda `RVA106`, `RV-107`, `TAN204`, `TAN205`, `EVA304`, `EVA305`, `EVA306`, `EVA307` y `EVA308`), sin `RVA101` (`Skipped`), `RVA105` ni la primera `RVA106`

#### Scenario: Etapa sin errores
- **WHEN** el usuario pide los errores de una etapa sin filas `Invalid` ni `Failed`
- **THEN** recibe un CSV con solo el encabezado
