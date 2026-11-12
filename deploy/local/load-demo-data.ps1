param(
    [string]$MaintenanceUrl = 'http://localhost:5101',
    [string]$FleetUrl = 'http://localhost:5102',
    [string]$CustomersUrl = 'http://localhost:5103',
    [string]$WorkshopsUrl = 'http://localhost:5104',
    [string]$SamplesPath = (Join-Path $PSScriptRoot '..\..\samples\import'),
    [int]$PropagationTimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
$Delimiter = ';'
$PollIntervalSeconds = 2
$PendingPropagationCodes = @('Vehicle.OwnerNotFound', 'Vehicle.WorkshopNotFound', 'Vehicle.ResponsibleNotFound', 'MaintenancePlan.NotFound')

function Read-Sample([string]$name) {
    Import-Csv -Path (Join-Path $SamplesPath "$name.csv") -Delimiter $Delimiter -Encoding UTF8
}

function Optional([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $null }
    return $value.Trim()
}

function OptionalNumber([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $null }
    return [long]$value
}

function Invoke-Bulk([string]$url, [object[]]$rows, [bool]$dryRun) {
    $body = @{ rows = $rows } | ConvertTo-Json -Depth 5
    $target = "$url`?dryRun=$($dryRun.ToString().ToLowerInvariant())"
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
    return Invoke-RestMethod -Method Post -Uri $target -Body $bytes -ContentType 'application/json; charset=utf-8'
}

function Test-Propagated([object[]]$results) {
    $pending = $results | Where-Object { $_.errors | Where-Object { $PendingPropagationCodes -contains $_.code } }
    return -not $pending
}

function Wait-Propagation([string]$url, [object[]]$rows, [string]$label) {
    $deadline = (Get-Date).AddSeconds($PropagationTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Propagated (Invoke-Bulk $url $rows $true)) { return }
        Write-Host "  waiting for $label to propagate through the EventBus..."
        Start-Sleep -Seconds $PollIntervalSeconds
    }
    throw "Timeout waiting for $label to propagate."
}

function Write-Summary([string]$label, [object[]]$results) {
    $groups = $results | Group-Object outcome | ForEach-Object { "$($_.Name)=$($_.Count)" }
    Write-Host ("{0}: {1}" -f $label, ($groups -join ', '))
    $results | Where-Object { $_.outcome -eq 'Invalid' } | ForEach-Object {
        Write-Host ("  row {0}: {1}" -f $_.rowNumber, (($_.errors | ForEach-Object { $_.code }) -join ', '))
    }
}

function Send-Batch([string]$label, [string]$url, [object[]]$rows) {
    Write-Summary $label (Invoke-Bulk $url $rows $false)
}

$index = 0
$workshops = @(Read-Sample 'talleres' | ForEach-Object {
    $index++
    @{ rowNumber = $index; taxId = $_.nit; name = $_.nombre; email = $_.correo; phone = (Optional $_.telefono); address = $_.direccion; city = $_.ciudad }
})

$index = 0
$customers = @(Read-Sample 'clientes' | ForEach-Object {
    $index++
    @{ rowNumber = $index; taxId = $_.nit; name = $_.nombre; email = $_.correo; phone = (Optional $_.telefono) }
})

$index = 0
$approvers = @(Read-Sample 'aprobadores' | ForEach-Object {
    $index++
    @{ rowNumber = $index; customerTaxId = $_.nit_cliente; fullName = $_.nombre; email = $_.correo }
})

$index = 0
$vehicles = @(Read-Sample 'vehiculos' | ForEach-Object {
    $index++
    @{
        rowNumber = $index; plate = $_.placa; vin = (Optional $_.vin); brand = $_.marca; model = $_.modelo; modelYear = [int]$_.anio
        ownerTaxId = $_.nit_dueno; workshopTaxId = $_.nit_taller; responsibleApproverEmail = (Optional $_.correo_responsable)
        initialOdometerKm = (OptionalNumber $_.odometro_km)
    }
})

$index = 0
$routines = @(Read-Sample 'rutinas' | ForEach-Object {
    $index++
    @{
        rowNumber = $index; plate = $_.placa; routineTypeCode = $_.codigo_rutina
        lastServiceKm = (OptionalNumber $_.km_ultimo_servicio); lastServiceDate = (Optional $_.fecha_ultimo_servicio)
        intervalKm = (OptionalNumber $_.intervalo_km); intervalMonths = (OptionalNumber $_.intervalo_meses)
        dueSoonThresholdPercent = (OptionalNumber $_.porcentaje_por_vencer)
    }
})

Send-Batch 'Workshops' "$WorkshopsUrl/api/v1/workshops/bulk" $workshops
Send-Batch 'Customers' "$CustomersUrl/api/v1/customers/bulk" $customers
Send-Batch 'Approvers' "$CustomersUrl/api/v1/customers/approvers/bulk" $approvers

$vehiclesUrl = "$FleetUrl/api/v1/fleet/vehicles/bulk"
Wait-Propagation $vehiclesUrl $vehicles 'customers, workshops and approvers'
Send-Batch 'Vehicles' $vehiclesUrl $vehicles

$routinesUrl = "$MaintenanceUrl/api/v1/maintenance/routines/bulk"
Wait-Propagation $routinesUrl $routines 'maintenance plans'
Send-Batch 'Routines' $routinesUrl $routines

Write-Host 'Demo data loaded.'
