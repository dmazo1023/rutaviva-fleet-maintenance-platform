## Purpose

Recibir, guardar en orden y repartir los eventos de integración entre los microservicios de RutaViva sin un broker externo, de forma que ningún evento se pierda ni se duplique en el log (ADR-0024).

## ADDED Requirements

### Requirement: Publicación idempotente con offset ordenado
El EventBus SHALL guardar cada evento recibido en un log de solo inserción y SHALL responder al publicador con el `Offset` asignado. Si el `MessageId` ya existe, MUST responder con el `Offset` existente sin insertar otra fila. Los offsets MUST asignarse con un único escritor, de modo que el orden de confirmación coincida con el orden del `Offset`.

#### Scenario: Evento nuevo
- **WHEN** un servicio publica un envelope con un `MessageId` que no existe en el log
- **THEN** el EventBus inserta el evento y responde con un `Offset` mayor que cualquier offset confirmado antes

#### Scenario: Reintento del publicador
- **WHEN** un servicio publica dos veces el mismo envelope (mismo `MessageId`)
- **THEN** el log contiene una sola fila con ese `MessageId` y ambas respuestas devuelven el mismo `Offset`

#### Scenario: Publicaciones concurrentes
- **WHEN** 50 publicaciones distintas llegan en paralelo
- **THEN** los offsets resultantes son 50 valores distintos y consecutivos, y la lectura `afterOffset = N` nunca devuelve el offset `N + 2` sin devolver antes el `N + 1`

### Requirement: Envelope CloudEvents validado
El EventBus SHALL aceptar solo envelopes CloudEvents 1.0 con `id`, `source`, `type`, `subject`, `time` y `data` presentes, y con `type` en el formato `rutaviva.{service}.{EventName}.v{n}`. Un envelope inválido MUST rechazarse sin guardarse.

#### Scenario: Envelope sin tipo
- **WHEN** un publicador envía un envelope sin `type`
- **THEN** la publicación falla con un error de validación y el log no cambia

### Requirement: Aviso sin contenido a los suscriptores del stream
Después de guardar un evento, el EventBus SHALL avisar a los suscriptores conectados al stream del evento con el par `(stream, offset)`, sin el contenido del evento.

#### Scenario: Aviso a un suscriptor del stream
- **WHEN** se guarda un evento del stream `serviceorders.service-order` con offset 42
- **THEN** cada suscriptor conectado a ese stream recibe `EventsAvailable("serviceorders.service-order", 42)` y ningún suscriptor de otros streams lo recibe

### Requirement: Lectura por offset
El EventBus SHALL devolver, para una lista de streams y un `afterOffset`, los eventos con offset mayor en orden ascendente, hasta un máximo de 100 por lectura. También SHALL devolver el último offset confirmado del log.

#### Scenario: Recuperación de eventos atrasados
- **WHEN** un suscriptor pide `GetEvents(["fleet.vehicle"], afterOffset: 10, max: 100)` y existen los offsets 11, 12 (de `fleet.vehicle`) y 13 (de otro stream)
- **THEN** recibe solo los offsets 11 y 12, en ese orden

#### Scenario: Tope por lectura
- **WHEN** hay 250 eventos pendientes para un suscriptor
- **THEN** cada lectura devuelve como máximo 100 eventos

### Requirement: Registro de fallos definitivos
El EventBus SHALL registrar en un almacén de *dead letters* cada fallo reportado por un suscriptor, con el nombre del suscriptor, el offset, el error y la fecha.

#### Scenario: Suscriptor reporta un fallo
- **WHEN** un suscriptor reporta `ReportFailure("maintenance", 57, "Timeout")`
- **THEN** existe una *dead letter* con esos datos y el evento 57 sigue disponible en el log

### Requirement: Acceso solo desde la red interna
El hub del EventBus MUST exponerse únicamente bajo la ruta `/internal/**` y MUST NOT ser accesible a través del Gateway público.

#### Scenario: Acceso desde el Gateway
- **WHEN** un cliente pide `/internal/hubs/events` a través del Gateway
- **THEN** recibe `404 Not Found`
