---
title: "[ADR-0019] Gestionar los parámetros de negocio en SQL Server, editables desde la web, sin caché y con reevaluación a demanda"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "data"
  - "configuration"
  - "sql-server"
  - "modifiability"
related_specs: []
related_plans: []
supersedes: ""
superseded_by: ""
---

## Contexto

- El umbral «por vencer» (10 % en el enunciado) debe ser **parametrizable por tipo de rutina** en una matriz SQL, con la posibilidad de ajustarlo por rutina (A-02).
- El monto de aprobación automática (1.500.000 COP) debe ser **parametrizable en la BD** y aplicar desde la siguiente cotización (A-06).
- Ambos parámetros, y los intervalos de cada rutina, deben poder **editarse desde la web** (RQ-01, RQ-02).
- Si alguien edita un valor **directamente en SQL**, la web debería reflejarlo. El equipo aceptó resolverlo con un **botón «Actualizar»** en lugar de una detección automática, que agregaría mucha complejidad (RQ-03).
- Los estados de las rutinas (`UpToDate`, `DueSoon`, `Overdue`) se **almacenan**, porque sus transiciones producen efectos (alertas y órdenes). Un cambio de parámetro puede dejar un estado almacenado desalineado hasta la siguiente evaluación.

## Decisión

Guardaremos los parámetros de negocio **en el schema del servicio que los usa** y los leeremos **siempre desde la BD**, sin caché:

| Parámetro | Tabla | Servicio | Efecto de un cambio |
|---|---|---|---|
| Intervalos por defecto y % «por vencer» por tipo de rutina (matriz) | `maintenance.RoutineTypes` | Maintenance | Reevaluación inmediata de las rutinas de ese tipo |
| Intervalos y ajuste de % por rutina del vehículo | `maintenance.MaintenanceRoutines` (`IntervalKm`, `IntervalMonths`, `DueSoonThresholdPercentOverride`) | Maintenance | Reevaluación inmediata del plan |
| Monto de aprobación automática | `serviceorders.ApprovalPolicies` (**solo inserción**, con `EffectiveFrom`) | ServiceOrders | Aplica a la siguiente cotización. Las órdenes cotizadas conservan su copia del valor |

- **Porcentaje efectivo** = `DueSoonThresholdPercentOverride ?? RoutineType.DueSoonThresholdPercent`. Lo resuelve el VO `DueSoonThreshold` en el dominio.
- **Cambios desde la web** (camino principal): un command de Application actualiza el parámetro y, en la misma transacción, dispara `ReevaluateMaintenancePlansCommand` para los planes afectados. Los eventos resultantes (`RoutineDueStatusChanged`, `AlertRaised`) llegan a la web por SignalR.
- **Ediciones directas en SQL** (camino secundario): el botón **«Actualizar»** de las vistas de Fleet y Maintenance llama a `POST /api/v1/maintenance/reevaluations { vehicleId? }`. Ese endpoint reevalúa con los parámetros vigentes en la BD, el odómetro actual y la fecha efectiva `max(hoy, LastTimeEvaluationDate)`, y la vista se recarga. Las cotizaciones no necesitan el botón, porque leen la política vigente en cada operación.
- Reglas de reevaluación por un cambio de parámetros (A-16):
  - `UpToDate ↔ DueSoon` puede cambiar en ambos sentidos. Si `DueSoon` vuelve a `UpToDate`, su alerta se resuelve con `ParametersChanged`.
  - `Overdue` **no retrocede**.
  - Un aviso ya emitido **no se repite** en el mismo ciclo.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Cambiar el 10 % de «Cambio de aceite» a 15 % desde la web actualiza los estados y las alertas de todos los vehículos afectados en segundos, sin desplegar | Positive |
| POS-002 | `ApprovalPolicies`, al ser solo de inserción, deja un historial auditable: quién cambió el monto, cuándo y desde qué fecha aplica | Positive |
| POS-003 | Una edición directa en SQL se refleja con un clic, sin infraestructura adicional (sin CDC, *triggers* ni *polling*) | Positive |
| POS-004 | Los parámetros siguen el principio de un dueño por schema: ningún servicio lee los parámetros de otro | Positive |
| NEG-001 | Leer sin caché agrega 1 consulta por evaluación o cotización (menos de 2 ms en local). Con alto volumen se usaría `HybridCache`, con invalidación por evento | Negative |
| NEG-002 | Una edición directa en SQL no se refleja hasta que alguien pulsa «Actualizar», o hasta la siguiente lectura de odómetro o revisión diaria | Negative |
| NEG-003 | La reevaluación masiva por un cambio de tipo de rutina recorre los planes en lotes (de 100). Con miles de vehículos tardaría segundos | Negative |

## Alternativas consideradas

### ALT-001 — Detección automática de ediciones en SQL (Change Tracking + *polling*, `SqlDependency`, o CDC con Debezium hacia Kafka)

**Descripción**: Detectar los cambios en las tablas de parámetros y reevaluar automáticamente.

**Rejection rationale**: Requiere habilitar Change Tracking o CDC, un proceso de *polling* o un conector Debezium, y su manejo de errores. El equipo lo descartó por complejidad frente al botón «Actualizar». Queda documentado como evolución.

### ALT-002 — Parámetros en `appsettings.json` u `IOptionsMonitor`

**Descripción**: Configuración por archivo, recargable.

**Rejection rationale**: No se puede editar desde la web, no deja historial y no permite un valor por tipo de rutina sin una estructura ad hoc.

### ALT-003 — Calcular el estado de las rutinas al leer, sin almacenarlo

**Descripción**: La vista siempre calcula el estado con los parámetros actuales.

**Rejection rationale**: Las transiciones deben producir efectos (alerta y orden) aunque nadie consulte la vista (RN-2 y RN-3). Si el estado calculado al leer y el estado almacenado difirieran, la UI mostraría una rutina vencida sin orden.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Validaciones (FluentValidation y VO): `0 < DueSoonThresholdPercent < 100`; `IntervalKm > 0`; `IntervalMonths > 0`; al menos un intervalo; `AutoApprovalMaxAmount ≥ 0` |
| IMP-002 | `IApprovalPolicyProvider.GetCurrentAsync(currency, asOf)` consulta `TOP (1) ... WHERE EffectiveFrom <= @asOf ORDER BY EffectiveFrom DESC` |
| IMP-003 | El script de referencia siembra los tipos de rutina (aceite 10.000 km / 6 meses / 10 %, frenos, llantas) y la política inicial de 1.500.000 COP |
| IMP-004 | La UI muestra, junto a cada rutina, el **% efectivo** y si viene del tipo o del ajuste de la rutina |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | adr-0013-data-sqlserver-schema-per-service · adr-0017-arch-time-evaluation-trigger |
| REF-002 | [Modelo de datos](../architecture/data-model.md): `RoutineTypes`, `ApprovalPolicies` |
| REF-003 | Microsoft Learn: *About Change Tracking* (SQL Server), descartado en ALT-001 |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0019` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
