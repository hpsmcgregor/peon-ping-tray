#requires -version 5
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $root 'PeonPingTray\PeonPingTray.csproj'
$pub  = Join-Path $root 'PeonPingTray\bin\publish'
$exe  = Join-Path $pub 'PeonPingTray.exe'

# Framework-dependent, single-file (small exe; requires the .NET 10 Desktop Runtime on the target).
& dotnet publish $proj -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -p:DebugType=none -o $pub
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host "Built: $exe" -ForegroundColor Green
$exe
