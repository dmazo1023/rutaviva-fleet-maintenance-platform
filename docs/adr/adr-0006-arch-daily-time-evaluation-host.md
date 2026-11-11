---
title: "[ADR-0006] Ejecutar la revisión diaria por tiempo como BackgroundService en la API, con caso de uso agnóstico del host y reloj abstracto"
status: "Superseded"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "architecture"
  - "scheduling"
  - "background-service"
  - "azure-functions"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: "adr-0017"
---

## Contexto

RN-2 exige que la evaluación por tiempo se realice en una **revisión diaria del sistema**.

- La evaluación debe recorrer todos los vehículos y reevaluar sus rutinas con la fecha actual. Puede producir transiciones a `DueSoon` (aviso) y a `Overdue` (creación de una orden).
- Los intervalos de tiempo son de meses, así que verificar el comportamiento en una demo en vivo exige poder **simular el paso del tiempo**.
- Las pruebas que dependen de `DateTime.Now` no son repetibles (principio FIRST).
- La persistencia es en memoria dentro del proceso de la API (ADR-0005). Un segundo proceso no ve el mismo estado.
- La compañía usa habitualmente Azure Functions con *Timer Trigger* para trabajos programados.

## Decisión

Modelaremos la revisión diaria como un **caso de uso de Application independiente del host**, `EvaluateTimeBasedRoutinesCommand`. Lo ejecutará un **`BackgroundService` con `PeriodicTimer`** dentro de `RutaViva.Api`:

- `DailyRoutineEvaluationJob` (en Api) solo despacha el command. No contiene lógica.
- El handler obtiene la fecha de **`IClock`** y llama a `vehicle.ReevaluateRoutines(today)` por cada vehículo, con la misma unidad de trabajo y despacho de eventos que la ingesta por km.
- La hora de ejecución y el periodo se configuran en `DailyEvaluationOptions` (`RunAtUtc` y `Period`), no en literales.
- **Solo en Development**: `ShiftableClock` (un decorator de `IClock`) y el endpoint `POST /api/v1/dev/clock/advance` permiten adelantar el reloj N días, y `POST /api/v1/dev/evaluations/time-based` dispara la revisión a demanda. Ambos se registran detrás de `IHostEnvironment.IsDevelopment()`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | La demo muestra la transición «al día → por vencer → vencida» por tiempo en menos de 1 minuto, sin esperar 6 meses | Positive |
| POS-002 | Las pruebas de dominio y de aplicación fijan la fecha con un `FakeClock`, con 0 dependencias de `DateTime.Now` | Positive |
| POS-003 | Un solo proceso comparte el estado en memoria con la ingesta HTTP, así que no hacen falta broker ni base de datos compartidos | Positive |
| POS-004 | Migrar a Azure Functions supone crear un proyecto host con un *Timer Trigger* que despacha el mismo command: 0 cambios en Domain y Application | Positive |
| NEG-001 | Con varias réplicas de la API, la revisión correría N veces. La idempotencia de las transiciones (ADR-0002) evita duplicar avisos u órdenes, pero se desperdicia cómputo. En producción haría falta un *leader lock* o Azure Functions | Negative |
| NEG-002 | Si la API está caída a la hora programada, la revisión de ese día no corre hasta el siguiente tick. Al arrancar se ejecuta una revisión inmediata para mitigarlo | Negative |

## Alternativas consideradas

### ALT-001 — Azure Functions (isolated worker, .NET 10) con Timer Trigger

**Descripción**: Un proyecto `RutaViva.Functions` con `[TimerTrigger("0 0 5 * * *")]` que despacha `EvaluateTimeBasedRoutinesCommand`.

**Rejection rationale**: Con persistencia en memoria, la Function corre en **otro proceso** y no vería los vehículos ni las órdenes de la API. Para funcionar necesitaría una base de datos real (excluida por el enunciado) o llamar a la API por HTTP, lo que agrega un salto de red y un contrato interno sin valor de negocio. También exige Azure Functions Core Tools y Azurite en el portátil de la demo. **Es la evolución recomendada** cuando exista persistencia compartida (ADR-0005, ALT-001), y se puede presentar así en la sustentación.

### ALT-002 — Hangfire o Quartz.NET

**Descripción**: Un planificador con persistencia de trabajos, reintentos y panel.

**Rejection rationale**: Requiere almacenamiento propio de los trabajos y agrega dependencias para un único trabajo diario. `PeriodicTimer` cubre el requisito con 0 dependencias (YAGNI).

### ALT-003 — Evaluar el tiempo «en lectura» (calcular el estado al consultar)

**Descripción**: No almacenar el estado y calcularlo cada vez que se consulta una rutina.

**Rejection rationale**: RN-2 y RN-3 exigen **efectos** al cruzar el umbral (avisar una vez y crear una orden). Un cálculo en lectura no produce eventos y un vehículo sin consultas nunca generaría su orden.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | `IClock` expone `DateTimeOffset UtcNow` y `DateOnly Today`. La zona horaria del negocio (`America/Bogota`) se configura en `ClockOptions`, porque el «día» de negocio no es el día UTC |
| IMP-002 | El job captura y registra las excepciones por vehículo (`TimeBasedEvaluationFailed` con `VehicleId`) para que un vehículo con error no detenga la revisión del resto |
| IMP-003 | Si se decide adoptar ALT-001 en la entrega, la Function **no** accede a datos directamente: despacha el command, y el handler usa los puertos de repositorio (restricción de Clean Architecture del equipo) |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Microsoft Learn: *Background tasks with hosted services in ASP.NET Core* |
| REF-002 | Microsoft Learn: *Timer trigger for Azure Functions* (isolated worker model) |
| REF-003 | adr-0005-data-persistence-ports-in-memory: estado en un solo proceso |
| REF-004 | adr-0002-arch-aggregate-boundaries: idempotencia de las transiciones |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0006` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
