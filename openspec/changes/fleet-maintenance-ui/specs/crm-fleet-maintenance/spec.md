## Purpose

Permitir al jefe de flota saber en segundos qué vehículo necesita mantenimiento y por qué, ajustar sus rutinas y controlar su estado de activación, viendo cada cambio del sistema en tiempo real.

## ADDED Requirements

### Requirement: Lista de vehículos priorizada
El CRM SHALL listar los vehículos del cliente con su placa, modelo, peor estado de rutina y porcentaje consumido, ordenados por vencida → por vencer → al día → inactivo y, dentro de cada grupo, por porcentaje consumido descendente. La lista MUST admitir búsqueda por placa o modelo y filtro por estado, sincronizados con la URL.

#### Scenario: Orden por urgencia
- **WHEN** el cliente tiene TAN201 (vencida), RVA102 (por vencer), RVA101 (al día) y un vehículo inactivo
- **THEN** la lista los muestra en ese orden

#### Scenario: Búsqueda sin coincidencias
- **WHEN** el usuario busca «ZZZ»
- **THEN** ve «Sin coincidencias» con la acción «Limpiar filtros»

#### Scenario: Flota vacía
- **WHEN** el cliente no tiene vehículos
- **THEN** ve «Aún no hay vehículos» con la acción «Cargar flota», que lleva a la carga inicial

#### Scenario: Error de carga
- **WHEN** la API de flota no responde
- **THEN** ve «No pudimos cargar la flota» con la acción «Reintentar»

#### Scenario: Cargando
- **WHEN** la lista está cargando
- **THEN** se muestra un esqueleto de 4 filas

### Requirement: Detalle del vehículo con pestañas
El detalle del vehículo SHALL mostrar el encabezado (placa, VIN, modelo, dueño, taller, responsable, odómetro y estado) y las pestañas Rutinas, Órdenes y Lecturas, con la pestaña activa en la URL. Desde 1100 px la lista y el detalle MUST verse en dos columnas; por debajo, el detalle reemplaza a la lista con un botón Volver.

#### Scenario: Diseño de dos columnas
- **WHEN** el contenedor mide 1440 px y se abre RVA101
- **THEN** la lista y el detalle se ven lado a lado

#### Scenario: Diseño de una columna
- **WHEN** el contenedor mide 768 px y se abre RVA101
- **THEN** solo se ve el detalle, con un botón Volver a la lista

### Requirement: Pista de la rutina por km y por tiempo
Cada rutina SHALL mostrar una pista por cada intervalo configurado (km y tiempo) con el porcentaje consumido, la franja «por vencer» rayada del ancho del umbral efectivo, un punto en la posición actual y el valor restante (negativo si se excedió) coloreado según el estado. La pista MUST exponer `role="meter"` con su valor accesible. Una rutina sin intervalo de tiempo MUST omitir esa pista.

#### Scenario: Rutina vencida por km
- **WHEN** abro un vehículo con una rutina vencida por km
- **THEN** veo la pista de km llena en rojo, el valor «−N km» en rojo y la pastilla «Vencida» con el ícono de velocímetro

#### Scenario: Umbral ajustado
- **WHEN** abro la rutina de aceite de EVA30D, que tiene un ajuste del 20 %
- **THEN** la franja rayada ocupa el 20 % final de la pista y el estado es «Por vencer»

#### Scenario: Consumo mayor al 100 %
- **WHEN** una rutina lleva el 108 % de su intervalo de km
- **THEN** la pista se muestra llena sin desbordarse y el valor restante es negativo

### Requirement: Ajustar parámetros de la rutina con vista previa
El CRM SHALL permitir editar los intervalos de km, de meses y el % «por vencer» de una rutina del vehículo, mostrando como marcador los valores del tipo de rutina y si el % efectivo viene del tipo o del ajuste. Antes de guardar MUST mostrar una vista previa de la pista y del estado con los valores ingresados. La validación MUST ocurrir al enviar, con el foco en el primer error.

#### Scenario: Vista previa en vivo
- **WHEN** en el diálogo «Ajustar rutina» cambio el % de aviso de 10 a 20
- **THEN** la vista previa refleja el nuevo estado antes de guardar

