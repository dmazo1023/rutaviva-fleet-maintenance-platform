## Why

Maintenance es el *core domain* de RutaViva: decide **cuándo le toca mantenimiento a cada vehículo** (RN-1, RN-2), avisa una sola vez por ciclo, declara el vencimiento que origina la orden de servicio (RN-3), reinicia el ciclo cuando el taller completa el servicio (RN-5) y protege la monotonía del odómetro (RN-6). Sin este servicio no hay alertas, ni órdenes automáticas, ni demo. Corresponde al corte **S1** del análisis (§10) y depende de `foundation-platform`.

## What Changes

- Servicio `RutaViva.Maintenance` (Domain, Application, Infrastructure, Api) con schema `maintenance` (ADR-0010, ADR-0013).
- Agregado **`RoutineType`**: matriz de parámetros (intervalos por defecto y % «por vencer») editable desde la web; un cambio reevalúa de inmediato las rutinas de ese tipo (ADR-0019, A-02).
- Agregado **`MaintenancePlan`** (identidad = `VehicleId`) con odómetro, fecha de la última revisión por tiempo y sus **`MaintenanceRoutine`**: dos dimensiones de estado (activación `RoutineState` y vencimiento `RoutineDueStatus`), ciclo, ajustes por rutina y aviso único por ciclo (ADR-0011, ADR-0020, D-08).
- Evaluación por **km y por tiempo con igual prioridad**: vence lo primero y la alerta indica `Distance`, `Time` o `DistanceAndTime` (RN-1, A-04). «Por vencer» al % efectivo del intervalo (RN-2). Salto directo a vencida solo emite la vencida (A-03). `Overdue` nunca retrocede por parámetros (A-16).
- **Ingesta HTTP de odómetro** con Anti-Corruption Layer, resolución por placa, monotonía y respuesta `202` con el resultado (ADR-0014). Vehículo desconocido: se ignora con log, sin reintento (C-03 de foundation).
- **Historial de lecturas** aceptadas e ignoradas y su consulta (ADR-0025).
- **Revisión diaria por tiempo** con scheduler a las 00:05 `America/Bogota` y endpoint `time-evaluations(asOfDate)` monótono (ADR-0017); el mismo command atiende el botón **«Actualizar»** (`reevaluations`, ADR-0019).
- **Activación de rutinas** desde el panel y **suspensión del plan** al inactivarse el vehículo (ADR-0020, A-17, A-18).
- Agregado **`Alert`** con ciclo `Active → Acknowledged → Resolved` y resolución automática (`EscalatedToOverdue`, `RoutineRestarted`, `ParametersChanged`, `RoutineDeactivated`) (ADR-0015, A-12).
- Suscriptor del EventBus: `VehicleRegistered` crea el plan, `VehicleStatusChanged` suspende o reanuda, `ServiceOrderCompleted` reinicia la rutina (RN-5, A-08).
- Endpoint masivo `POST /api/v1/maintenance/routines/bulk?dryRun=` para la carga inicial (ADR-0022), con línea base del primer ciclo (A-01).
- Consultas del CRM: plan con pistas km/tiempo, resumen de planes, lecturas, alertas y tipos de rutina.
- `samples/http/odometer-readings.http` y `samples/http/time-evaluations.http` reemplazan a `samples/kafka/*.jsonl` (C-01 de foundation).

## Capabilities

### New Capabilities

- `routine-type-catalog`: matriz de tipos de rutina con intervalos por defecto y % «por vencer», editable con efecto inmediato.
- `maintenance-plan-evaluation`: reglas de vencimiento por km y tiempo (RN-1, RN-2), emisión del vencimiento (RN-3), reinicio de ciclo (RN-5) y suspensión del plan.
- `routine-administration`: alta masiva de rutinas con simulación, ajustes por rutina, activación y consulta del plan.
- `odometer-ingestion`: recepción de lecturas de telemetría con ACL, monotonía (RN-6) e historial.
- `time-evaluation`: revisión diaria por tiempo, disparo manual por fecha y reevaluación a demanda.
- `maintenance-alerts`: ciclo de vida de las alertas, unicidad por ciclo y bandeja del CRM.

### Modified Capabilities

(ninguna)

## Impact

- Código nuevo: `src/Services/Maintenance/*`, `tests/Services/Maintenance/*`, `tests/RutaViva.Maintenance.AcceptanceTests` (recortable).
- Datos: schema `maintenance` (`RoutineTypes`, `MaintenancePlans`, `MaintenanceRoutines`, `Alerts`, `OdometerReadings`, `OutboxMessages`, `EventCheckpoints`), con semilla de tipos de rutina.
- Eventos producidos: stream `maintenance.routine` y `maintenance.alert`. Consumidos: `fleet.vehicle` y `serviceorders.service-order`.
- Muestras: `samples/http/*.http` nuevos y sección 3 de `samples/README.md` actualizada a HTTP.
- Consumidores aguas abajo: `service-orders-core` (orden automática y réplica de rutinas), `edge-gateway-notifications`, `reporting`, `data-import` y `fleet-maintenance-ui`.

## Fuera de alcance

- Creación de órdenes de servicio (`service-orders-core`).
- Envío real de avisos: basta con publicar el evento (enunciado).
- Alta individual de rutinas desde la UI (solo carga masiva; el mockup no la incluye).
- Resolución manual de alertas (ADR-0015, YAGNI) y retención del historial de lecturas (ADR-0025 NEG-001).
- Reordenar lecturas por fecha (ADR-0014 ALT-003).

**Recortable** (orden del análisis §10): pruebas de aceptación Reqnroll de RN-1, RN-2, RN-5 y RN-6.
