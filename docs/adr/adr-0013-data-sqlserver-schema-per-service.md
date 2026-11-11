---
title: "[ADR-0013] Persistir en SQL Server local con una base de datos, un schema y un usuario de mínimo privilegio por servicio"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "data"
  - "sql-server"
  - "ef-core"
  - "schema-per-service"
related_specs: []
related_plans: []
supersedes: "adr-0005"
superseded_by: ""
---

> **Enmienda 2026-11-10:** las pruebas de integración usan el SQL Server local con una BD desechable por fixture en lugar de Testcontainers (ADR-0027).

## Contexto

Este ADR reemplaza al ADR-0005, que proponía persistencia en memoria.

- El equipo dispone de **SQL Server en localhost** y considera el **modelado de la base de datos** un entregable valorado.
- Hay 5 servicios de negocio, cada uno dueño de sus datos (ADR-0010).
- El equipo prefiere **una sola base de datos con un schema por servicio** para simplificar el entorno local.
- En una base compartida, nada impide técnicamente que un servicio lea o escriba las tablas de otro (acoplamiento por base de datos, el antipatrón *shared database integration*).
- RN-2 y RN-3 requieren unicidad condicional (una alerta por ciclo y tipo; una orden abierta por rutina). SQL Server lo soporta con **índices únicos filtrados**.
- El Outbox y el checkpoint del suscriptor (ADR-0024) requieren transacciones locales en la misma base que el agregado.
- El EventBus (ADR-0024) necesita persistir su log de eventos.

## Decisión

Usaremos **SQL Server** con **EF Core 10** y la siguiente topología:

- Una base de datos `RutaViva`, con un schema por servicio: `customers`, `workshops`, `fleet`, `maintenance` y `serviceorders`, más los de soporte y técnicos `imports`, `reporting` y `eventbus` (log de eventos y *dead letters*). Notifications y Gateway no tienen schema.
- **Un usuario de base de datos por servicio** (`rutaviva_customers_app`, etc.) con `SELECT, INSERT, UPDATE, DELETE` **solo sobre su schema** y `DENY` explícito sobre los demás. La propiedad de los datos se impone en el motor, no solo por convención.
- Un `DbContext` por servicio con `HasDefaultSchema("{schema}")` y su propia tabla de historial de migraciones `{schema}.__EFMigrationsHistory`. Las migraciones se aplican con un usuario administrativo distinto del de ejecución.
- Concurrencia optimista con `rowversion` en cada raíz de agregado.
- Los estados se guardan como `varchar(20)` con `CHECK` (legibles en consultas y en la demo), y el dinero como `decimal(18,2)` con `char(3)` para la moneda.
- El diseño completo, sus justificaciones y el script de referencia están en [`data-model.md`](../architecture/data-model.md) y en [`rutaviva-reference-schema.sql`](../architecture/database/rutaviva-reference-schema.sql).

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Si un servicio intenta consultar el schema de otro, recibe el error 229 (*permission denied*): el acoplamiento por base de datos es detectable en el primer intento | Positive |
| POS-002 | Hay un solo servidor y una sola cadena de conexión base que levantar en local. Basta un `CREATE DATABASE` más el script de usuarios | Positive |
| POS-003 | Las invariantes de unicidad de RN-2 y RN-3 se refuerzan en el motor con 2 índices únicos filtrados | Positive |
| POS-004 | Separar un schema en su propia base de datos más adelante solo exige cambiar la cadena de conexión del servicio, sin cambios de código (no hay *joins* entre schemas) | Positive |
| NEG-001 | Comparten el mismo recurso de cómputo y el mismo punto único de falla: si SQL Server cae, caen los 5 servicios. Es aceptable en local, pero en producción se separarían las bases | Negative |
| NEG-002 | El manejo de usuarios y secretos por servicio agrega 5 credenciales que gestionar (en *user-secrets* en local, nunca en el repositorio) | Negative |
| NEG-003 | Los `uniqueidentifier` como clave agrupada fragmentan el índice. `Guid.CreateVersion7()` no es secuencial para el ordenamiento de SQL Server. Es aceptable en este volumen y se documenta como punto de evolución | Negative |

## Alternativas consideradas

### ALT-001 — Una base de datos por servicio

**Descripción**: `RutaViva_Customers`, `RutaViva_Fleet`, etc., en el mismo servidor.

**Rejection rationale**: Da mayor aislamiento, pero el equipo eligió el schema por servicio por simplicidad del entorno local. Con usuarios restringidos por schema se obtiene el mismo aislamiento lógico. Queda como la evolución natural (POS-004).

### ALT-002 — Una base de datos compartida sin restricciones de permisos

**Descripción**: Los schemas existen, pero todos los servicios usan el mismo usuario.

**Rejection rationale**: La separación sería solo una convención. Un `JOIN` entre `fleet.Vehicles` y `serviceorders.ServiceOrders` compilaría y funcionaría, lo que introduce acoplamiento oculto.

### ALT-003 — PostgreSQL

**Descripción**: Usar PostgreSQL con índices parciales.

**Rejection rationale**: El equipo tiene SQL Server listo en localhost. Ambos motores soportan los índices requeridos, así que cambiar de motor no aporta valor al reto.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | Las cadenas de conexión se guardan en `dotnet user-secrets` por servicio o como parámetros del AppHost de Aspire. Ninguna contraseña se escribe en `appsettings*.json` ni en scripts versionados (los scripts usan variables `sqlcmd` del tipo `$(CustomersAppPassword)`) |
| IMP-002 | `EnableRetryOnFailure()` del proveedor de SQL Server está activo. Las transacciones del Outbox usan `CreateExecutionStrategy()` para ser compatibles con los reintentos |
| IMP-003 | La fuente de verdad del esquema son las migraciones de EF Core. El script de referencia documenta el diseño y se valida con `dotnet ef migrations script` antes de la entrega |
| IMP-004 | Las pruebas de integración usan Testcontainers con `mcr.microsoft.com/mssql/server:2022-latest` y aplican las migraciones al iniciar |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | Microsoft Learn: *Create filtered indexes* (SQL Server) |
| REF-002 | Microsoft Learn: *EF Core: Connection resiliency* y *Default schema* |
| REF-003 | Richardson, C. *Pattern: Database per service* (variante *schema per service*) |
| REF-004 | adr-0005-data-persistence-ports-in-memory: decisión reemplazada |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0013` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
