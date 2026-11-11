## 1. Proyecto y reglas

- [x] 1.1 Crear `src/Web/rutaviva-fleet-maintenance-ui` con Angular CLI 22 (standalone, sin zone.js, SCSS, routing), `app.config.ts` con `provideZonelessChangeDetection`, `provideRouter(routes, withComponentInputBinding())` y `provideHttpClient(withInterceptors([]))`, y `proxy.conf.json` hacia el Gateway. Verificar con `ng build` y `ng test` en verde sobre el proyecto recién creado
- [x] 1.2 Configurar ESLint (`no-restricted-imports` para la regla de dependencia de design D-01) y Stylelint (`color-no-hex` salvo `_tokens.scss`, sin `::ng-deep`, sin `transition: all`) en `npm run lint`. Verificar con `npm run lint` en verde y con un hex temporal en un componente que lo hace fallar
- [x] 1.3 Componente de humo con `templateUrl`, `input()` y `output()` y su prueba (Ola 0 del harness); borrarlo al pasar. Verificar con `ng test` en verde

## 2. Contratos de la SPA (primero)

- [x] 2.1 `core/models/enums.ts` con los tipos literales espejo de data-model §5 (más `OdometerReadingOutcome`, `OdometerReadingIgnoredReason`, `ServiceOrderAction`, `ServiceOrderBlockedReason`, `Profile`) y `core/models/enum-labels.ts` con el mapa único en español. Verificar con prueba que cada valor de cada tipo tiene etiqueta (tabla exhaustiva tipada con `Record<T, string>`)
- [x] 2.2 `core/api/api-routes.ts` con todas las rutas del registro de `foundation-platform` (D-09) y de CR-01 a CR-09 como funciones puras. Verificar con prueba unitaria de 5 rutas representativas (con y sin parámetros de consulta)
- [x] 2.3 `core/realtime/hub-methods.ts` y `core/realtime/realtime-events.ts` (nombres y payloads del hub, ADR-0016 IMP-002 + CR-07). Verificar con `ng build` (tipos) y revisión de que ningún otro archivo usa literales de métodos del hub (`grep`)
- [x] 2.4 VMs de design D-02 y del handoff §2 en `features/*/models/*.vm.ts` y el tipo `Loadable<T>` en `shared/state/loadable.ts`. Verificar con `ng build`
- [x] 2.5 `src/styles/_tokens.scss` (MASTER.md, claro y `[data-theme="dark"]`), `_typography.scss`, fuentes Manrope, JetBrains Mono y Material Symbols Rounded, clases de tono `is-*`. Verificar con `npm run lint` y comparación visual con el mockup en 1440 px

## 3. Core

- [x] 3.1 `core/api/api-client.ts` (get/post/put/patch y `download` con `Content-Disposition`) y `problem-details.interceptor.ts` (400/409/422 → `ToastStore`, 409 de orden → evento de recarga). Verificar con pruebas usando `HttpTestingController`
- [x] 3.2 `core/feedback/toast.store.ts` (6 s, acción opcional, persistente para prioritarias) y `shared/toast-outlet` con `aria-live="polite"` y `role="alert"` para errores. Verificar con pruebas con temporizadores falsos
- [x] 3.3 `core/context/session.store.ts` (cliente, perfil, actor; `localStorage` con `try/catch`) y carga de clientes, aprobadores y talleres para el selector (CR-09). Verificar con prueba del store con `localStorage` que lanza error
- [x] 3.4 `core/realtime/notifications.service.ts`: conexión con reconexión automática, `connectionState` como *signal*, `JoinCustomer`/`LeaveCustomer`/`JoinApprover` al cambiar la sesión y evento `reconnected`. Verificar con pruebas con un `HubConnection` falso (cambio de cliente, reconexión)

## 4. Shell

- [ ] 4.1 `core/shell/shell-rail.component` (6 secciones, contadores de alertas y por aprobar, barra inferior ≤ 760 px) y `skip-link`. Verificar con pruebas de contadores por perfil (spec `crm-shell`)
- [ ] 4.2 `core/shell/top-bar.component` (selector de cliente, perfil y actor; indicador «En vivo»/«Reconectando…»; botón «Actualizar» con `aria-busy` que llama a `reevaluations` y recarga). Verificar con pruebas del indicador y del botón ocupado
- [ ] 4.3 `app.routes.ts` con rutas perezosas `/flota`, `/flota/:plate`, `/alertas`, `/ordenes`, `/ordenes/:id`, `/parametros`, `/carga`, `/reportes`, `/simulador`, foco al `h1` al navegar. Verificar con prueba de router: `/flota/RVA101?tab=lecturas` activa la pestaña

## 5. Componentes compartidos

- [ ] 5.1 `shared/plate-chip` (md/lg, `<a>` o `<span>`) y `shared/status-pill` (estado, disparador con íconos y `sr-only`). Verificar con pruebas de nombre accesible por disparador
- [ ] 5.2 `shared/routine-track` (firma: riel, franja rayada del umbral, relleno con `scaleX`, punto, restante coloreado, `role="meter"`). Verificar con pruebas en 0 %, en el umbral y en 108 %
- [ ] 5.3 `shared/mini-bar`, `shared/switch` (`role="switch"`, `aria-checked`) y `shared/segmented-filter` (`fieldset` + radios sincronizados con query params). Verificar con pruebas de teclado y de sincronización con la URL
- [ ] 5.4 `shared/confirm-impact.dialog`, `empty-state`, `error-state` y `skeleton`. Verificar con prueba: Escape cierra y el foco vuelve al disparador

## 6. Flota y mantenimiento

