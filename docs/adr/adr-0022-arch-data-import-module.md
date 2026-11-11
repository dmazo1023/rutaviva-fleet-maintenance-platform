---
title: "[ADR-0022] Crear un servicio DataImport para la carga inicial desde CSV con validación en tres niveles y simulación previa (dry-run) contra los servicios dueños"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "architecture"
  - "data-import"
  - "csv"
  - "onboarding"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: ""
---

## Contexto

- El enunciado parte de que los clientes **llevan el mantenimiento preventivo en Excel**. La adopción del sistema requiere migrar esa información.
- El equipo pide un **módulo aparte** de **carga inicial en CSV**, con **validación de datos**, que sea el **punto de partida** de la demo, alimentado por archivos de ejemplo.
- Los datos a cargar pertenecen a 4 servicios distintos: talleres (Workshops), clientes y aprobadores (Customers), vehículos (Fleet) y rutinas con su línea base (Maintenance). Cada servicio es dueño exclusivo de su schema (ADR-0013).
- Hay dependencias de orden entre las cargas: un vehículo referencia a su dueño y a su taller, y una rutina requiere que exista el plan del vehículo. Las réplicas se propagan por el EventBus (ADR-0024) con latencia de milisegundos a segundos.
- Excel en configuración regional de Colombia exporta el CSV con separador `;` y codificación Windows-1252. Otras herramientas usan `,` y UTF-8.
- El usuario necesita **ver los errores antes de guardar** y corregir el archivo.

## Decisión

Crearemos el servicio **`DataImport`** (Clean Architecture, schema `imports`) como **orquestador de la carga inicial**. **No escribe en schemas ajenos**: cada fila se registra a través de la **API del servicio dueño**, que aplica sus propias reglas de dominio.

**Archivos y orden de carga** (cada uno es una etapa de un `ImportJob`):

| Etapa | Archivo | Servicio dueño | Endpoint masivo |
|---|---|---|---|
| 1 | `talleres.csv` | Workshops | `POST /api/v1/workshops/bulk?dryRun=` |
| 2 | `clientes.csv` | Customers | `POST /api/v1/customers/bulk?dryRun=` |
| 3 | `aprobadores.csv` | Customers | `POST /api/v1/customers/approvers/bulk?dryRun=` |
| 4 | `vehiculos.csv` | Fleet | `POST /api/v1/fleet/vehicles/bulk?dryRun=` |
| 5 | `rutinas.csv` | Maintenance | `POST /api/v1/maintenance/routines/bulk?dryRun=` |

**Validación en tres niveles** (cada fila guarda su resultado y su motivo):

1. **Archivo** (DataImport): tamaño ≤ 5 MB y ≤ 5.000 filas, detección del separador (`;` o `,`) y de la codificación (UTF-8 con o sin BOM, Windows-1252), y encabezados exactos según la plantilla.
2. **Formato de la fila** (DataImport): campos obligatorios, tipos, rangos y formatos. Por ejemplo: placa `^[A-Z]{3}[0-9]{2}[0-9A-Z]$`, VIN de 17 caracteres, km ≥ 0, fecha no futura y % entre 0 y 100. También detecta duplicados dentro del mismo archivo.
3. **Reglas de negocio** (servicio dueño, `dryRun=true`): la placa ya existe, el NIT del dueño o del taller no existe, el código de rutina no existe, el km del último servicio es mayor que el odómetro, etc. El dueño responde un resultado por fila **sin persistir**.

**Flujo**: subir los archivos → **vista previa** con filas válidas y rechazadas → **confirmar** → se envían **solo las filas válidas** (`dryRun=false`), etapa por etapa. Una placa o NIT que ya existe se reporta como `Skipped`, así que cargar el mismo archivo dos veces no duplica registros. Antes de cada etapa dependiente, DataImport **espera la propagación** de la etapa anterior: reintenta la simulación hasta 10 s mientras haya errores de «referencia no encontrada».

