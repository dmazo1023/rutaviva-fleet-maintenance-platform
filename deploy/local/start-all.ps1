param(
    [switch]$SkipBuild,
    [switch]$SkipMigrations,
    [switch]$SkipUi,
    [string]$ConnectionString = 'Server=hydra.gearhost.io;Database=rutavivaBD;User Id=ua6p7u67svj_rutavivabd;Password=UABQX9#7DEh76bnv;TrustServerCertificate=True;Connect Timeout=60;',
    [string]$NodePath = (Join-Path $env:APPDATA 'nvm\v24.15.0')
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$LogsPath = Join-Path $PSScriptRoot 'logs'
$PidsFile = Join-Path $LogsPath 'pids.txt'
$EventBusUrl = 'http://localhost:5100'
$UiPath = Join-Path $RepositoryRoot 'src\Web\rutaviva-fleet-maintenance-ui'
$MinimumNodeMajor = 22

$Services = @(
    @{ Name = 'EventBus.Hub'; Api = 'src\Platform\EventBus\RutaViva.EventBus.Hub'; Infrastructure = 'src\Platform\EventBus\RutaViva.EventBus.Infrastructure'; Context = 'EventBusDbContext'; Url = $EventBusUrl },
    @{ Name = 'Maintenance.Api'; Api = 'src\Services\Maintenance\RutaViva.Maintenance.Api'; Infrastructure = 'src\Services\Maintenance\RutaViva.Maintenance.Infrastructure'; Context = 'MaintenanceDbContext'; Url = 'http://localhost:5101' },
    @{ Name = 'Fleet.Api'; Api = 'src\Services\Fleet\RutaViva.Fleet.Api'; Infrastructure = 'src\Services\Fleet\RutaViva.Fleet.Infrastructure'; Context = 'FleetDbContext'; Url = 'http://localhost:5102' },
    @{ Name = 'Customers.Api'; Api = 'src\Services\Customers\RutaViva.Customers.Api'; Infrastructure = 'src\Services\Customers\RutaViva.Customers.Infrastructure'; Context = 'CustomersDbContext'; Url = 'http://localhost:5103' },
    @{ Name = 'Workshops.Api'; Api = 'src\Services\Workshops\RutaViva.Workshops.Api'; Infrastructure = 'src\Services\Workshops\RutaViva.Workshops.Infrastructure'; Context = 'WorkshopsDbContext'; Url = 'http://localhost:5104' },
    @{ Name = 'ServiceOrders.Api'; Api = 'src\Services\ServiceOrders\RutaViva.ServiceOrders.Api'; Infrastructure = 'src\Services\ServiceOrders\RutaViva.ServiceOrders.Infrastructure'; Context = 'ServiceOrdersDbContext'; Url = 'http://localhost:5105' }
)

function Invoke-Step([string]$label, [scriptblock]$action) {
    Write-Host "==> $label"
    & $action
    if ($LASTEXITCODE -ne 0) { throw "$label failed with exit code $LASTEXITCODE." }
}

function Update-Databases {
    foreach ($service in $Services) {
        Invoke-Step "Applying migrations of $($service.Context)" {
            dotnet tool run dotnet-ef database update --no-build `
                --project (Join-Path $RepositoryRoot $service.Infrastructure) `
                --startup-project (Join-Path $RepositoryRoot $service.Api) `
                --context $service.Context `
                --connection $ConnectionString
        }
    }
}

function Start-Service($service) {
    $output = Join-Path $LogsPath "$($service.Name).log"
    $errors = Join-Path $LogsPath "$($service.Name).err.log"
    $env:ConnectionStrings__RutaViva = $ConnectionString
    $env:EventBus__HubUrl = $EventBusUrl
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $arguments = @('run', '--no-build', '--project', (Join-Path $RepositoryRoot $service.Api), '--urls', $service.Url)
    $process = Start-Process dotnet -ArgumentList $arguments -PassThru -WindowStyle Hidden -RedirectStandardOutput $output -RedirectStandardError $errors
    Add-Content $PidsFile $process.Id
    Write-Host ("    {0,-16} {1}  (pid {2}, log {3})" -f $service.Name, $service.Url, $process.Id, $output)
}

function Start-Ui {
    if (Test-Path $NodePath) { $env:Path = "$NodePath;$env:Path" }
    $nodeMajor = [int]((node -v).TrimStart('v').Split('.')[0])
    if ($nodeMajor -lt $MinimumNodeMajor) { throw "Angular 22 needs Node $MinimumNodeMajor or later; found $(node -v). Use -NodePath or install Node LTS." }
    if (-not (Test-Path (Join-Path $UiPath 'node_modules'))) {
        Invoke-Step 'Installing UI dependencies' { npm --prefix $UiPath install }
    }
    $output = Join-Path $LogsPath 'ui.log'
    $errors = Join-Path $LogsPath 'ui.err.log'
    $process = Start-Process npm.cmd -ArgumentList @('--prefix', $UiPath, 'start') -PassThru -WindowStyle Hidden -RedirectStandardOutput $output -RedirectStandardError $errors
    Add-Content $PidsFile $process.Id
    Write-Host ("    {0,-16} {1}  (pid {2}, log {3})" -f 'UI', 'http://localhost:4200', $process.Id, $output)
}

New-Item -ItemType Directory -Force $LogsPath | Out-Null
Set-Content $PidsFile ''
Set-Location $RepositoryRoot

Write-Host ("==> Database: {0}" -f ($ConnectionString -replace 'Password=[^;]*', 'Password=***'))
if (-not $SkipBuild) { Invoke-Step 'Building RutaViva.slnx' { dotnet build RutaViva.slnx -nologo -v q } }
if (-not $SkipMigrations) { Update-Databases }

Write-Host '==> Starting services'
foreach ($service in $Services) { Start-Service $service }
if (-not $SkipUi) { Start-Ui }

Write-Host ''
Write-Host 'RutaViva is starting. When the services answer, load the demo data with:'
Write-Host '    .\deploy\local\load-demo-data.ps1'
Write-Host 'Stop everything with:'
Write-Host '    .\deploy\local\stop-all.ps1'
