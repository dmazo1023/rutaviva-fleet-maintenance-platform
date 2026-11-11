---
title: "[ADR-0017] Disparar la revisión diaria por tiempo con un scheduler interno y un endpoint TimeEvaluation(asOfDate), sobre un único caso de uso monótono"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "architecture"
  - "scheduling"
  - "testability"
related_specs: []
related_plans: []
supersedes: "adr-0006"
superseded_by: ""
---

> **Revisión 2026-11-10 (en estado Proposed):** al eliminar Kafka (ADR-0024), el disparo deja de ser el evento `TimeEvaluationRequested` y pasa a ser un endpoint. El servicio Simulators desaparece. La separación entre disparo y ejecución y la monotonía de la fecha se mantienen.

## Contexto

Este ADR reemplaza al ADR-0006, que ejecutaba la revisión en un `BackgroundService` con un reloj adelantable mediante un endpoint de desarrollo.

- RN-2 exige una **revisión diaria** de las rutinas por tiempo.
- Los intervalos son de meses. Para la demo y las pruebas se necesita evaluar «como si hoy fuera» otra fecha, sin alterar el reloj del sistema.
- Ya no hay broker de mensajes (ADR-0024).
- Maintenance se despliega con una sola instancia (ADR-0024, NEG-003).
- Azure Functions no es necesaria para el reto.

## Decisión

Separaremos el **disparo** de la **ejecución** de la revisión:

1. **Ejecución**: `ReevaluateMaintenancePlansCommand(AsOfDate, VehicleId: null)` recorre los planes **activos** en **lotes de 100** e invoca `MaintenancePlan.Reevaluate(effectiveDate, thresholds)`, donde `effectiveDate = max(AsOfDate, LastTimeEvaluationDate)`. El tiempo nunca retrocede (monotonía, igual que RN-6 para el odómetro), y los parámetros vigentes en la BD siempre se vuelven a aplicar.
2. **Disparo automático**: `DailyTimeEvaluationScheduler` (un `BackgroundService` en Maintenance) despacha el command a las 00:05 en `America/Bogota`. Al arrancar, lo despacha con la fecha actual si `LastTimeEvaluationDate` es anterior.
3. **Disparo manual**: `POST /api/v1/maintenance/time-evaluations { asOfDate }`, expuesto por el Gateway, despacha el mismo command y responde `200` con un resumen: `{ evaluatedPlans, dueSoon, overdue, ordersRequested }`. Lo usan la pantalla de simulación del CRM y los archivos `.http` de la demo.
4. El mismo command atiende el botón «Actualizar» de la web, con `AsOfDate` = hoy (ADR-0019).

Repetir una fecha ya evaluada no produce efectos: la monotonía mantiene el estado, el índice único de alertas evita duplicarlas y el índice de RN-3 evita duplicar órdenes.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | En la demo, `asOfDate = 2027-04-25` lleva una rutina de 6 meses a «por vencer» y `2027-05-12` a «vencida», en segundos y sin alterar el reloj del sistema | Positive |
| POS-002 | Las pruebas pasan la fecha como dato. No hay `DateTime.Now` en el dominio ni en Application | Positive |
| POS-003 | Una revisión repetida o atrasada no retrocede estados ni duplica alertas u órdenes | Positive |
| POS-004 | Se elimina el servicio Simulators: el CRM llama a los endpoints reales de Maintenance | Positive |
| NEG-001 | Como la monotonía impide retroceder, para repetir la demo desde cero hay que reiniciar los datos (script de *seed*) | Negative |
| NEG-002 | Con varias instancias de Maintenance, cada una ejecutaría su scheduler. Por los índices únicos el resultado sería correcto, pero el trabajo se duplicaría. Escalar requiere un *lock* distribuido (por ejemplo `sp_getapplock`) | Negative |
| NEG-003 | El endpoint manual es síncrono. Con miles de planes, la petición se alarga; la evolución es responder `202` y procesar en segundo plano | Negative |

## Alternativas consideradas

### ALT-001 — Evento `TimeEvaluationRequested` en Kafka (versión previa de este ADR)

**Descripción**: El scheduler publicaba el evento y un consumidor ejecutaba la revisión.

**Rejection rationale**: Kafka se eliminó de la solución (ADR-0024).

### ALT-002 — Publicar `TimeEvaluationRequested` en el EventBus

**Descripción**: El scheduler publica en un stream del EventBus y Maintenance se suscribe a sí mismo.

**Rejection rationale**: El productor y el consumidor son el mismo servicio. El bus agrega un salto sin desacoplar a nadie (YAGNI).

### ALT-003 — Azure Functions con Timer Trigger

**Descripción**: Una Function que dispara la revisión.

**Rejection rationale**: El equipo la descartó como no necesaria. Agrega Core Tools y Azurite al entorno local sin un driver.

### ALT-004 — Evaluar el tiempo en cada lectura de odómetro

**Descripción**: Aprovechar las lecturas para evaluar también el tiempo.

**Rejection rationale**: Un vehículo detenido no envía lecturas y su rutina por tiempo nunca vencería, lo que incumple RN-2.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | `IClock` solo lo usa el scheduler (calcular «hoy» en `America/Bogota`). La lógica de evaluación recibe `AsOfDate` como parámetro |
| IMP-002 | Un fallo de un plan dentro del lote se registra (`TimeEvaluationFailed` con `VehicleId`) y no aborta el lote |
| IMP-003 | `samples/http/time-evaluations.http` reproduce las fechas de la demo (2027-01-20 y 2027-02-10) con sus resultados esperados |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | adr-0006-arch-daily-time-evaluation-host: decisión reemplazada |
| REF-002 | adr-0024-int-eventbus-signalr-sql-log: eliminación de Kafka |
| REF-003 | adr-0019-data-business-parameters-in-database: botón «Actualizar» |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0017` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
