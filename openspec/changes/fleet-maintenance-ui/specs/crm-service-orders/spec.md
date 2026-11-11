## Purpose

Permitir que el aprobador y el taller muevan cada orden de servicio por su ciclo de vida sin salir de la pantalla, con las acciones que el backend autoriza y viendo los cambios del otro actor en tiempo real (ADR-0003, ADR-0021).

## ADDED Requirements

### Requirement: Bandeja de órdenes priorizada
El CRM SHALL listar las órdenes del cliente con número, placa, rutina, estado, total y fecha, filtrables por Por aprobar, Abiertas, Cerradas y Todas (filtro en la URL). El orden MUST ser: aprobaciones prioritarias → por aprobar → abiertas → cerradas, y dentro de cada grupo por antigüedad. Las prioritarias MUST mostrar el ícono `priority_high` y el texto «Vehículo detenido en el taller».

#### Scenario: Prioritaria primero
- **WHEN** hay una orden en `PendingApproval` por servicios adicionales y otra por cotización más antigua
- **THEN** la de servicios adicionales aparece primero, marcada como prioritaria

#### Scenario: Nada por aprobar
- **WHEN** el filtro es Por aprobar y no hay órdenes en `PendingApproval`
- **THEN** veo «Nada por aprobar»

### Requirement: Detalle de la orden
El detalle SHALL mostrar un indicador de 6 fases (Creada, Cotizada/Por aprobar, Aprobada, En sitio, En proceso, Finalizada) con la fase actual marcada con `aria-current="step"`, las líneas de servicio (tipo, quién la pidió, estado de aprobación y valor), el total, la marca «Aprobación automática» si aplica, la cita y el historial plegado. Los estados En pausa y Cancelada MUST mostrarse como aviso sobre el indicador.

#### Scenario: Orden aprobada automáticamente
- **WHEN** el taller cotiza hasta el monto vigente
- **THEN** la orden queda «Aprobada» con la pastilla «Aprobación automática»

#### Scenario: Orden en pausa
- **WHEN** el vehículo de la orden se desactiva
- **THEN** el detalle muestra el aviso «En pausa» con el estado al que volverá

#### Scenario: Esperando cotización
- **WHEN** la orden está en `Created`
- **THEN** la sección de líneas muestra «Esperando cotización del taller»

### Requirement: Acciones guiadas por el backend
La barra de acciones SHALL mostrar únicamente las acciones presentes en `availableActions` de la orden para el perfil activo; el CRM MUST NOT calcular las transiciones permitidas. Cuando una acción esté visible pero bloqueada, MUST mostrarse deshabilitada con el motivo enlazado por `aria-describedby`. «Cancelar orden» va a la izquierda y la acción principal a la derecha.

#### Scenario: Aprobación bloqueada por precio
- **WHEN** el perfil es Aprobador y la orden en `PendingApproval` tiene una línea `PendingPricing`
- **THEN** «Aprobar» está deshabilitado y se lee «Esperando precio del taller»

#### Scenario: En sitio sin cita
- **WHEN** el perfil es Taller y la orden está en `Approved` sin cita
- **THEN** «En sitio» está deshabilitado y se lee «Sin cita»

#### Scenario: Taller sin cancelar
- **WHEN** el perfil es Taller
- **THEN** la acción «Cancelar orden» no aparece en ninguna orden

### Requirement: Acciones con datos
Las acciones Cotizar, Pedir servicio, Agregar servicio, Guardar precio, Agendar/Reprogramar cita y Finalizar SHALL abrir un formulario con validación al enviar y llamar a su endpoint de `/api/v1/service-orders/{id}/...`. Finalizar MUST pedir el km y la fecha reales del servicio.

#### Scenario: Cotizar por encima del monto vigente
- **WHEN** el taller cotiza 2.000.000 COP con un monto vigente de 1.500.000
- **THEN** la orden pasa a «Pendiente de aprobación»

#### Scenario: Finalizar con km y fecha
- **WHEN** el taller finaliza con 55.100 km y la fecha de hoy
- **THEN** la orden queda «Finalizada» y, sin recargar, la rutina del vehículo vuelve a «Al día»

#### Scenario: Agregar servicio en sitio
- **WHEN** el taller agrega un servicio con precio en una orden `OnSite`
- **THEN** la orden pasa a «Pendiente de aprobación» con prioridad alta

### Requirement: Acciones destructivas con impacto
Rechazar y Cancelar orden SHALL pedir confirmación mostrando su impacto, con el botón destructivo y «Volver» como secundario.

#### Scenario: Rechazar una cotización
- **WHEN** el aprobador rechaza una orden por cotización
- **THEN** el diálogo indica «La orden se cancela» y «Podrás crear una orden manual»

#### Scenario: Rechazar servicios adicionales
- **WHEN** el aprobador rechaza una orden por servicios adicionales con 1 línea pendiente
- **THEN** el diálogo indica «1 servicio(s) se descartan» y «La orden sigue en sitio con lo aprobado»; al confirmar, solo esa línea queda tachada y la orden vuelve a «En sitio»

### Requirement: Órdenes en vivo
Cuando llega por la conexión en vivo un cambio de estado, de líneas, de cita o una solicitud de aprobación de una orden del cliente activo, el CRM SHALL actualizar la fila de la bandeja, el detalle abierto y el contador de por aprobar sin recargar. Una solicitud de prioridad alta MUST mostrarse como aviso persistente hasta que el usuario lo cierre o abra la orden.

#### Scenario: Aprobación prioritaria
- **WHEN** el perfil es Aprobador y el taller agrega un servicio en sitio
- **THEN** aparece un aviso persistente prioritario con el enlace «Ver» a la orden

#### Scenario: El taller cotiza mientras el aprobador mira
- **WHEN** el aprobador tiene abierta una orden en `Created` y el taller la cotiza
- **THEN** el detalle muestra las líneas, el total y las nuevas acciones sin recargar