**Resultado**: el `ImportJob` queda con totales por etapa (`Accepted`, `Rejected`, `Skipped`) y un **CSV de errores descargable**, que es el mismo archivo con una columna extra `error`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | La demo arranca con 5 archivos y un clic: la flota queda creada y las rutinas vencidas generan alertas y órdenes en segundos | Positive |
| POS-002 | Las reglas de negocio no se duplican: la simulación usa el mismo caso de uso del dueño, con `dryRun` | Positive |
| POS-003 | Se respeta la propiedad de los datos: DataImport no tiene permisos sobre otros schemas | Positive |
| POS-004 | El usuario corrige los errores con el CSV de errores descargado y vuelve a cargar sin duplicar | Positive |
| POS-005 | Agregar un nuevo tipo de archivo es agregar una `IImportDefinition` (patrón Strategy) sin tocar el motor | Positive |
| NEG-001 | **Excepción consciente a ADR-0010**: DataImport llama por HTTP a las APIs de otros servicios. Se justifica porque no es un bounded context de negocio, sino un cliente de las APIs públicas (Open Host Service), igual que la UI, y porque la vista previa exige respuesta síncrona | Negative |
| NEG-002 | Los servicios dueños necesitan endpoints masivos con `dryRun`: 5 endpoints adicionales | Negative |
| NEG-003 | La carga no es atómica entre etapas. Si la etapa 4 falla a mitad, las etapas 1 a 3 quedan guardadas. Se mitiga con la idempotencia (`Skipped`) y la recarga | Negative |
| NEG-004 | Unas 4 a 5 horas de esfuerzo en el plazo | Negative |

## Alternativas consideradas

### ALT-001 — Importación dentro de cada servicio dueño (un CSV por servicio, sin módulo aparte)

**Descripción**: Fleet importa vehículos, Maintenance importa rutinas, etc.

**Rejection rationale**: El equipo pidió un **módulo aparte** como punto de partida. Además, duplicaría en 4 servicios el lector de CSV, la detección de codificación y la vista previa.

### ALT-002 — DataImport escribe directamente en los schemas de los servicios

**Descripción**: Insertar con SQL masivo en `fleet.Vehicles`, `maintenance.MaintenanceRoutines`, etc.

**Rejection rationale**: Viola la propiedad por schema (ADR-0013) y los permisos `DENY` lo impiden. Además, saltaría las reglas de dominio y el Outbox, de modo que los vehículos importados nunca generarían sus planes ni sus alertas.

### ALT-003 — Publicar comandos en el bus de eventos (`RegisterVehicleRequested`) en lugar de HTTP

**Descripción**: DataImport produce comandos y los dueños responden con eventos de resultado.

**Rejection rationale**: La vista previa requiere una respuesta síncrona por fila. Correlacionar las respuestas asíncronas de 5.000 filas agrega estado, tiempos de espera y complejidad sin beneficio para una carga inicial.

### ALT-004 — Leer `.xlsx` directamente

**Descripción**: Aceptar el libro de Excel sin convertirlo.

**Rejection rationale**: El equipo definió CSV. Queda como evolución con ClosedXML, implementando otro `IFileReader`.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Lectura con **CsvHelper**, encapsulada en `CsvFileReader : IFileReader`. Application no conoce CsvHelper |
| IMP-002 | Patrones: **Strategy** (`IImportDefinition` por tipo de archivo: encabezados, mapeo, validador de fila y cliente del dueño), **Template Method** en `ImportStagePipeline` (leer → validar formato → simular → confirmar) y **Adapter** (`IOwnerServiceClient` por servicio con `HttpClient` tipado y *service discovery*) |
| IMP-003 | Endpoints: `POST /api/v1/imports` (multipart, crea el job y devuelve la vista previa), `POST /api/v1/imports/{id}/confirm`, `GET /api/v1/imports/{id}` y `GET /api/v1/imports/{id}/errors.csv`. También `GET /api/v1/imports/templates/{entity}.csv` para las plantillas |
| IMP-004 | Los endpoints masivos de los dueños procesan lotes de hasta 500 filas y devuelven `[{ rowNumber, outcome: Valid|Invalid|Skipped, errors[] }]`. Con `dryRun=true` ejecutan el validador y las reglas del dominio y luego descartan la unidad de trabajo |
| IMP-005 | Los archivos de ejemplo están en `samples/import/` (válidos) y en `samples/import/con-errores/` (para mostrar la validación). `samples/README.md` describe el resultado esperado de cada vehículo |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | adr-0010-arch-microservices-by-bounded-context: excepción documentada en NEG-001 |
| REF-002 | adr-0013-data-sqlserver-schema-per-service: schema `imports` |
| REF-003 | [Modelo de datos](../architecture/data-model.md): `imports.ImportJobs`, `imports.ImportRows` |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0022` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
