#requires -version 5
param(
  [string]$InstallDir = (Join-Path $env:LOCALAPPDATA 'PeonPingTray'),
  [string]$StartupDir = [Environment]::GetFolderPath('Startup')
)
$ErrorActionPreference = 'SilentlyContinue'
Get-Process -Name 'PeonPingTray' | Stop-Process -Force
$lnk = Join-Path $StartupDir 'PeonPingTray.lnk'
if (Test-Path $lnk) { Remove-Item $lnk -Force }
if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }
Write-Host "Uninstalled PeonPingTray (peon-ping itself was left untouched)." -ForegroundColor Green
