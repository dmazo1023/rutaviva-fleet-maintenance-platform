## Purpose

Dar al usuario del CRM un marco común (navegación, contexto de cliente y perfil, conexión en vivo, actualización manual, avisos y errores) para que todas las vistas se comporten igual y reflejen los cambios del sistema sin recargar.

## ADDED Requirements

### Requirement: Navegación principal con contadores
El CRM SHALL mostrar una navegación con las secciones Flota, Alertas, Órdenes, Parámetros, Carga inicial y Reportes, y SHALL mostrar el número de alertas activas y, solo para el perfil Aprobador, el número de órdenes por aprobar. En anchos de 760 px o menos la navegación MUST pasar a una barra inferior.

#### Scenario: Contadores visibles para el Aprobador
- **WHEN** el perfil activo es Aprobador y el cliente tiene 3 alertas activas y 2 órdenes en `PendingApproval`
- **THEN** la navegación muestra 3 en Alertas y 2 en Órdenes

#### Scenario: Contador de órdenes oculto para el Taller
- **WHEN** el perfil activo es Taller
- **THEN** la navegación no muestra el contador de órdenes por aprobar

#### Scenario: Ancho de teléfono
- **WHEN** el contenedor de la aplicación mide 390 px
- **THEN** la navegación se muestra como barra inferior y no hay desplazamiento horizontal

### Requirement: Contexto de cliente y perfil
El CRM SHALL permitir elegir el cliente y el perfil (Operador, Aprobador o Taller) en la barra superior. El cliente elegido MUST filtrar las vistas y definir el grupo de notificaciones en vivo; el perfil MUST definir qué acciones se muestran.

#### Scenario: Cambio de cliente
- **WHEN** el usuario cambia el cliente de RutaViva S.A.S. a Transportes Andinos S.A.
- **THEN** la flota, las alertas y las órdenes se recargan con los datos del nuevo cliente y la conexión en vivo se une al grupo del nuevo cliente

#### Scenario: Perfil Operador sin acciones de aprobación
- **WHEN** el perfil activo es Operador y abre una orden en `PendingApproval`
- **THEN** no ve las acciones Aprobar, Rechazar ni Cancelar orden

### Requirement: Indicador de conexión en vivo
El CRM SHALL mostrar un indicador «En vivo» mientras la conexión de notificaciones está activa y «Reconectando…» mientras se recupera. Al reconectarse MUST recargar por REST los datos de la vista abierta para no perder cambios ocurridos durante la desconexión.

#### Scenario: Pérdida y recuperación de la conexión
- **WHEN** la conexión de notificaciones se cae y se recupera
- **THEN** el indicador pasa a «Reconectando…» y vuelve a «En vivo», y la vista abierta se recarga una vez desde la API

### Requirement: Botón Actualizar
El CRM SHALL ofrecer un botón «Actualizar» que solicita la reevaluación de mantenimiento con los parámetros vigentes en la base de datos (del vehículo abierto o de todos) y luego recarga la vista (ADR-0019, RQ-03).

#### Scenario: Edición directa en SQL reflejada
- **WHEN** un administrador cambia en SQL el % «por vencer» de un tipo de rutina y el usuario pulsa «Actualizar» con el detalle del vehículo abierto
- **THEN** el CRM llama a la reevaluación para ese vehículo y muestra el estado de sus rutinas con el nuevo umbral

#### Scenario: Botón ocupado
- **WHEN** la reevaluación está en curso
- **THEN** el botón queda deshabilitado con `aria-busy="true"` y su texto termina en «…»

### Requirement: Avisos y errores uniformes
El CRM SHALL mostrar los errores `400`, `409` y `422` de la API como avisos con el `title` y el `detail` del `ProblemDetails`. Los avisos MUST anunciarse con `aria-live="polite"`, desaparecer a los 6 segundos y admitir una acción opcional (Ver o Deshacer). Un `409` sobre una orden MUST recargar la orden.

#### Scenario: Regla de negocio incumplida
- **WHEN** la API responde `422` con `title = "Transición no permitida"`
- **THEN** aparece un aviso con ese título y su detalle, sin romper la vista

#### Scenario: Conflicto de concurrencia en una orden
- **WHEN** una acción sobre una orden responde `409`
- **THEN** el CRM muestra el aviso y recarga el detalle de la orden

### Requirement: Estado de la vista en la URL
Los filtros, la pestaña activa y la entidad abierta SHALL vivir en la URL, de modo que un enlace directo y el botón Atrás restauren la misma vista.

#### Scenario: Enlace directo a una pestaña
- **WHEN** el usuario abre `/flota/RVA101?tab=lecturas`
- **THEN** ve el detalle de RVA101 con la pestaña Lecturas activa

#### Scenario: Botón Atrás
- **WHEN** el usuario cambia el filtro de la flota a «Vencida» y luego pulsa Atrás
- **THEN** la lista vuelve al filtro anterior

### Requirement: Accesibilidad de teclado y foco
El CRM SHALL ofrecer un enlace «Saltar al contenido», llevar el foco al título principal al navegar, permitir el uso completo con teclado con foco visible y cumplir contraste AA en tema claro y oscuro. Los botones con solo ícono MUST tener nombre accesible y tooltip.

#### Scenario: Navegación por teclado
- **WHEN** el usuario recorre la aplicación solo con Tab
- **THEN** el orden es navegación → barra superior → filtros → lista → detalle → barra de acciones, y cada elemento enfocado tiene un indicador visible

#### Scenario: Diálogo con Escape
- **WHEN** un diálogo está abierto y el usuario pulsa Escape
- **THEN** el diálogo se cierra y el foco vuelve al control que lo abrió

### Requirement: Textos y colores centralizados
Los textos visibles SHALL provenir de constantes por *feature* y las etiquetas de los enums de un único mapa en español. Los colores MUST provenir de los tokens de diseño; ningún componente MUST declarar un color hexadecimal propio.

#### Scenario: Etiqueta de estado de orden
- **WHEN** una orden tiene estado `OnSite`
- **THEN** la pastilla muestra «En sitio», tomado del mapa único de etiquetas

#### Scenario: Revisión de colores
- **WHEN** se ejecuta la verificación de estilos del proyecto
- **THEN** no hay valores hexadecimales fuera del archivo de tokens
