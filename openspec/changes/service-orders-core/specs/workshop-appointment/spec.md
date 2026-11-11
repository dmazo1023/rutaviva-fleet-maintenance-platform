## Purpose

Registrar cuándo llevará el vehículo al taller una orden aprobada, como precondición para marcarla «En sitio», sin construir un módulo de agendamiento (ADR-0021, A-20). Capacidad recortable.

## ADDED Requirements

### Requirement: Registro y reprogramación de la cita por el taller (Recortable)
Con la orden en `Approved`, el taller asignado SHALL registrar la cita con `PUT /api/v1/service-orders/{id}/appointment { scheduledAt, notes }`. La fecha MUST ser posterior al momento del registro y las notas MUST tener como máximo 250 caracteres. El primer registro SHALL quedar en el historial como `AppointmentScheduled` y los siguientes como `AppointmentRescheduled`; cada uno publica `ServiceOrderAppointmentScheduled` con `IsReschedule` según corresponda.

#### Scenario: Primera cita
- **WHEN** el taller registra la cita para mañana a las 08:00 en una orden `Approved`
- **THEN** la orden guarda la cita, el historial tiene `AppointmentScheduled` y se publica `ServiceOrderAppointmentScheduled` con `IsReschedule = false`

#### Scenario: Reprogramación
- **WHEN** el taller vuelve a registrar la cita para pasado mañana
- **THEN** la cita se reemplaza, el historial tiene `AppointmentRescheduled` y el evento lleva `IsReschedule = true`

#### Scenario: Cita en el pasado
- **WHEN** el taller registra una cita con fecha anterior a ahora
- **THEN** la respuesta es `400` con `code = "ServiceOrder.AppointmentInPast"`

#### Scenario: Cita fuera de Aprobada
- **WHEN** el taller registra una cita en una orden `OnSite`
- **THEN** la respuesta es `422` con `code = "ServiceOrder.InvalidTransition"`

### Requirement: En sitio exige cita (Recortable)
El taller asignado SHALL marcar `OnSite` una orden `Approved` solo si tiene cita registrada; sin cita MUST rechazarse con `422` y `code = "ServiceOrder.AppointmentRequired"`. Al marcarla, la orden guarda la fecha de llegada.

#### Scenario: En sitio sin cita
- **WHEN** el taller marca En sitio una orden `Approved` sin cita
- **THEN** la respuesta es `422` con `code = "ServiceOrder.AppointmentRequired"` y la orden sigue `Approved`

#### Scenario: En sitio con cita
- **WHEN** el taller marca En sitio una orden `Approved` con cita
- **THEN** la orden queda `OnSite` con su fecha de llegada
