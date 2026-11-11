## Purpose

Permitir que el negocio cambie desde la web, sin desplegar, el monto hasta el cual una cotización se aprueba automáticamente, conservando el historial y sin alterar las órdenes ya cotizadas (A-06, ADR-0019).

## ADDED Requirements

### Requirement: Política versionada de solo inserción
Cada cambio del monto de aprobación automática SHALL registrarse como una versión nueva con moneda, monto (≥ 0), fecha de vigencia y autor. Las versiones existentes MUST NOT modificarse ni borrarse. La política vigente para una moneda es la versión con la fecha de vigencia más reciente que no sea futura.

#### Scenario: Nueva política
- **WHEN** se envía `POST /api/v1/service-orders/approval-policies` con `{ currency: "COP", autoApprovalMaxAmount: 2000000, createdBy: "Laura Gómez" }`
- **THEN** se crea una versión vigente desde ahora y la anterior sigue en el historial

#### Scenario: Monto negativo
- **WHEN** se envía una política con monto −1
- **THEN** la respuesta es `400` y no se crea ninguna versión

#### Scenario: Política con vigencia futura
- **WHEN** se registra una política con vigencia a partir de mañana
- **THEN** las cotizaciones de hoy siguen usando la versión anterior

### Requirement: Aplicación desde la siguiente cotización
La cotización SHALL leer la política vigente en el momento de cotizar, sin caché, y la orden MUST guardar una copia del monto aplicado. Un cambio de política MUST NOT alterar el estado ni el monto aplicado de las órdenes ya cotizadas.

#### Scenario: Cambio de política después de cotizar
- **WHEN** una orden se cotizó en 1.800.000 COP con la política de 1.500.000 COP (quedó `PendingApproval`) y luego la política sube a 2.000.000 COP
- **THEN** la orden sigue `PendingApproval` con monto aplicado 1.500.000 COP

#### Scenario: Siguiente cotización con la política nueva
- **WHEN** después de ese cambio el taller cotiza otra orden en 1.800.000 COP
- **THEN** esa orden queda `Approved` con `autoApproved = true` y monto aplicado 2.000.000 COP

#### Scenario: Edición directa en SQL
- **WHEN** se inserta una versión nueva directamente en la base de datos
- **THEN** la siguiente cotización la aplica sin reiniciar el servicio

### Requirement: Política inicial y consulta del historial
El servicio SHALL iniciar con una política de 1.500.000 COP. `GET /api/v1/service-orders/approval-policies?currency=COP` SHALL devolver todas las versiones de la más reciente a la más antigua, marcando cuál está vigente.

#### Scenario: Sistema recién instalado
- **WHEN** se consulta el historial sin haber registrado políticas
- **THEN** se devuelve una única versión de 1.500.000 COP marcada como vigente
