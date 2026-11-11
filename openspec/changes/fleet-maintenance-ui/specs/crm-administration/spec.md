## Purpose

Permitir que el negocio cambie desde la web, sin despliegues, los parámetros de mantenimiento por tipo de rutina y el monto de aprobación automática (ADR-0019, RQ-01 y RQ-02).

## ADDED Requirements

### Requirement: Matriz de tipos de rutina editable
El CRM SHALL listar los tipos de rutina con su código, nombre, intervalo de km por defecto, intervalo de meses por defecto y % «por vencer», y SHALL permitir editarlos con validación al enviar: % mayor que 0 y menor que 100, intervalos mayores que 0 y al menos un intervalo.

#### Scenario: Cambio del % de un tipo
- **WHEN** cambio el % de OIL_CHANGE de 10 a 15 y guardo
- **THEN** el CRM envía `PUT /api/v1/maintenance/routine-types/{id}` y muestra un aviso de éxito

#### Scenario: Sin intervalos
- **WHEN** intento guardar un tipo con ambos intervalos vacíos
- **THEN** veo el error «Indica al menos un intervalo» y no se envía la petición

### Requirement: Política de aprobación automática con historial
El CRM SHALL mostrar el monto vigente de aprobación automática y su historial (monto, vigente desde y autor), y SHALL permitir registrar un monto nuevo mayor o igual a 0, que aplica desde la siguiente cotización.

#### Scenario: Nuevo monto
- **WHEN** registro 2.000.000 COP
- **THEN** el CRM envía `POST /api/v1/service-orders/approval-policies`, el monto vigente pasa a 2.000.000 y el anterior queda en el historial

#### Scenario: Monto negativo
- **WHEN** intento registrar −1
- **THEN** veo un error de validación y no se envía la petición

### Requirement: Solo lectura fuera del perfil Operador
La edición de parámetros SHALL estar disponible solo para el perfil Operador; los demás perfiles MUST ver los valores en modo de solo lectura.

#### Scenario: Taller en Parámetros
- **WHEN** el perfil es Taller y abre `/parametros`
- **THEN** ve la matriz y la política sin botones de edición
