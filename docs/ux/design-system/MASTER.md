# RutaViva · Sistema de diseño (MASTER)

Fuente global del mockup del CRM. Modo **externo** con toques de Doggie (íconos Material Symbols, tipo oración, densidad de tabla, alturas de control 40–44 px). Las páginas pueden definir excepciones en `pages/{pagina}.md`.

## Esencia

| Eje | Decisión |
|---|---|
| Propósito | Que el jefe de flota sepa en segundos qué vehículo necesita mantenimiento y por qué |
| Tono | Técnico, preciso y en calma: la audacia está en la claridad del dato |
| Firma | **Ruta de la rutina**: pista doble (km y tiempo) del último servicio al próximo, con zona «por vencer» rayada y punto de posición actual |
| Microcopy | Mínimo. Íconos con tooltip y `aria-label`; sin textos que expliquen lo que un control ya comunica |

## Color (tokens semánticos)

| Token | Claro | Oscuro | Rol |
|---|---|---|---|
| `--c-brand` | `#0E5A4A` | `#3DCB9F` | Marca y acción principal (desde v3 ya no representa un estado) |
| `--c-on-brand` | `#FFFFFF` | `#04261C` | Texto sobre marca (8.1:1 / 7.9:1) |
| `--c-pulse` | `#12B886` | `#3DCB9F` | Solo decorativo: indicador «en vivo» |
| `--c-bg` / `--c-surface` / `--c-sunken` | `#F5F7F6` / `#FFFFFF` / `#EEF2F0` | `#0E1513` / `#151E1B` / `#1C2723` | Superficies |
| `--c-text` / `--c-muted` | `#16201C` / `#56635E` | `#E8EFEC` / `#9DB0A8` | Texto (15.5:1 / 6.3:1) |
| `--c-ok` · `--c-ok-bar` · `--c-ok-soft` | `#047857` · `#10B981` · `#ECFDF5` | `#6EE7B7` · `#34D399` · `#12302A` | Al día (texto 5.2:1 sobre soft) |
| `--c-warn` · `--c-warn-bar` · `--c-warn-soft` | `#A16207` · `#EAB308` · `#FEF9C3` | `#FDE047` · `#FACC15` · `#332C12` | Por vencer (texto 4.6:1 sobre soft) |
| `--c-danger` · `--c-danger-bar` · `--c-danger-soft` | `#B91C1C` · `#DC2626` · `#FEF2F2` | `#FCA5A5` · `#F87171` · `#3A1E1C` | Vencida, destructivo (texto 5.9:1) |
| `--c-info` sobre `--c-info-soft` | `#1F5E8C` / `#E5F0F8` | `#7CB8E8` / `#16283A` | Estados intermedios de la orden |
| `--c-plate` / `--c-plate-ink` | `#F9D85A` / `#1A1A1A` | igual | Chip de placa (12.4:1) |

> **Semáforo (v3)**, tomado del reporte de consumo DashCam: el relleno de la barra es brillante y no se usa solo (verde 2.5:1 y amarillo 1.9:1 sobre blanco); el estado siempre va acompañado del valor en texto oscuro, la pastilla con ícono y el punto de posición en tono oscuro (≥3:1).

## Tipografía

| Rol | Familia | Tamaño / peso |
|---|---|---|
| Interfaz y títulos | Manrope | 13 / 14 / 15 / 18 / 22 / 28 px · 400–800 |
| Identificadores (placa, VIN, n.º de orden) | JetBrains Mono | 12–15 px · 500–600 |
| Números | `font-variant-numeric: tabular-nums` | — |

## Forma y espacio

- Radios por jerarquía: chip 4–6 px · control 10 px · panel 14 px · modal 20 px.
- Escala de espacio estándar 4/8/12/16/24/32/48.
- Sombra solo en capas flotantes (modal, toast, popover); los paneles usan borde.
- Movimiento: transiciones que explican un cambio (pista que avanza, odómetro que cuenta, fila que destella al recibir un evento). Se respeta `prefers-reduced-motion`.

## Estados de dominio

| Dominio | Valores y color |
|---|---|
| Rutina (vencimiento) | Al día (brand) · Por vencer (warn) · Vencida (danger) · Inactiva (neutral) |
| Vehículo | Activo · Inactivo (neutral, evaluaciones en pausa) |
| Orden | Creada (neutral) · Pendiente de aprobación (warn) · Aprobada / En sitio (info) · En proceso (brand) · En pausa (neutral) · Finalizada (ok) · Cancelada (neutral) |
| Alerta | Activa · Atendida · Resuelta |
