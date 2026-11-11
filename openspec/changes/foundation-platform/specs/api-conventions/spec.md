## Purpose

Definir el comportamiento HTTP común de todas las APIs de RutaViva, para que el CRM, el taller, la telemetría y DataImport consuman cualquier servicio de la misma forma.

## ADDED Requirements

### Requirement: Rutas versionadas por servicio
Cada servicio de negocio SHALL exponer sus endpoints bajo `/api/v1/{service-segment}/...`, donde `{service-segment}` es uno de `customers`, `workshops`, `fleet`, `maintenance`, `service-orders`, `imports` o `reports`.

#### Scenario: Ruta de un servicio
- **WHEN** un cliente pide `GET /api/v1/fleet/vehicles`
- **THEN** responde el servicio Fleet

### Requirement: Errores como ProblemDetails con código de negocio
Toda respuesta de error SHALL usar `application/problem+json` (RFC 9457) con `title`, `detail`, `status` y una extensión `code` con el código de error de negocio. El mapeo MUST ser: validación de entrada → `400`, recurso inexistente → `404`, conflicto de concurrencia o de estado → `409`, regla de negocio incumplida → `422`.

#### Scenario: Regla de negocio incumplida
- **WHEN** una operación es rechazada por una regla de dominio con el código `ServiceOrder.InvalidTransition`
- **THEN** la respuesta es `422` con `code = "ServiceOrder.InvalidTransition"` y un `detail` legible

#### Scenario: Validación de entrada
- **WHEN** un request llega con un campo obligatorio vacío
- **THEN** la respuesta es `400` con un `errors` que nombra el campo

#### Scenario: Conflicto de concurrencia
- **WHEN** dos usuarios modifican el mismo agregado y el segundo guarda con una versión obsoleta
- **THEN** el segundo recibe `409` con `code = "Concurrency.Conflict"`

### Requirement: Enums y fechas como texto estándar
Las APIs SHALL serializar los enums por nombre y las fechas en ISO 8601 (instantes con desplazamiento y fechas de negocio como `yyyy-MM-dd`). Un enum con un valor desconocido en un request MUST responder `400`.

#### Scenario: Enum desconocido
- **WHEN** un request envía `"status": "Paused"` para un enum que no tiene ese valor
- **THEN** la respuesta es `400`

### Requirement: Correlación de peticiones
Cada respuesta SHALL incluir el encabezado `X-Correlation-Id`. Si la petición lo trae, MUST reutilizarse; si no, se genera uno nuevo. El identificador MUST aparecer en los logs estructurados de la petición.

#### Scenario: Correlación propagada
- **WHEN** una petición llega con `X-Correlation-Id: abc-123`
- **THEN** la respuesta trae el mismo valor y los logs de esa petición lo incluyen

### Requirement: Health checks
Cada servicio SHALL exponer `/health` (dependencias, incluida la base de datos) y `/alive` (proceso vivo).

#### Scenario: Base de datos caída
- **WHEN** SQL Server no está disponible
- **THEN** `/health` responde `503` y `/alive` responde `200`
