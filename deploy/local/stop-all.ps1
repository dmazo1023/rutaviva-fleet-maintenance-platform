$ErrorActionPreference = 'Continue'
$PidsFile = Join-Path $PSScriptRoot 'logs\pids.txt'

if (-not (Test-Path $PidsFile)) {
    Write-Host 'No processes registered by start-all.ps1.'
    return
}

Get-Content $PidsFile | Where-Object { $_ -match '^\d+$' } | ForEach-Object {
    taskkill /PID $_ /T /F 2>$null | Out-Null
    Write-Host "Stopped process tree $_"
}

Set-Content $PidsFile ''
