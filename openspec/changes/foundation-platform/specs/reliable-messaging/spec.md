## Purpose

Garantizar que cada cambio de estado de un servicio produzca exactamente sus eventos de integración, y que cada suscriptor aplique cada evento exactamente una vez aunque el transporte entregue duplicados o el suscriptor se reinicie (ADR-0011, ADR-0024).

## ADDED Requirements

### Requirement: Publicación atómica con el cambio de estado
Un servicio SHALL registrar sus eventos de integración en su propio Outbox dentro de la misma transacción que el cambio de estado que los origina. Si la transacción falla, MUST NOT quedar ningún evento pendiente de publicar.

#### Scenario: Transacción confirmada
- **WHEN** un servicio confirma un cambio de un agregado que genera 2 eventos de integración
- **THEN** el Outbox contiene 2 mensajes pendientes en la misma confirmación

#### Scenario: Transacción revertida
- **WHEN** la transacción del cambio falla después de generar los eventos
- **THEN** el Outbox no contiene ninguno de esos mensajes

### Requirement: Eventos de dominio despachados antes de confirmar
Dentro de un servicio, los eventos de dominio SHALL procesarse antes de confirmar, en la misma transacción, de modo que los efectos internos (por ejemplo, crear una alerta) y el Outbox se confirmen juntos con el cambio que los originó.

#### Scenario: Efecto interno atómico
- **WHEN** un agregado emite un evento de dominio cuyo handler crea otro agregado del mismo servicio y la transacción falla
- **THEN** no se persiste ni el cambio original, ni el agregado creado, ni el mensaje del Outbox

### Requirement: Relay del Outbox con entrega al menos una vez
El relay SHALL enviar al EventBus los mensajes pendientes en orden de creación y SHALL marcarlos como enviados solo después de recibir el `Offset`. Un fallo de envío MUST dejar el mensaje pendiente, incrementar su contador de intentos y registrar el último error.

#### Scenario: EventBus caído
- **WHEN** el EventBus no está disponible durante 3 ciclos del relay y luego vuelve
- **THEN** el mensaje se publica una sola vez en el log y su contador de intentos refleja los fallos previos

### Requirement: Consumo secuencial e idempotente con checkpoint
Cada suscriptor SHALL procesar los eventos de sus streams de uno en uno, en orden de offset, y SHALL guardar su checkpoint (`LastOffset`) en la misma transacción que el efecto del evento. Un evento con offset menor o igual al checkpoint MUST omitirse sin efectos.

#### Scenario: Evento entregado cinco veces
- **WHEN** el mismo evento llega 5 veces a un suscriptor
- **THEN** su efecto se aplica exactamente una vez

#### Scenario: Suscriptor detenido que se pone al día
- **WHEN** un suscriptor está detenido mientras se publican 3 eventos de sus streams y luego arranca
- **THEN** procesa los 3 eventos en orden de offset exactamente una vez

#### Scenario: Aviso perdido
- **WHEN** un suscriptor no recibe el aviso de un evento nuevo
- **THEN** lo procesa a más tardar en el siguiente sondeo de respaldo de 5 segundos

### Requirement: Reintentos acotados y avance tras fallo definitivo
Si el handler de un evento falla, el suscriptor SHALL reintentarlo 3 veces con espera creciente (1 s, 2 s y 4 s). Si sigue fallando, MUST reportar el fallo al EventBus y avanzar su checkpoint para no bloquear el stream.

#### Scenario: Handler que siempre falla
- **WHEN** un handler lanza un error en los 4 intentos de un evento
- **THEN** existe una *dead letter* para ese suscriptor y offset, y el siguiente evento del stream se procesa

### Requirement: Suscripción desde el último offset para avisos en vivo
Un suscriptor configurado como de solo avisos en vivo SHALL comenzar desde el último offset del log al arrancar, sin reprocesar eventos antiguos.

#### Scenario: Notifications arranca tarde
- **WHEN** Notifications arranca con 100 eventos ya en el log
- **THEN** no retransmite ninguno de esos 100 y retransmite el siguiente que se publique

### Requirement: Contratos de integración versionados y serializados como texto
Todo evento de integración SHALL viajar en un envelope CloudEvents 1.0 con `type = rutaviva.{service}.{EventName}.v{n}` y `subject` = identificador del agregado. Los enums MUST serializarse por nombre. Un cambio incompatible de un contrato MUST publicarse como una versión nueva.

#### Scenario: Snapshot de contrato
- **WHEN** cambia el nombre o el tipo de un campo de un evento `v1`
- **THEN** la prueba de contrato del evento falla contra su snapshot aprobado
