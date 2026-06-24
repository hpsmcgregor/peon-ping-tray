#requires -version 5
param(
  [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'PeonPingTray'),
  [string]$StartupDir = [Environment]::GetFolderPath('Startup'),
  [switch]$NoLaunch
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe  = Join-Path $root 'PeonPingTray\bin\publish\PeonPingTray.exe'

$peon = Join-Path $env:USERPROFILE '.claude\hooks\peon-ping\peon.ps1'
if (-not (Test-Path $peon)) {
  Write-Host "Warning: peon-ping not found at $peon. The tray will show 'unknown' until it's installed." -ForegroundColor Yellow
}

if (-not (Test-Path $exe)) {
  Write-Host "Building PeonPingTray..."
  $exe = & (Join-Path $root 'build.ps1') | Select-Object -Last 1
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item -Path $exe -Destination (Join-Path $InstallDir 'PeonPingTray.exe') -Force

$confSrc = Join-Path $root 'config\peonping-groups.conf'
$confDst = Join-Path $InstallDir 'peonping-groups.conf'
if (-not (Test-Path $confDst)) { Copy-Item -Path $confSrc -Destination $confDst -Force }

# The tray reads groups.conf from %LOCALAPPDATA%\PeonPingTray; copy there too when installing elsewhere (tests).
$appData = Join-Path $env:LOCALAPPDATA 'PeonPingTray'
if ($InstallDir -ne $appData) {
  New-Item -ItemType Directory -Force -Path $appData | Out-Null
  if (-not (Test-Path (Join-Path $appData 'peonping-groups.conf'))) {
    Copy-Item -Path $confSrc -Destination (Join-Path $appData 'peonping-groups.conf') -Force
  }
}

New-Item -ItemType Directory -Force -Path $StartupDir | Out-Null
$lnk = Join-Path $StartupDir 'PeonPingTray.lnk'
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut($lnk)
$sc.TargetPath = (Join-Path $InstallDir 'PeonPingTray.exe')
$sc.WorkingDirectory = $InstallDir
$sc.Description = 'Peon-Ping tray toggle'
$sc.Save()

Write-Host "Installed to $InstallDir; startup shortcut at $lnk" -ForegroundColor Green
if (-not $NoLaunch) { Start-Process -FilePath (Join-Path $InstallDir 'PeonPingTray.exe') }
