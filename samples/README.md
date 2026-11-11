# Datos de ejemplo — RutaViva

Datos ficticios para la demo. Los correos de personas (aprobadores y responsables) siguen el formato `Nombre_Apellido@rutaviva.com`, sin tildes. Los correos de contacto de empresas y talleres usan dominios `.example`. La carga inicial con **DataImport** es el punto de partida ([ADR-0022](../docs/adr/adr-0022-arch-data-import-module.md)).

## 1. Carga inicial (`samples/import/`)

Formato: separador `;`, fechas `yyyy-MM-dd` (también se acepta `dd/MM/yyyy`), UTF-8 o Windows-1252. Las columnas vacías toman el valor por defecto.

| Etapa | Archivo | Filas | Notas |
|---|---|---|---|
| 1 | `talleres.csv` | 3 | |
| 2 | `clientes.csv` | 3 | RutaViva S.A.S. es dueña de su propia flota (A-11) |
| 3 | `aprobadores.csv` | 5 | |
| 4 | `vehiculos.csv` | 10 | `correo_responsable` es opcional (A-11) |
| 5 | `rutinas.csv` | 15 | km y fecha vacíos = vehículo nuevo, arranca en 0 km y en la fecha de carga (A-01) |

### Resultado esperado tras cargar `rutinas.csv` (evaluado el 2026-11-12)

Parámetros por defecto: aceite 10.000 km / 6 meses, frenos 20.000 km / 12 meses, llantas 40.000 km / 24 meses, todos con 10 %.

| Placa | Rutina | Último servicio | Odómetro | Consumo | Estado esperado | Efecto |
|---|---|---|---|---|---|---|
| RVA101 | Aceite | 45.000 · 2026-08-01 | 52.300 | 73 % km | 🟢 Al día | — |
| RVA101 | Frenos | 40.000 · 2026-03-15 | 52.300 | 61,5 % km | 🟢 Al día | — |
| RVA102 | Aceite | 72.000 · 2026-07-15 | 81.450 | 94,5 % km | 🟡 Por vencer (km) | Alerta `DueSoon` |
| RVA103 | Aceite | 28.000 · 2026-05-25 | 30.200 | vence 2026-11-25 (quedan 13 de 184 días) | 🟡 Por vencer (tiempo) | Alerta `DueSoon` |
| RVA104 | Aceite | 92.000 · 2026-09-20 | 98.000 | 60 % km | 🟢 Al día | — |
| RVA104 | Frenos | 77.000 · 2025-12-01 | 98.000 | 105 % km | 🔴 Vencida (km) | Alerta `Overdue` + **orden automática** |
| TAN201 | Aceite | 110.000 · 2026-09-01 | 120.800 | 108 % km | 🔴 Vencida (km) | Alerta + **orden** |
| TAN201 | Frenos | 105.000 · 2026-06-10 | 120.800 | 79 % km | 🟢 Al día | — |
| TAN202 | Aceite | 15.000 · 2026-04-20 | 19.000 | venció 2026-10-20 | 🔴 Vencida (tiempo) | Alerta + **orden** |
| TAN203 | Aceite | 60.000 · 2026-05-01 | 70.100 | 101 % km y venció 2026-11-01 | 🔴 Vencida (**km y tiempo**) | Alerta `DistanceAndTime` + **orden** |
| EVA301 | Aceite / Frenos | (vacío) | 0 | 0 % | 🟢 Al día (vehículo nuevo) | — |
| EVA302 | Frenos | 27.000 · 2026-02-10 | 45.500 | 92,5 % km | 🟡 Por vencer (km) | Alerta `DueSoon` |
| EVA302 | Llantas | 10.000 · 2025-08-01 | 45.500 | 88,75 % km | 🟢 Al día | — |
| EVA30D | Aceite (moto: 3.000 km / 3 meses / **20 %**) | 10.000 · 2026-10-01 | 12.500 | 83 % km | 🟡 Por vencer (km) **por el ajuste del 20 %** | Alerta `DueSoon`. Con el 10 % por defecto estaría al día (A-02) |

**Totales esperados:** 4 alertas por vencer, 4 alertas vencidas y **4 órdenes de servicio automáticas** (RVA104 frenos, TAN201, TAN202 y TAN203 aceite).

## 2. Validación (`samples/import/con-errores/`)