#### Scenario: Valor fuera de rango
- **WHEN** envío el diálogo con un % de 150
- **THEN** el campo muestra un error con la causa y la solución, y el foco queda en él

#### Scenario: Guardado
- **WHEN** guardo un intervalo de km válido
- **THEN** el CRM envía `PUT /api/v1/maintenance/plans/{vehicleId}/routines/{routineId}` y la rutina se actualiza con el estado reevaluado

### Requirement: Activar y desactivar rutinas
Cada rutina SHALL tener un interruptor de activación (`role="switch"`) que cambia su estado sin confirmación y ofrece «Deshacer» en el aviso. Una rutina inactiva MUST mostrarse atenuada con la insignia «Inactiva».

#### Scenario: Desactivar con deshacer
- **WHEN** desactivo la rutina de frenos de RVA101
- **THEN** el CRM envía el cambio de estado, la rutina queda «Inactiva» y un aviso ofrece «Deshacer»

### Requirement: Desactivar y reactivar el vehículo con impacto
El CRM SHALL pedir confirmación antes de desactivar un vehículo, listando el impacto: cuántas órdenes abiertas pasan a pausa y que las rutinas dejan de evaluarse. Un vehículo inactivo MUST mostrar el aviso «Evaluaciones y órdenes en pausa» y sus controles de rutina deshabilitados.

#### Scenario: Confirmación con impacto
- **WHEN** pido desactivar TAN201, que tiene 1 orden abierta
- **THEN** el diálogo indica que 1 orden pasa a pausa y que las rutinas dejan de evaluarse, con el botón destructivo y «Volver»

#### Scenario: Vehículo inactivo
- **WHEN** abro un vehículo inactivo
- **THEN** veo «Evaluaciones y órdenes en pausa» y los interruptores y el ajuste de rutinas están deshabilitados

### Requirement: Crear orden manual desde la rutina
Para una rutina vencida, activa y sin orden abierta, el CRM SHALL ofrecer «Crear orden», que crea la orden manual (A-07). Si la rutina tiene una orden abierta MUST mostrar un enlace a esa orden en su lugar.

#### Scenario: Rutina vencida tras una orden cancelada
- **WHEN** la rutina de aceite de TAN201 está vencida y su orden fue cancelada
- **THEN** veo «Crear orden» y al pulsarlo se crea una orden con origen manual

#### Scenario: Rutina con orden abierta
- **WHEN** la rutina tiene una orden en `Approved`
- **THEN** veo el enlace a esa orden y no veo «Crear orden»

### Requirement: Historial de lecturas de odómetro
La pestaña Lecturas SHALL listar las lecturas del vehículo (más recientes primero) con km, fecha reportada y resultado. Las ignoradas por RN-6 MUST mostrarse tachadas con su motivo (ADR-0025).

#### Scenario: Lectura repetida ignorada
- **WHEN** RVA101 recibió dos lecturas de 53.000 km
- **THEN** la segunda aparece tachada con el motivo «No es mayor que el odómetro actual»

#### Scenario: Sin lecturas
- **WHEN** el vehículo no tiene lecturas
- **THEN** la pestaña muestra «Sin lecturas»

### Requirement: Actualización en vivo de flota y rutinas
Cuando llega por la conexión en vivo un cambio de rutina, de plan o una lectura de odómetro del cliente activo, el CRM SHALL actualizar la fila del vehículo y, si está abierto, su detalle, sin recargar la página. La fila MUST destellar, el odómetro MUST animar el conteo y, si cambió el estado de una rutina, MUST aparecer un aviso. Con `prefers-reduced-motion` las animaciones MUST omitirse.

#### Scenario: Lectura que cambia el estado
- **WHEN** llega una lectura que deja la rutina de aceite de RVA101 «Por vencer»
- **THEN** la fila de RVA101 destella, el odómetro cuenta hasta el nuevo valor, la rutina cambia a «Por vencer» y aparece un aviso

#### Scenario: Movimiento reducido
- **WHEN** el sistema operativo pide movimiento reducido y llega una lectura
- **THEN** los valores se actualizan sin destello ni conteo animado
