## Purpose

Permitir reproducir en la sustentación la telemetría y la revisión diaria contra los endpoints reales de Maintenance, para mostrar RN-2, RN-3 y RN-6 sin herramientas externas (ADR-0014, ADR-0017).

## ADDED Requirements

### Requirement: Emitir una lectura de odómetro
El CRM SHALL permitir enviar una lectura con placa, km y fecha de lectura a `POST /api/v1/maintenance/odometer-readings`, y MUST mostrar el resultado devuelto (`Accepted` o `Ignored` con su motivo).

#### Scenario: Lectura aceptada
- **WHEN** envío RVA101 con 53.000 km
- **THEN** veo «Aceptada» y, sin recargar, el odómetro de RVA101 en la flota pasa a 53.000

#### Scenario: Lectura repetida
- **WHEN** vuelvo a enviar RVA101 con 53.000 km
- **THEN** veo «Ignorada: no es mayor que el odómetro actual»

#### Scenario: Vehículo desconocido
- **WHEN** envío XYZ999 con 15.000 km
- **THEN** veo «Ignorada: vehículo desconocido»

### Requirement: Ejecutar la revisión por fecha
El CRM SHALL permitir ejecutar la revisión de vencimientos por tiempo con una fecha elegida a través de `POST /api/v1/maintenance/time-evaluations` y MUST mostrar el resumen devuelto (planes evaluados, por vencer, vencidas y órdenes solicitadas).

#### Scenario: Revisión al 2027-01-20
- **WHEN** ejecuto la revisión con la fecha 2027-01-20 después de la carga inicial
- **THEN** veo el resumen con las rutinas vencidas por tiempo y las órdenes solicitadas

### Requirement: Simuladores solo para el Operador
Los simuladores SHALL estar disponibles solo con el perfil Operador.

#### Scenario: Aprobador sin simuladores
- **WHEN** el perfil es Aprobador
- **THEN** los simuladores no aparecen