Se cargan **después** de la carga válida para mostrar la vista previa de errores.

| Archivo | Fila (placa) | Resultado esperado | Nivel |
|---|---|---|---|
| `vehiculos.csv` | RVA105 | ✅ Válida | — |
| | RVA106 (primera) | ✅ Válida | — |
| | RVA106 (repetida) | ❌ Placa duplicada en el archivo | Formato |
| | RV-107 | ❌ Formato de placa inválido | Formato |
| | TAN204 | ❌ NIT del dueño 999999999 no existe | Negocio (dry-run) |
| | TAN205 | ❌ NIT del taller 900199999 no existe | Negocio (dry-run) |
| | EVA304 | ❌ Año 1890 fuera de rango | Formato |
| | EVA305 | ❌ Odómetro negativo | Formato |
| | EVA306 | ❌ Marca obligatoria | Formato |
| | EVA307 | ❌ El VIN debe tener 17 caracteres | Formato |
| | EVA308 | ❌ El responsable no es un aprobador registrado | Negocio (dry-run) |
| | RVA101 | ⏭️ Omitida: la placa ya existe (idempotencia) | Negocio (dry-run) |
| `rutinas.csv` | RVA102 · BRAKES | ✅ Válida | — |
| | RVA102 · ENGINE_X | ❌ Código de rutina inexistente | Negocio (dry-run) |
| | RVA103 · BRAKES | ❌ km del último servicio (35.000) mayor que el odómetro (30.200) | Negocio (dry-run) |
| | TAN202 · TIRES | ❌ Fecha del último servicio en el futuro | Formato |
| | ZZZ999 | ❌ Vehículo no registrado | Negocio (dry-run) |
| | EVA302 · OIL_CHANGE | ❌ % «por vencer» 150 fuera de rango (0–100) | Formato |
| | EVA30D · BRAKES | ❌ km no numérico | Formato |
| | RVA104 · TIRES | ❌ Intervalo de km debe ser mayor que 0 | Formato |
| | RVA101 · OIL_CHANGE | ⏭️ Omitida: la rutina ya existe para el vehículo | Negocio (dry-run) |

## 3. Eventos de Kafka (`samples/kafka/`)

Se publican desde Kafka UI o desde el endpoint de Simulators (ADR-0017). El formato es `{ key, value }` por línea.

### `telemetry-odometer-readings.v1.jsonl` (topic `telemetry.odometer-readings.v1`, clave = placa)

| # | Mensaje | Resultado esperado |
|---|---|---|
| 1 | RVA101 · 53.000 km | ✅ Aceptada. El aceite queda en 80 %, al día |
| 2 | RVA101 · 53.000 km (repetida) | ⏭️ Ignorada: no es mayor (RN-6) |
| 3 | RVA101 · 52.950 km (fuera de orden) | ⏭️ Ignorada: es menor (RN-6, A-09) |
| 4 | RVA101 · 54.100 km | 🟡 Aceite por vencer (91 %). Alerta `DueSoon` |
| 5 | RVA101 · 55.050 km | 🔴 Aceite vencido. La alerta `DueSoon` se resuelve por escalamiento y se crean una alerta `Overdue` y una **orden** |
| 6 | RVA102 · 82.100 km | 🔴 Aceite vencido (estaba por vencer). Se escala y se crea una **orden** |
| 7 | XYZ999 · 15.000 km | ⏭️ Vehículo desconocido: se reintenta unos segundos y luego se ignora con log (A-13) |

### `maintenance-time-evaluation-requested.v1.jsonl` (revisión por fecha, ADR-0017)

| `asOfDate` | Resultado esperado |
|---|---|
| 2027-01-20 | 🔴 RVA103 aceite vencido por tiempo (venció 2026-11-25) → **orden**. 🔴 EVA30D aceite vencido por tiempo (venció 2027-01-01) → **orden** |
| 2027-02-10 | 🟡 RVA101 frenos por vencer por tiempo (quedan 33 de 365 días ≤ 10 %). 🔴 EVA302 frenos vencidos por tiempo (2027-02-10) → **orden** |

> La revisión por fecha es monótona: después de publicar 2027-02-10, una fecha anterior se ignora. Para repetir la demo desde cero hay que reiniciar la base de datos y volver a cargar los CSV.
