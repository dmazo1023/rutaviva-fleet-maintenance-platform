## Purpose

Ofrecer un único origen HTTP y WebSocket para el CRM, el taller y la telemetría, que enruta cada petición a su servicio sin conocer reglas de negocio y que nunca expone los componentes internos (ADR-0016, ADR-0024 §8).

## ADDED Requirements

### Requirement: Enrutamiento por segmento de servicio
El Gateway SHALL reenviar cada petición cuya ruta empiece por `/api/v1/{segment}/` al servicio dueño del segmento, conservando el método, la ruta, la *query string*, los encabezados y el cuerpo. Los segmentos MUST ser exactamente: `customers` → Customers, `workshops` → Workshops, `fleet` → Fleet, `maintenance` → Maintenance, `service-orders` → ServiceOrders, `imports` → DataImport y `reports` → Reporting. Una ruta que no coincide con ningún segmento MUST responder `404`.

#### Scenario: Petición a Fleet
- **WHEN** un cliente pide `GET /api/v1/fleet/vehicles?status=Active` al Gateway
- **THEN** Fleet recibe `GET /api/v1/fleet/vehicles?status=Active` y el cliente recibe la respuesta de Fleet sin cambios

#### Scenario: Petición del taller a ServiceOrders
- **WHEN** el taller envía `POST /api/v1/service-orders/{id}/quote` con un cuerpo JSON
- **THEN** ServiceOrders recibe el mismo método, ruta y cuerpo

#### Scenario: Telemetría hacia Maintenance
- **WHEN** la plataforma de telemetría envía `POST /api/v1/maintenance/odometer-readings`
- **THEN** la petición llega a Maintenance

#### Scenario: Segmento desconocido
- **WHEN** un cliente pide `GET /api/v1/billing/invoices`
- **THEN** el Gateway responde `404` sin contactar a ningún servicio

#### Scenario: Carga de archivos de importación
- **WHEN** el CRM envía `POST /api/v1/imports` en `multipart/form-data` con 5 archivos que suman 25 MB
- **THEN** DataImport recibe la petición completa

### Requirement: Respuestas de los servicios sin alteración
El Gateway MUST NOT modificar el código de estado ni el cuerpo de las respuestas de los servicios, incluidos los `ProblemDetails`, ni combinar respuestas de varios servicios.

#### Scenario: Error de negocio de un servicio
- **WHEN** ServiceOrders responde `422` con un `ProblemDetails` cuyo `code` es `ServiceOrder.InvalidTransition`
- **THEN** el cliente recibe `422` con el mismo cuerpo

#### Scenario: Servicio destino caído
- **WHEN** Maintenance no está disponible y un cliente pide `GET /api/v1/maintenance/alerts`
- **THEN** el Gateway responde `502` y no reintenta la petición

### Requirement: Componentes internos inaccesibles
El Gateway MUST NOT reenviar ninguna ruta bajo `/internal/**`, en particular el hub del EventBus `/internal/hubs/events`.

#### Scenario: Intento de acceso al hub del EventBus
- **WHEN** un cliente pide `POST /internal/hubs/events/negotiate` al Gateway
- **THEN** el Gateway responde `404` y el EventBus no recibe la petición

### Requirement: Tiempo de espera extendido para la carga inicial
El Gateway SHALL aplicar un tiempo de espera de 120 segundos a las rutas `/api/v1/imports/**`, porque la confirmación de una carga es síncrona y procesa hasta 5 etapas (ADR-0022). Las demás rutas MUST conservar el tiempo de espera por defecto de 30 segundos.

#### Scenario: Confirmación de carga lenta
- **WHEN** el servicio DataImport tarda 60 segundos en responder `POST /api/v1/imports/{id}/confirm`
- **THEN** el Gateway entrega la respuesta del servicio sin cortar la petición

#### Scenario: Otro servicio lento
- **WHEN** el servicio Fleet tarda 45 segundos en responder
- **THEN** el Gateway responde `504` al cumplirse los 30 segundos

### Requirement: WebSockets hacia el hub de notificaciones
El Gateway SHALL reenviar `/hubs/**` a Notifications, incluida la negociación de SignalR y la actualización de la conexión a WebSocket.

#### Scenario: Conexión SignalR a través del Gateway
- **WHEN** el CRM abre una conexión SignalR a `/hubs/notifications` en el Gateway
- **THEN** la negociación responde `200` y la conexión se establece con transporte WebSocket

### Requirement: CORS para el origen de la SPA
El Gateway SHALL permitir peticiones de origen cruzado, con credenciales, solo desde los orígenes configurados para la SPA. Los orígenes MUST leerse de la configuración, nunca estar fijos en el código. Una petición desde un origen no configurado MUST NOT recibir encabezados `Access-Control-Allow-*`.

#### Scenario: Preflight desde la SPA
- **WHEN** el navegador envía `OPTIONS /api/v1/fleet/vehicles` con `Origin` igual al origen configurado de la SPA
- **THEN** la respuesta incluye `Access-Control-Allow-Origin` con ese origen y `Access-Control-Allow-Credentials: true`

#### Scenario: Origen no permitido
- **WHEN** llega una petición con `Origin: https://malicioso.example`
- **THEN** la respuesta no incluye `Access-Control-Allow-Origin`

### Requirement: Correlación de extremo a extremo
El Gateway SHALL garantizar que toda petición reenviada lleve `X-Correlation-Id`: reutiliza el recibido o genera uno nuevo. El mismo valor MUST llegar al servicio destino y volver en la respuesta al cliente.

#### Scenario: Petición sin correlación
- **WHEN** un cliente envía una petición sin `X-Correlation-Id`
- **THEN** el servicio destino recibe un `X-Correlation-Id` generado y el cliente recibe el mismo valor en la respuesta

#### Scenario: Petición con correlación
- **WHEN** un cliente envía `X-Correlation-Id: demo-001`
- **THEN** el servicio destino y la respuesta usan `demo-001`

### Requirement: Salud propia del Gateway
El Gateway SHALL exponer `/health` y `/alive` sobre su propio proceso, sin consultar la salud de los servicios destino.

#### Scenario: Servicio destino caído
- **WHEN** Reporting está caído
- **THEN** `/health` del Gateway responde `200`
