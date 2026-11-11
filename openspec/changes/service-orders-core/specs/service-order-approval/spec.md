## Purpose

Asegurar que los costos altos y cualquier ampliación del alcance de una orden pasen por una decisión del dueño del vehículo o de su responsable, priorizando las solicitudes que tienen el vehículo detenido en el taller (RN-4, A-10, A-11, A-15).

## ADDED Requirements

### Requirement: Autorización del aprobador
Solo SHALL aprobar, rechazar, pedir servicios adicionales o cancelar un aprobador **activo** que pertenezca al cliente dueño del vehículo **o** que sea el responsable asignado al vehículo. Cualquier otro MUST recibir `422` con `code = "ServiceOrder.ApproverNotAuthorized"` y la orden no cambia.

#### Scenario: Aprobador del dueño
- **WHEN** un aprobador activo del cliente dueño aprueba una orden `PendingApproval`
- **THEN** la orden queda `Approved` y guarda el aprobador que la aprobó

#### Scenario: Responsable de otro cliente
- **WHEN** el responsable asignado al vehículo, que pertenece a otro cliente, aprueba la orden
- **THEN** la orden queda `Approved`

#### Scenario: Aprobador de otro cliente
- **WHEN** un aprobador activo de otro cliente, que no es el responsable, aprueba la orden
- **THEN** la respuesta es `422` con `code = "ServiceOrder.ApproverNotAuthorized"`

#### Scenario: Aprobador inactivo
- **WHEN** un aprobador del dueño cuyo estado replicado es `Inactive` aprueba la orden
- **THEN** la respuesta es `422` con `code = "ServiceOrder.ApproverNotAuthorized"`

### Requirement: Resultado de la aprobación según el motivo
Una orden `PendingApproval` SHALL resolverse según su motivo: con motivo `Quote`, aprobar la deja `Approved` y rechazar la deja `Cancelled` con `cancellationReason = QuoteRejected`; con motivo `AdditionalServices`, aprobar marca las líneas adicionales pendientes como `Approved` y rechazar las marca como `Rejected`, y en ambos casos la orden vuelve a `OnSite` sin cancelarse.

#### Scenario: Rechazo de la cotización
- **WHEN** un aprobador autorizado rechaza una orden `PendingApproval` con motivo `Quote`
- **THEN** la orden queda `Cancelled` con `cancellationReason = QuoteRejected`

#### Scenario: Rechazo de servicios adicionales
- **WHEN** un aprobador autorizado rechaza una orden `PendingApproval` con motivo `AdditionalServices` y una línea adicional de 300.000 COP
- **THEN** la orden vuelve a `OnSite`, solo esa línea queda `Rejected`, la línea de rutina sigue `Approved` y el total excluye los 300.000 COP

#### Scenario: Aprobación de servicios adicionales
- **WHEN** un aprobador autorizado aprueba una orden `PendingApproval` con motivo `AdditionalServices`
- **THEN** la orden vuelve a `OnSite` y las líneas adicionales quedan `Approved`

### Requirement: Aprobación bloqueada por líneas sin precio
Mientras la orden tenga alguna línea con `pricingStatus = PendingPricing`, la aprobación MUST rechazarse con `422` y `code = "ServiceOrder.PendingPricingLines"`. El rechazo y la cancelación siguen permitidos.

#### Scenario: Aprobar con un servicio sin cotizar
- **WHEN** un aprobador aprueba una orden con una línea adicional pedida y aún sin precio
- **THEN** la respuesta es `422` con `code = "ServiceOrder.PendingPricingLines"` y la orden sigue `PendingApproval`

#### Scenario: Aprobar después de que el taller cotiza
- **WHEN** el taller pone precio a la línea pendiente y luego el aprobador aprueba
- **THEN** la orden queda `Approved` y el total incluye la línea adicional

### Requirement: Servicios adicionales pedidos por el aprobador (Recortable)
Con la orden en `PendingApproval` y motivo `Quote`, un aprobador autorizado SHALL poder pedir un servicio adicional con su descripción; la línea queda `PendingPricing`, pedida por `Approver`. El taller asignado SHALL ponerle un valor mayor que 0 y el total se recalcula. Con motivo `AdditionalServices` MUST rechazarse con `ServiceOrder.InvalidTransition`.

#### Scenario: Pedir y cotizar un servicio adicional
- **WHEN** el aprobador pide «Alineación» y el taller le pone 120.000 COP
- **THEN** la orden sigue `PendingApproval`, la línea queda `Priced` y se publican dos `ServiceOrderLinesChanged`

#### Scenario: Poner precio a una línea ya cotizada
- **WHEN** el taller intenta poner precio a una línea que ya está `Priced`
- **THEN** la respuesta es `422` con `code = "ServiceOrder.LineAlreadyPriced"`

### Requirement: Servicios adicionales agregados en sitio con prioridad alta (Recortable)
Solo con la orden en `OnSite`, el taller asignado SHALL poder agregar un servicio adicional con descripción y valor mayor que 0. La orden MUST pasar a `PendingApproval` con motivo `AdditionalServices` y prioridad `High`, **sin importar el monto ni la política vigente**, y publicar `ServiceOrderApprovalRequested` con `Priority = High`.

#### Scenario: Servicio adicional de bajo valor
- **WHEN** el taller agrega en sitio un servicio de 50.000 COP con una política de 1.500.000 COP
- **THEN** la orden queda `PendingApproval` con `approvalReason = AdditionalServices` y `approvalPriority = High`

#### Scenario: Agregar servicio en Aprobada
- **WHEN** el taller intenta agregar un servicio a una orden `Approved`
- **THEN** la respuesta es `422` con `code = "ServiceOrder.InvalidTransition"`

#### Scenario: Agregar servicio en En proceso
- **WHEN** el taller intenta agregar un servicio a una orden `InProgress`
- **THEN** la respuesta es `422` con `code = "ServiceOrder.InvalidTransition"`

### Requirement: Bandeja de aprobaciones priorizada
`GET /api/v1/service-orders/approvals?approverId=` SHALL devolver las órdenes `PendingApproval` que ese aprobador puede decidir, ordenadas primero por prioridad (`High` antes que `Normal`) y luego por antigüedad de la última actualización (la más antigua primero).

#### Scenario: Orden de la bandeja
- **WHEN** existen una orden `Normal` actualizada a las 08:00, otra `Normal` a las 09:00 y una `High` a las 10:00
- **THEN** la bandeja las devuelve en el orden `High` 10:00, `Normal` 08:00, `Normal` 09:00

#### Scenario: Bandeja del responsable
- **WHEN** el responsable de un vehículo de otro cliente consulta su bandeja
- **THEN** ve las órdenes pendientes de ese vehículo y no las del resto de ese cliente
