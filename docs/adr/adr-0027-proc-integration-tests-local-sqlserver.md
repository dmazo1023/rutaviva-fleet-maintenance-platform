---
title: "[ADR-0027] Ejecutar las pruebas de integración contra el SQL Server local con una base de datos desechable por fixture"
status: "Proposed"
date_created: "2026-11-10"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "process"
  - "testing"
  - "sql-server"
related_specs:
  - "openspec/changes/foundation-platform"
related_plans: []
supersedes: ""
superseded_by: ""
---

> Enmienda el uso de Testcontainers del ADR-0009 (tabla de pirámide, fila `{Service}.Api.IntegrationTests`) y la nota IMP-004 del ADR-0013. El resto de ambos ADRs sigue vigente.

## Contexto

- El ADR-0009 y el ADR-0013 IMP-004 piden Testcontainers con la imagen `mcr.microsoft.com/mssql/server:2022-latest` para las pruebas de integración.
- El equipo de desarrollo no tiene Docker, y la sustentación se hace desde el portátil del candidato.
- El equipo sí tiene SQL Server 2022 Developer en `localhost` con autenticación de Windows, el mismo motor que usa la solución (ADR-0013).
- Unas 40 tareas de los changes se verifican con pruebas de integración contra SQL Server real: índices únicos filtrados, `CHECK` de enums, Outbox, checkpoints y concurrencia con `rowversion`. Una base en memoria no prueba ninguna de esas reglas.

## Decisión

Las pruebas de integración usarán **el SQL Server local con una base de datos desechable por fixture**, detrás de una única abstracción en `tests/RutaViva.Testing.Common`:

- `SqlServerTestDatabase : IAsyncLifetime` crea `RutaViva_Tests_{guid}` al iniciar, expone su cadena de conexión, aplica las migraciones del `DbContext` bajo prueba y la elimina al terminar (`ALTER DATABASE … SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE …`).
- La cadena base se lee de la variable de entorno `RUTAVIVA_TEST_SQL`. Su valor por defecto es `Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;`.
- Cada clase de pruebas usa su propia base (`IClassFixture`), así que las clases pueden ejecutarse en paralelo sin interferir.
- Las pruebas llevan `[Trait("Category", "Integration")]` para poder excluirlas en una ejecución rápida.
- Al arrancar, el fixture borra las bases `RutaViva_Tests_*` con más de 2 horas de antigüedad, para limpiar las que dejó una ejecución interrumpida.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | Las pruebas de integración corren hoy y en el portátil de la sustentación sin instalar Docker | Positive |
| POS-002 | Se prueba el mismo motor de producción, con sus índices filtrados, `CHECK` y `rowversion` | Positive |
| POS-003 | Una sola clase concentra la creación y limpieza de bases (DRY); cambiar a Testcontainers en el futuro solo cambia ese fixture | Positive |
| NEG-001 | Las pruebas dependen de un SQL Server local; en un servidor de CI hay que proveerlo (servicio de la plataforma de CI o Testcontainers detrás de la misma abstracción) | Negative |
| NEG-002 | Una ejecución interrumpida deja bases huérfanas hasta la siguiente limpieza | Negative |

## Alternativas consideradas

### ALT-001 — Instalar Docker Desktop y mantener Testcontainers

**Descripción**: Seguir el ADR-0009 tal cual.

**Rejection rationale**: Exige instalar y configurar Docker y WSL2 en ambos equipos con el plazo encima, sin mejorar lo que se prueba: el motor es el mismo.

### ALT-002 — Testcontainers con Podman

**Descripción**: El equipo tiene Podman 5.6; Testcontainers puede apuntar a su socket compatible con Docker.

**Rejection rationale**: Requiere configurar el socket y el modo *rootful* de Podman en Windows, con riesgo de fallas de infraestructura durante las olas de implementación. El SQL Server local ya está disponible.

### ALT-003 — EF Core InMemory o SQLite

**Descripción**: Proveedor en memoria para las pruebas.

**Rejection rationale**: No soporta índices únicos filtrados, `CHECK`, `rowversion` ni `SEQUENCE`, que son justamente las invariantes que prueban estas pruebas.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | `SqlServerTestDatabase` usa `Microsoft.Data.SqlClient`; no hay contraseñas en el código porque usa autenticación de Windows |
| IMP-002 | La prueba de humo de la Ola 0 crea, migra y borra una base con este fixture antes de lanzar agentes |
| IMP-003 | **Snapshots de contratos sin Verify.** Verify 33.x exige declarar una licencia de patrocinio (SponsorCheck SC021) para compilar. Las pruebas de contrato usan un helper propio `ContractSnapshot` en `RutaViva.IntegrationContracts.Tests`: serializa con las opciones JSON de la solución y compara con `Snapshots/{Evento}.approved.json` versionado; si el archivo no existe, lo crea y la prueba falla para que se revise. Mismo comportamiento que Verify para este uso, sin dependencia de licencia |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | adr-0009-proc-testing-strategy-fitness-functions |
| REF-002 | adr-0013-data-sqlserver-schema-per-service (IMP-004) |
| REF-003 | xUnit v3: *Shared context between tests* (`IClassFixture`, `IAsyncLifetime`) |

---

> **Immutability Notice**: Once this ADR reaches `Accepted` status, its Context, Decision, and Consequences sections must not be modified. If circumstances change and the decision is no longer valid, create a new ADR with `supersedes: adr-0027` pointing to this document, and update the `superseded_by` field above. The only mutable field on an accepted ADR is `superseded_by`.
