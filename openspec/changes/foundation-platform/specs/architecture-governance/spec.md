## Purpose

Hacer verificable en cada build que la solución respeta Clean Architecture, el aislamiento entre bounded contexts y las reglas de estilo obligatorias del reto, para que la arquitectura no se degrade al crecer (ADR-0009).

## ADDED Requirements

### Requirement: Regla de dependencia de Clean Architecture
En cada servicio, `Domain` MUST NOT depender de `Application`, `Infrastructure` ni `Api`; `Application` MUST NOT depender de `Infrastructure` ni `Api`; e `Infrastructure` MUST NOT depender de `Api`.

#### Scenario: Domain referencia EF Core
- **WHEN** un tipo de `RutaViva.Maintenance.Domain` usa un tipo de `Microsoft.EntityFrameworkCore`
- **THEN** la prueba de arquitectura falla nombrando el tipo infractor

### Requirement: Aislamiento entre bounded contexts
Ningún proyecto `RutaViva.{A}.*` MUST referenciar a `RutaViva.{B}.*` de otro servicio. Las únicas dependencias compartidas permitidas son `BuildingBlocks.*` y `RutaViva.IntegrationContracts`.

#### Scenario: ServiceOrders usa un tipo de Maintenance
- **WHEN** `RutaViva.ServiceOrders.Application` referencia `RutaViva.Maintenance.Domain`
- **THEN** la prueba de arquitectura falla

### Requirement: Application independiente del transporte y del framework web
Los proyectos `Application` MUST NOT depender de `Microsoft.AspNetCore.*` (incluido el cliente de SignalR), de EF Core ni de librerías de archivos (CsvHelper, ClosedXML).

#### Scenario: Application usa SignalR
- **WHEN** un handler de Application referencia `Microsoft.AspNetCore.SignalR.Client`
- **THEN** la prueba de arquitectura falla

### Requirement: Dominio encapsulado
Las propiedades de entidades y agregados MUST NOT tener *setters* públicos, y los agregados MUST exponer sus colecciones internas como solo lectura.

#### Scenario: Setter público
- **WHEN** una entidad de dominio declara una propiedad con `set` público
- **THEN** la prueba de arquitectura falla

### Requirement: Estilo obligatorio verificado por el compilador
La compilación MUST fallar si se usa `var`, y los avisos MUST tratarse como errores en todos los proyectos.

#### Scenario: Uso de var
- **WHEN** un archivo C# declara `var total = 0;`
- **THEN** `dotnet build` falla

### Requirement: Enums con valor inicial explícito
Todo enum de dominio MUST declarar valores numéricos explícitos que empiecen en 1, de modo que `default(T)` no sea un estado válido (data-model E-01).

#### Scenario: Enum sin valores explícitos
- **WHEN** un enum de dominio declara su primer miembro sin valor o con valor 0
- **THEN** la prueba de arquitectura falla
