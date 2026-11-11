## Purpose

Validar los archivos CSV de la carga inicial antes de que lleguen a los servicios dueños: aceptar los formatos que exporta Excel, rechazar archivos mal formados y señalar cada fila con errores de formato con su causa (ADR-0022, niveles 1 y 2).

## ADDED Requirements

### Requirement: Archivos identificados por tipo de entidad
Cada archivo de una carga SHALL declararse con su tipo de entidad (`Workshops`, `Customers`, `Approvers`, `Vehicles` o `Routines`). Una carga MUST contener entre 1 y 5 archivos y como máximo uno por tipo de entidad. Un tipo de entidad desconocido o repetido MUST rechazarse con `400` sin crear el trabajo.

#### Scenario: Carga parcial
- **WHEN** el usuario sube solo `vehiculos.csv` (`Vehicles`) y `rutinas.csv` (`Routines`) de `samples/import/con-errores/`
- **THEN** se crea un trabajo con 2 etapas, en orden `Vehicles` (4) y `Routines` (5)

#### Scenario: Tipo de entidad repetido
- **WHEN** el usuario sube dos archivos declarados como `Vehicles`
- **THEN** la respuesta es `400` y no se crea ningún trabajo

### Requirement: Límites de tamaño y de filas por archivo
Un archivo MUST pesar como máximo 5 MB y tener como máximo 5.000 filas de datos. Un archivo que excede un límite MUST marcar su etapa como `Failed` con el motivo, sin validar sus filas.

#### Scenario: Archivo con 5.001 filas
- **WHEN** se sube un `vehiculos.csv` con el encabezado y 5.001 filas de datos
- **THEN** la etapa `Vehicles` queda `Failed` con el motivo `ImportFile.TooManyRows` y ninguna fila registrada

#### Scenario: Archivo de 5 MB exactos
- **WHEN** se sube un archivo de exactamente 5 MB con encabezados válidos
- **THEN** el archivo se procesa

### Requirement: Detección de separador y codificación
El sistema SHALL detectar el separador (`;` o `,`) a partir del encabezado y la codificación entre UTF-8 con BOM, UTF-8 sin BOM y Windows-1252, y SHALL registrar ambos en la etapa. Los textos con tildes y eñes MUST leerse sin alteración en las tres codificaciones.

#### Scenario: Excel en configuración regional de Colombia
- **WHEN** se sube `talleres.csv` guardado en Windows-1252 con separador `;`
- **THEN** la etapa registra `Delimiter = ';'` y `FileEncoding = Windows1252`, y el nombre «Autofrenos Medellín» se lee con su tilde

#### Scenario: UTF-8 con BOM y comas
- **WHEN** se sube `clientes.csv` en UTF-8 con BOM y separador `,`
- **THEN** la etapa registra `Delimiter = ','` y `FileEncoding = Utf8Bom`, y el BOM no forma parte del primer encabezado

### Requirement: Encabezados exactos según la plantilla
Los encabezados de cada archivo MUST coincidir exactamente, en nombre y orden, con la plantilla de su tipo de entidad, tras recortar espacios. Si no coinciden, la etapa MUST quedar `Failed` con el motivo que nombra los encabezados esperados.

| Entidad | Encabezados |
|---|---|
| `Workshops` | `nit;nombre;correo;telefono;direccion;ciudad` |
| `Customers` | `nit;nombre;correo;telefono` |
| `Approvers` | `nit_cliente;nombre;correo` |
| `Vehicles` | `placa;vin;marca;modelo;anio;nit_dueno;nit_taller;correo_responsable;odometro_km` |
| `Routines` | `placa;codigo_rutina;km_ultimo_servicio;fecha_ultimo_servicio;intervalo_km;intervalo_meses;porcentaje_por_vencer` |

#### Scenario: Columna faltante
- **WHEN** se sube un `vehiculos.csv` sin la columna `odometro_km`
- **THEN** la etapa `Vehicles` queda `Failed` con el motivo `ImportFile.InvalidHeaders`, que lista los encabezados esperados

#### Scenario: Plantillas de ejemplo
- **WHEN** se suben los 5 archivos de `samples/import/`
- **THEN** ninguna etapa falla por encabezados

