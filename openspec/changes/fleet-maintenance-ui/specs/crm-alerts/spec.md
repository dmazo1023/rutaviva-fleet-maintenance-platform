## Purpose

Permitir al usuario del CRM gestionar las alertas de mantenimiento por km y por tiempo (activas, atendidas y resueltas) y marcar como atendidas las que ya revisó (ADR-0015).

## ADDED Requirements

### Requirement: Bandeja de alertas por estado
El CRM SHALL listar las alertas del cliente activo filtradas por Activas, Atendidas o Resueltas (filtro en la URL), con placa, rutina, tipo (por vencer o vencida), disparador (km, tiempo o ambos) y fecha, de la más reciente a la más antigua.

#### Scenario: Filtro por defecto
- **WHEN** abro `/alertas`
- **THEN** veo las alertas activas del cliente

#### Scenario: Disparador por km y tiempo
- **WHEN** una alerta vencida tiene disparador `DistanceAndTime`
- **THEN** su pastilla muestra los íconos de velocímetro y calendario con el texto accesible «Por km y tiempo»

#### Scenario: Sin alertas
- **WHEN** no hay alertas activas
- **THEN** veo «Todo en orden»

#### Scenario: Error de carga
- **WHEN** la API de alertas no responde
- **THEN** veo el mensaje de error con «Reintentar»

### Requirement: Atender una alerta
Para una alerta activa el CRM SHALL ofrecer la acción «Atender», que la marca como atendida registrando quién la atendió (texto del perfil activo, ADR-0015 IMP-002). Las alertas resueltas MUST mostrar el motivo de resolución y no ofrecer acciones.

#### Scenario: Atender
- **WHEN** pulso «Atender» en una alerta activa
- **THEN** el CRM envía `POST /api/v1/maintenance/alerts/{id}/acknowledge` y la alerta pasa a Atendidas

#### Scenario: Alerta resuelta por reinicio
- **WHEN** veo una alerta resuelta con motivo `RoutineRestarted`
- **THEN** se muestra «Resuelta: rutina reiniciada» sin acciones

### Requirement: Alertas en vivo
Cuando llega por la conexión en vivo una alerta nueva o un cambio de estado de alerta del cliente activo, el CRM SHALL actualizar la bandeja y el contador de alertas activas de la navegación sin recargar.

#### Scenario: Alerta escalada
- **WHEN** una alerta por vencer se resuelve por escalamiento y nace una vencida
- **THEN** la por vencer sale de Activas, la vencida aparece primero y el contador de la navegación no cambia
