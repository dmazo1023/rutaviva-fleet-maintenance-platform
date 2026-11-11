## Why

El CRM Angular, el taller y la plataforma de telemetría necesitan un único punto de entrada a los 7 servicios con API (ADR-0016), y el usuario del CRM debe ver al instante lo que hacen el taller, la telemetría y el sistema (driver «tiempo real» del análisis §2). Sin un Gateway, la SPA conocería la topología interna y repetiría CORS y correlación en cada servicio; sin Notifications, la web tendría que sondear y la alerta prioritaria de servicios adicionales (RQ-07, A-15) llegaría tarde, con el vehículo detenido en el taller.

Corresponde al corte **S4** del análisis (§10).

## What Changes

- Servicio **`RutaViva.ApiGateway`** (YARP): enruta `/api/v1/{customers|workshops|fleet|maintenance|service-orders|imports|reports}/**` a su servicio (foundation design D-09) y `/hubs/**` a Notifications con WebSockets. **No** enruta `/internal/**` (hub del EventBus, ADR-0024 §8). Centraliza CORS para el origen de la SPA y la correlación `X-Correlation-Id`. Sin lógica de negocio ni agregación de respuestas.
- Servicio **`RutaViva.Notifications.Hub`** (Application + Api): suscriptor del EventBus que arranca desde el último offset (ADR-0024 §7) y retransmite por un hub fuertemente tipado `NotificationsHub` / `INotificationsClient` los eventos de `maintenance.routine`, `maintenance.alert`, `serviceorders.service-order` y `fleet.vehicle`, traducidos a **mensajes propios** (no los contratos de integración, ADR-0016 IMP-002).
- Grupos por perfil: `customer:{CustomerId}` (dueño y operador), `approver:{ApproverId}` (responsable asignado) y `workshop:{WorkshopId}` (vista del taller). Métodos `JoinCustomer`, `LeaveCustomer`, `JoinApprover`, `LeaveApprover`, `JoinWorkshop`, `LeaveWorkshop`.
- **Alerta prioritaria** (RQ-07, A-15): `ApprovalRequested` con `Priority = High` llega al dueño y al responsable del vehículo.
- Sin persistencia: Notifications no tiene schema ni repite avisos perdidos; la web recarga por REST al reconectar (ADR-0016 IMP-003).

## Capabilities

### New Capabilities

- `api-gateway`: punto de entrada único para la SPA, el taller y la telemetría: enrutamiento por servicio, WebSockets hacia el hub de notificaciones, aislamiento de lo interno, CORS y correlación.
- `realtime-notifications`: difusión en tiempo real, por SignalR y por audiencia (dueño, responsable, taller), de los cambios de órdenes, aprobaciones, alertas, rutinas, lecturas de odómetro y vehículos.

### Modified Capabilities

(ninguna)

## Impact

- Código nuevo: `src/Gateways/RutaViva.ApiGateway`, `src/Platform/Notifications/RutaViva.Notifications.Application`, `src/Platform/Notifications/RutaViva.Notifications.Hub`, `tests/Gateways/RutaViva.ApiGateway.IntegrationTests`, `tests/Platform/RutaViva.Notifications.Application.UnitTests`, `tests/Platform/RutaViva.Notifications.Hub.IntegrationTests`, `tests/RutaViva.Architecture.Tests/NotificationsArchitectureTests.cs`.
- Archivos compartidos que se tocan: `RutaViva.slnx` (alta de proyectos) y, solo en la tarea recortable, `src/Platform/RutaViva.AppHost/Program.cs`.
- Dependencias NuGet: `Yarp.ReverseProxy`, `Microsoft.Extensions.ServiceDiscovery.Yarp` (si hay Aspire), `Microsoft.AspNetCore.SignalR.Client` (solo pruebas).
- Depende de `foundation-platform` (BuildingBlocks, Contracts, EventBus). Lo consume `fleet-maintenance-ui` (`core/hub-methods.ts`, `NotificationsService`).
- Requiere dos ampliaciones del registro de contratos de foundation (ver `design.md` § Contratos nuevos requeridos).

## Fuera de alcance

- Autenticación y autorización de usuarios y de la unión a grupos (fuera del reto; riesgo documentado).
- *Rate limiting*, caché de respuestas, agregación o composición de APIs en el Gateway (BFF).
- Reenvío de avisos perdidos durante una desconexión, notificaciones push, correo o SMS (el reto solo exige publicar el evento).
- Varias instancias de Notifications con *backplane* de SignalR (ADR-0024 NEG-003).

**Recortable** (orden del análisis §10): el registro de Gateway y Notifications en el AppHost de Aspire (tarea 6.1). El Gateway y Notifications en sí **no** son recortables: el CRM depende de ellos.