### Requirement: Validación de formato por fila
Cada fila SHALL validarse contra las reglas de formato de su entidad; una fila que incumple al menos una MUST quedar `Invalid` con un error por regla incumplida (código y mensaje en español). Las columnas vacías opcionales MUST tomarse como ausentes. Las fechas MUST aceptarse en `yyyy-MM-dd` y en `dd/MM/yyyy`. Reglas mínimas:

| Entidad | Reglas |
|---|---|
| Todas | NIT con 6 a 15 dígitos; correos con formato válido; textos obligatorios no vacíos |
| `Workshops` | `nit`, `nombre`, `ciudad` obligatorios |
| `Customers` | `nit`, `nombre` obligatorios |
| `Approvers` | `nit_cliente`, `nombre`, `correo` obligatorios |
| `Vehicles` | placa `^[A-Z]{3}[0-9]{2}[0-9A-Z]$`; `vin` opcional de 17 caracteres; `marca`, `modelo`, `nit_dueno`, `nit_taller` obligatorios; `anio` entre 1950 y el año actual + 1; `odometro_km` entero ≥ 0; `correo_responsable` opcional |
| `Routines` | placa con el mismo patrón; `codigo_rutina` obligatorio; `km_ultimo_servicio` entero ≥ 0 si está; `fecha_ultimo_servicio` no futura si está; `intervalo_km` y `intervalo_meses` enteros > 0 si están; `porcentaje_por_vencer` mayor que 0 y menor que 100 si está |

#### Scenario: Errores de formato en vehículos con errores
- **WHEN** se valida `samples/import/con-errores/vehiculos.csv`
- **THEN** quedan `Invalid` por formato: `RV-107` (placa inválida), `EVA304` (año 1890 fuera de rango), `EVA305` (odómetro negativo), `EVA306` (marca obligatoria) y `EVA307` (VIN de 11 caracteres)

#### Scenario: Errores de formato en rutinas con errores
- **WHEN** se valida `samples/import/con-errores/rutinas.csv` con la fecha actual 2026-11-12
- **THEN** quedan `Invalid` por formato: `TAN202 · TIRES` (fecha 2027-01-01 en el futuro), `EVA302 · OIL_CHANGE` (porcentaje 150 fuera de rango), `EVA30D · BRAKES` (km `abc` no numérico) y `RVA104 · TIRES` (intervalo de km 0)

#### Scenario: Vehículo nuevo sin último servicio
- **WHEN** se valida la fila `EVA301;OIL_CHANGE;;;;;` de `samples/import/rutinas.csv`
- **THEN** la fila no tiene errores de formato

#### Scenario: Fecha en formato colombiano
- **WHEN** una rutina trae `fecha_ultimo_servicio = 15/03/2026`
- **THEN** se interpreta como 2026-03-15 sin error

#### Scenario: Límite del porcentaje
- **WHEN** una rutina trae `porcentaje_por_vencer` igual a 0 o igual a 100
- **THEN** la fila queda `Invalid` por porcentaje fuera de rango

### Requirement: Duplicados dentro del mismo archivo
Dentro de un archivo, la primera aparición de una clave natural SHALL validarse normalmente y cada aparición posterior MUST quedar `Invalid` por duplicado. Claves: `Workshops` y `Customers` por `nit`; `Approvers` por `nit_cliente` + `correo`; `Vehicles` por `placa` (y por `vin` cuando está); `Routines` por `placa` + `codigo_rutina`.

#### Scenario: Placa repetida
- **WHEN** se valida `samples/import/con-errores/vehiculos.csv`, donde `RVA106` aparece dos veces
- **THEN** la primera fila de `RVA106` no tiene errores de formato y la segunda queda `Invalid` con `ImportRow.DuplicateInFile`

### Requirement: Plantillas descargables (Recortable)
El sistema SHALL entregar, para cada tipo de entidad, un CSV con solo el encabezado de su plantilla, en UTF-8 con BOM, separador `;` y nombre de archivo en español.

#### Scenario: Descargar plantilla de vehículos
- **WHEN** el usuario pide `GET /api/v1/imports/templates/Vehicles.csv`
- **THEN** recibe `text/csv` con `Content-Disposition: attachment; filename="vehiculos.csv"` y una sola línea con los encabezados de `Vehicles`

#### Scenario: Entidad inexistente
- **WHEN** el usuario pide `GET /api/v1/imports/templates/drivers.csv`
- **THEN** la respuesta es `404`