- [ ] 6.1 `features/fleet/data/fleet.api.ts` y `features/maintenance/data/maintenance.api.ts` (Adapters con mapeo a VMs, CR-01, CR-02, CR-08 y lecturas de ADR-0025). Verificar con pruebas de mapeo DTO → VM
- [ ] 6.2 `features/fleet/state/fleet.store.ts`, `features/maintenance/state/plan.store.ts` y `fleet.facade.ts` (composición Fleet + Maintenance, reacción a `RoutineChanged`, `OdometerReadingRecorded`, `VehicleChanged`, `ServiceOrderChanged`). Verificar con pruebas del orden por urgencia y de *upsert* por evento
- [ ] 6.3 `features/fleet/pages/fleet.page` + `components/vehicle-list` (búsqueda, filtro, estados cargando/vacío/error/sin coincidencias, destello). Verificar con pruebas de los cuatro estados de spec `crm-fleet-maintenance`
- [ ] 6.4 `features/fleet/pages/vehicle-detail.page` (encabezado, pestañas ARIA con `?tab=`, dos columnas ≥ 1100 px, aviso de inactivo). Verificar con pruebas de pestañas con ← / → y del aviso de inactivo
- [ ] 6.5 `features/maintenance/components/routine-row` (pistas, interruptor con «Deshacer», «Crear orden» o enlace a la orden). Verificar con pruebas de `canCreateOrder` y del aviso con «Deshacer»
- [ ] 6.6 `features/maintenance/dialogs/routine-params.dialog` (marcadores del tipo, origen del %, vista previa en vivo con `routine-track`, validación al enviar). Verificar con prueba: cambiar el % actualiza la vista previa antes de guardar
- [ ] 6.7 `features/fleet/dialogs/deactivate-vehicle.dialog` (impacto: órdenes abiertas y rutinas) y reactivación. Verificar con prueba del texto de impacto
- [ ] 6.8 `features/maintenance/components/odometer-readings` (pestaña Lecturas, ignoradas tachadas con motivo, vacío). Verificar con prueba de la lectura ignorada

## 7. Alertas

- [ ] 7.1 `features/maintenance/state/alerts.store.ts` y `pages/alerts-inbox.page` (filtros Activas/Atendidas/Resueltas en URL, Atender, motivo de resolución, reacción a `AlertChanged`, contador del rail). Verificar con pruebas de Atender y del escalamiento en vivo

## 8. Órdenes de servicio

- [ ] 8.1 `features/service-orders/data/service-orders.api.ts` (CR-04 a CR-06, acciones y políticas) y `state/service-orders.store.ts` + `service-orders.facade.ts` (reacción a `ServiceOrderChanged` y `ApprovalRequested`). Verificar con pruebas de mapeo de `availableActions` y de la recarga ante `409`
- [ ] 8.2 `pages/orders-inbox.page` (Por aprobar · Abiertas · Cerradas · Todas, orden prioritaria primero, estados vacíos). Verificar con prueba de orden de la bandeja
- [ ] 8.3 `pages/order-detail.page` + `components/order-stepper`, `service-lines` (precio en línea para el taller) y `order-history` (plegado). Verificar con pruebas del paso actual, del aviso de pausa y de «Esperando cotización del taller»
- [ ] 8.4 `components/order-actions` (tablas `ACTION_FORMS`/`ACTION_IMPACT`, Cancelar a la izquierda, motivo con `aria-describedby`). Verificar con pruebas: Aprobar deshabilitado con «Esperando precio del taller»; Taller sin Cancelar
- [ ] 8.5 `dialogs/order-form.dialog` (Cotizar con líneas, Pedir/Agregar servicio, Precio, Cita, Finalizar con km y fecha) y confirmaciones de Rechazar/Cancelar con impacto. Verificar con pruebas de validación al enviar y del texto de impacto para cotización y para servicios adicionales

## 9. Parámetros y simuladores

- [ ] 9.1 `features/administration` (matriz de tipos de rutina editable y política con historial; solo lectura fuera del Operador). Verificar con pruebas de validación (sin intervalos, % fuera de rango, monto negativo) y del modo solo lectura
- [ ] 9.2 `features/simulators` (lectura de odómetro con resultado y revisión por fecha con resumen; solo Operador). Verificar con pruebas del mensaje por cada `outcome`

## 10. Integración

- [ ] 10.1 Prueba manual guiada con el sistema levantado y los datos de `samples/import`: criterios de aceptación de UI del handoff §8 (salvo los de tamaño y tema, en 10.3). Verificar registrando el resultado de cada criterio en `src/Web/rutaviva-fleet-maintenance-ui/ACCEPTANCE.md`

## 11. Recortables

- [ ] 11.1 (Recortable) `features/data-import` básico: plantillas, subida de 1 a 5 archivos, vista previa por etapa con filas rechazadas, confirmar y CSV de errores. Verificar con pruebas del Adapter y carga manual de `samples/import/con-errores/vehiculos.csv`
- [ ] 11.2 (Recortable) `features/reports` básico: catálogo, filtros con el cliente activo, vista previa paginada y descarga xlsx/csv. Verificar con pruebas del Adapter y descarga manual de R-01
- [ ] 11.3 (Recortable) Estilos finos: tema oscuro con `data-theme`, *container queries* 1180/1100/900, animaciones (relleno 700 ms, conteo del odómetro, destello) con `prefers-reduced-motion`. Verificar comparando con el mockup en 390/768/1024/1440 px en ambos temas
- [ ] 11.4 (Recortable) E2E Playwright `e2e/maintenance-to-order.spec.ts`: lectura → alerta y orden → el taller cotiza 2.000.000 → el aprobador aprueba → el taller agenda, marca en sitio, inicia y completa → la rutina vuelve a «Al día» y la alerta se resuelve, sin recargar (ADR-0008 IMP-003). Verificar con `npx playwright test` en verde
