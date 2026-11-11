# Registro de decisiones (Decision Log)

Decisiones arquitectónicas de **RutaViva CRM de Mantenimiento**. Contexto general en el [análisis del reto](../architecture/analisis-del-reto.md) y en el [modelo de datos](../architecture/data-model.md).

| Número | Título | Estado | Categoría | Fecha |
|---|---|---|---|---|
| [ADR-0001](adr-0001-arch-clean-modular-monolith.md) | Monolito modular con Clean Architecture y DDD táctico | Superseded por ADR-0010 | architecture | 2026-11-10 |
| [ADR-0002](adr-0002-arch-aggregate-boundaries.md) | Agregados `Vehicle` y `ServiceOrder` con consistencia eventual en proceso | Superseded por ADR-0011 | architecture | 2026-11-10 |
| [ADR-0003](adr-0003-arch-service-order-state-machine.md) | Máquina de estados de `ServiceOrder` (9 estados con En sitio), aprobación priorizada, líneas, política versionada y autorización | Proposed | architecture | 2026-11-10 |
| [ADR-0004](adr-0004-data-idempotent-odometer-ingestion.md) | Ingesta HTTP de odómetro idempotente y monótona | Superseded por ADR-0014 | data | 2026-11-10 |
| [ADR-0005](adr-0005-data-persistence-ports-in-memory.md) | Persistencia en memoria detrás de puertos | Superseded por ADR-0013 | data | 2026-11-10 |
| [ADR-0006](adr-0006-arch-daily-time-evaluation-host.md) | Revisión diaria como `BackgroundService` con reloj adelantable | Superseded por ADR-0017 | architecture | 2026-11-10 |
| [ADR-0007](adr-0007-int-integration-events-signalr.md) | Eventos de integración en proceso y SignalR | Superseded por ADR-0012 (a su vez reemplazado por ADR-0024) | integration | 2026-11-10 |
| [ADR-0008](adr-0008-tech-angular-22-frontend.md) | CRM web con Angular 22 (standalone, signals, zoneless) | Proposed | technology | 2026-11-10 |
| [ADR-0009](adr-0009-proc-testing-strategy-fitness-functions.md) | Pirámide de pruebas, Testcontainers, contratos y fitness functions | Proposed | process | 2026-11-10 |
| [ADR-0010](adr-0010-arch-microservices-by-bounded-context.md) | Microservicios por bounded context | Proposed | architecture | 2026-11-10 |
| [ADR-0011](adr-0011-arch-aggregates-cross-service-consistency.md) | Agregados por servicio y RN-3 en tres niveles | Proposed | architecture | 2026-11-10 |
| [ADR-0012](adr-0012-int-kafka-event-backbone-outbox-inbox.md) | Kafka con Outbox, Inbox y CloudEvents | Superseded por ADR-0024 | integration | 2026-11-10 |
| [ADR-0013](adr-0013-data-sqlserver-schema-per-service.md) | SQL Server: una BD, un schema y un usuario por servicio | Proposed | data | 2026-11-10 |
| [ADR-0014](adr-0014-data-odometer-ingestion-acl.md) | Ingesta de odómetro por HTTP en Maintenance con ACL y monotonía | Proposed | data | 2026-11-10 |
| [ADR-0015](adr-0015-arch-alert-lifecycle.md) | Ciclo de vida de las alertas en Maintenance | Proposed | architecture | 2026-11-10 |
| [ADR-0016](adr-0016-int-gateway-notifications-signalr.md) | Gateway YARP + Notifications SignalR alimentado por el EventBus | Proposed | integration | 2026-11-10 |
| [ADR-0017](adr-0017-arch-time-evaluation-trigger.md) | Revisión diaria con scheduler interno y endpoint `time-evaluations(asOfDate)` | Proposed | architecture | 2026-11-10 |
| [ADR-0018](adr-0018-proc-local-orchestration-aspire.md) | Orquestación local con .NET Aspire | Proposed | process | 2026-11-10 |
| [ADR-0019](adr-0019-data-business-parameters-in-database.md) | Parámetros de negocio en BD editables desde la web y reevaluación a demanda | Proposed | data | 2026-11-10 |
| [ADR-0020](adr-0020-arch-activation-states-suspension-cascade.md) | Estados de activación de vehículo y rutina con suspensión en cascada | Proposed | architecture | 2026-11-10 |
| [ADR-0021](adr-0021-arch-workshop-appointment-minimal.md) | Cita del taller como dato de la orden, sin agendamiento (extensión del alcance) | Proposed | architecture | 2026-11-10 |
| [ADR-0022](adr-0022-arch-data-import-module.md) | Servicio DataImport: carga inicial CSV con validación en 3 niveles y dry-run | Proposed | architecture | 2026-11-10 |
| [ADR-0023](adr-0023-arch-reporting-module-projections.md) | Servicio Reporting: proyecciones desde el EventBus y reportes Excel/CSV | Proposed | architecture | 2026-11-10 |
| [ADR-0024](adr-0024-int-eventbus-signalr-sql-log.md) | EventBus propio: SignalR para avisar y SQL Server como log de eventos (reemplaza a Kafka) | Proposed | integration | 2026-11-10 |
| [ADR-0025](adr-0025-data-odometer-reading-log.md) | Historial de lecturas de odómetro como log de solo inserción (enmienda ADR-0014 IMP-003) | Proposed | data | 2026-11-10 |
| [ADR-0026](adr-0026-proc-naming-conventions.md) | Nombres de proyectos, carpetas y aplicación web según el tipo de componente (`.Api`, `.Hub`, `ApiGateway`, `-ui`) | Proposed | process | 2026-11-10 |
| [ADR-0027](adr-0027-proc-integration-tests-local-sqlserver.md) | Pruebas de integración contra el SQL Server local con BD desechable (enmienda Testcontainers de ADR-0009 y ADR-0013) | Proposed | process | 2026-11-10 |
