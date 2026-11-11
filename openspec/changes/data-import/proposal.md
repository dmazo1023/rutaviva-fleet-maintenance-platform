## Why

Los clientes de RutaViva llevan hoy el mantenimiento preventivo en Excel. Para adoptar el sistema necesitan migrar talleres, clientes, aprobadores, vehículos y rutinas con su último servicio, y ver los errores **antes** de guardar nada (RQ-08). La carga inicial es además el **punto de partida de la demo**: con 5 archivos y un clic, la flota queda creada y las rutinas vencidas generan alertas y órdenes (ADR-0022 POS-001).

Los datos pertenecen a 4 servicios distintos (ADR-0013), así que la carga no puede escribir en sus schemas: debe pasar por las reglas de dominio de cada dueño. Corresponde al corte **S4b** del análisis (§10). La carga CSV de vehículos y rutinas está entre lo **imprescindible para la demo**.

## What Changes

- Servicio nuevo **`RutaViva.DataImport`** (Clean Architecture, schema `imports`, usuario `rutaviva_imports_app`) con el agregado `ImportJob` (etapas `ImportStage` y filas `ImportRow`) (ADR-0022).
- Carga de 1 a 5 archivos CSV por trabajo, procesados en orden fijo: `talleres.csv` → `clientes.csv` → `aprobadores.csv` → `vehiculos.csv` → `rutinas.csv`.
- Validación en tres niveles: archivo (tamaño, filas, separador, codificación y encabezados), formato de fila (obligatorios, tipos, rangos, patrones y duplicados en el archivo) y reglas de negocio del servicio dueño mediante `bulk?dryRun=true`.
- Vista previa por fila con resultado `Valid`, `Invalid` o `Skipped` y sus motivos; las referencias a filas válidas de etapas anteriores del mismo trabajo se consideran resueltas.
- Confirmación que envía **solo las filas válidas**, etapa por etapa, con espera de propagación de hasta 10 s ante errores de «referencia no encontrada».
- Idempotencia: una placa, NIT o rutina que ya existe se reporta como `Skipped`; recargar los mismos archivos no duplica registros.
- CSV de errores descargable por etapa (el archivo original con la columna extra `error`) y plantillas CSV descargables **(Recortable)**.
- Endpoints `POST /api/v1/imports`, `POST /api/v1/imports/{id}/confirm`, `GET /api/v1/imports/{id}`, `GET /api/v1/imports/{id}/errors.csv` y `GET /api/v1/imports/templates/{entity}.csv` (registro de contratos de `foundation-platform`).
- Pruebas: dominio, aplicación, lector CSV contra `samples/import/` y `samples/import/con-errores/`, e integración con servicios dueños simulados.

## Capabilities

### New Capabilities

- `data-import-validation`: validación de los archivos CSV de carga inicial en los niveles de archivo y de formato de fila, y plantillas descargables.
- `data-import-workflow`: ciclo de vida del trabajo de importación: vista previa con simulación en el dueño, confirmación por etapas, espera de propagación, idempotencia y CSV de errores.

### Modified Capabilities

(ninguna)

## Impact

- Código nuevo: `src/Services/DataImport/RutaViva.DataImport.{Domain,Application,Infrastructure,Api}` y `tests/Services/DataImport/*`.
- Dependencias NuGet: CsvHelper (solo en Infrastructure) y `Microsoft.Extensions.Http.Resilience`.
- Depende de `foundation-platform` (BuildingBlocks, contrato bulk) y, para ejecutarse de punta a punta, de los endpoints `bulk?dryRun=` de `supporting-contexts` (Workshops, Customers, Fleet) y `maintenance-core` (rutinas). Requiere contratos nuevos en esos changes (ver `design.md` § Contratos nuevos requeridos).
- Consumidor: la pantalla `/carga` de `fleet-maintenance-ui`.
- Gateway: ruta `/api/v1/imports/**` (change `edge-gateway-notifications`).

## Fuera de alcance

- Leer `.xlsx` directamente (ADR-0022 ALT-004).
- Atomicidad entre etapas: si una etapa falla a mitad, las anteriores quedan guardadas (ADR-0022 NEG-003; se mitiga con `Skipped` y la recarga).
- Procesamiento asíncrono en segundo plano y cargas mayores a 5.000 filas o 5 MB por archivo.
- Actualizar entidades existentes desde el CSV (solo alta; lo existente es `Skipped`).
- Publicar eventos de integración propios de DataImport (YAGNI: nadie los consume).

**Recortable**: plantillas CSV descargables (`GET /api/v1/imports/templates/{entity}.csv`).
