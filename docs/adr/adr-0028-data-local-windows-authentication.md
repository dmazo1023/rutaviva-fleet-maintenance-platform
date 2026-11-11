---
title: "[ADR-0028] Conectar los servicios a SQL Server local con autenticación de Windows y conservar los usuarios por servicio como control verificable"
status: "Proposed"
date_created: "2026-11-11"
authors:
  - "Daniel Mazo <daniel_mazo>"
tags:
  - "data"
  - "sql-server"
  - "security"
  - "local-development"
related_specs:
  - "openspec/changes/foundation-platform"
related_plans: []
supersedes: ""
superseded_by: ""
---

> Precisa la decisión del ADR-0013 («un usuario de base de datos por servicio») para el entorno local. En un entorno desplegado sigue aplicando el ADR-0013 tal cual.

## Contexto

- El ADR-0013 define un usuario SQL por servicio (`rutaviva_{schema}_app`) con `GRANT` sobre su schema y `DENY` sobre los demás, para imponer la propiedad de los datos en el motor.
- El SQL Server 2022 local del equipo de desarrollo y de la sustentación acepta **solo autenticación de Windows** (`SERVERPROPERTY('IsIntegratedSecurityOnly') = 1`). Los logins SQL se crean, pero no pueden iniciar sesión.
- Activar el modo mixto exige cambiar la configuración del servidor y reiniciar el servicio en cada equipo donde se ejecute la demo.
- El script `deploy/local/sql/01-schemas-and-users.sql` ya se verificó con `EXECUTE AS USER`: `rutaviva_fleet_app` recibe el error 229 al leer el schema `maintenance`.

## Decisión

- En local, **todos los servicios se conectan con autenticación de Windows**, con la cadena `Server=localhost;Database=RutaViva;Trusted_Connection=True;TrustServerCertificate=True;`. No hay contraseñas en ningún archivo ni en *user-secrets*.
- Cada servicio sigue usando **solo su schema** (`HasDefaultSchema`, un `DbContext` por servicio, su propia tabla de historial de migraciones), y las pruebas de arquitectura impiden que un servicio referencie a otro (ADR-0010 IMP-004).
- Los usuarios por servicio del ADR-0013 se siguen creando con el script, como **control verificable**. Una prueba de integración comprueba con `EXECUTE AS USER` que cada usuario solo accede a su schema.
- La cadena de conexión se lee de `ConnectionStrings:RutaViva` (o del recurso `rutaviva-sql` de Aspire), de modo que un entorno con modo mixto o en la nube cambia solo la configuración: `User Id=rutaviva_{schema}_app;Password=…`.

## Consecuencias

| ID | Consecuencia | Tipo |
|---|---|---|
| POS-001 | La solución arranca en cualquier equipo con SQL Server sin tocar su configuración ni manejar contraseñas | Positive |
| POS-002 | El aislamiento por schema sigue demostrable: la prueba con `EXECUTE AS USER` falla si un usuario puede leer un schema ajeno | Positive |
| POS-003 | Pasar a usuarios por servicio en ejecución es solo cambiar la cadena de conexión | Positive |
| NEG-001 | En local, el motor no impide que un servicio mal configurado lea un schema ajeno: la protección en ejecución la dan el código (un `DbContext` por schema) y las pruebas de arquitectura | Negative |

## Alternativas consideradas

### ALT-001 — Activar el modo mixto de autenticación

**Descripción**: Cambiar el servidor a autenticación SQL y Windows, y conectar cada servicio con su usuario.

**Rejection rationale**: Requiere modificar la configuración del servidor y reiniciar SQL Server en cada equipo de la demo, con riesgo el día de la sustentación. El aislamiento ya se verifica con `EXECUTE AS USER`.

### ALT-002 — Un usuario de Windows por servicio

**Descripción**: Cuentas de Windows distintas para cada proceso.

**Rejection rationale**: Desproporcionado para un entorno local: exige crear cuentas del sistema operativo y ejecutar cada servicio con una identidad distinta.

## Notas de implementación

| ID | Nota |
|---|---|
| IMP-001 | `appsettings.Development.json` de cada servicio define `ConnectionStrings:RutaViva` con la cadena de Windows; no contiene secretos, así que puede versionarse |
| IMP-002 | `01-schemas-and-users.sql` sigue pidiendo contraseñas por variables `sqlcmd`; en local pueden ser valores aleatorios porque esos logins no se usan para conectarse |
| IMP-003 | Prueba de integración `SchemaIsolationTests` en `tests/RutaViva.Architecture.Tests` o en el proyecto de integración de BuildingBlocks: ejecuta el script en una BD desechable y verifica con `EXECUTE AS USER` el acceso propio y el error 229 en los schemas ajenos |

## Referencias

| ID | Referencia |
|---|---|
| REF-001 | adr-0013-data-sqlserver-schema-per-service |
| REF-002 | Microsoft Learn: *Choose an authentication mode* y *EXECUTE AS (Transact-SQL)* |
