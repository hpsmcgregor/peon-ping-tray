#requires -version 5
$ErrorActionPreference = 'Stop'
$script:fail = 0
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Assert-Equal($expected, $actual, $msg) {
  if ("$expected" -ne "$actual") { Write-Host "FAIL: $msg`n  expected: $expected`n  actual:   $actual" -ForegroundColor Red; $script:fail++ }
  else { Write-Host "PASS: $msg" -ForegroundColor Green }
}
function Assert-True($cond, $msg) {
  if ($cond) { Write-Host "PASS: $msg" -ForegroundColor Green } else { Write-Host "FAIL: $msg" -ForegroundColor Red; $script:fail++ }
}

# Build once.
$exe = & (Join-Path $root 'build.ps1') | Select-Object -Last 1
Assert-True (Test-Path $exe) "build produced exe at $exe"

# Invoke the (WinExe) exe and reliably wait for it + capture stdout. A plain
# "& $exe" does not reliably wait for / capture a GUI-subsystem app's output,
# so use Start-Process -Wait with a redirected stdout file.
function Invoke-Exe([string[]]$ExeArgs) {
  $so = [System.IO.Path]::GetTempFileName()
  $se = [System.IO.Path]::GetTempFileName()
  try {
    $p = Start-Process -FilePath $exe -ArgumentList $ExeArgs -Wait -PassThru -NoNewWindow `
           -RedirectStandardOutput $so -RedirectStandardError $se
    return [pscustomobject]@{ ExitCode = $p.ExitCode; Out = (Get-Content $so -Raw) }
  } finally {
    Remove-Item $so, $se -Force -ErrorAction SilentlyContinue
  }
}

function Dump($hookDir, $groupsConf) {
  $a = @('--dump', $hookDir); if ($groupsConf) { $a += $groupsConf }
  return ((Invoke-Exe $a).Out | ConvertFrom-Json)
}

function New-Fixture { param([bool]$Enabled = $true, [string]$DefaultPack = 'peon')
  $dir = Join-Path ([System.IO.Path]::GetTempPath()) ("ppt_" + [System.Guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  ($([pscustomobject]@{ enabled = $Enabled; default_pack = $DefaultPack; volume = 0.5 }) | ConvertTo-Json) |
    Set-Content -Path (Join-Path $dir 'config.json') -Encoding UTF8
  return $dir
}

function Add-Pack { param([string]$HookDir, [string]$Id, [string]$DisplayName, [switch]$NoWav)
  $p = Join-Path (Join-Path $HookDir 'packs') $Id
  New-Item -ItemType Directory -Force -Path (Join-Path $p 'sounds') | Out-Null
  ($([pscustomobject]@{ display_name = $DisplayName }) | ConvertTo-Json) |
    Set-Content -Path (Join-Path $p 'openpeon.json') -Encoding UTF8
  if (-not $NoWav) {
    Set-Content -Path (Join-Path $p 'sounds\a_first.wav') -Value 'x' -Encoding ASCII
    Set-Content -Path (Join-Path $p 'sounds\b_second.wav') -Value 'x' -Encoding ASCII
  }
}

# --- Task 1 smoke ---
$fx = New-Fixture
$d = Dump $fx $null
Assert-Equal $fx $d.hookDir "--dump echoes hookDir"

# --- Task 2: state mapping ---
$on  = Dump (New-Fixture -Enabled $true  -DefaultPack 'peon') $null
Assert-Equal 'ON'   $on.state       "enabled:true => ON"
Assert-Equal 'True' $on.configFound "config found"
Assert-Equal 'peon' $on.defaultPack "default_pack read"

$off = Dump (New-Fixture -Enabled $false -DefaultPack 'glados') $null
Assert-Equal 'OFF' $off.state "enabled:false => OFF"

$missingDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ppt_none_" + [System.Guid]::NewGuid().ToString('N'))
$unk = Dump $missingDir $null
Assert-Equal 'UNKNOWN' $unk.state "missing config => UNKNOWN"
Assert-Equal 'False'   $unk.configFound "missing config not found"

# --- Task 3: pack discovery ---
$fx3 = New-Fixture -Enabled $true -DefaultPack 'peon'
Add-Pack -HookDir $fx3 -Id 'peon'   -DisplayName 'Orc Peon'
Add-Pack -HookDir $fx3 -Id 'glados' -DisplayName 'GLaDOS'
Add-Pack -HookDir $fx3 -Id 'nodisp' -DisplayName '' -NoWav
$d3 = Dump $fx3 $null
$ids = @($d3.packs | ForEach-Object { $_.id })
Assert-True ($ids -contains 'peon' -and $ids -contains 'glados') "packs discovered"
$peon = $d3.packs | Where-Object { $_.id -eq 'peon' }
Assert-Equal 'Orc Peon' $peon.displayName "display_name read"
Assert-Equal 'True'     $peon.isCurrent   "current pack flagged"
Assert-True ($peon.previewWav -like '*a_first.wav') "first wav chosen for preview"
$nod = $d3.packs | Where-Object { $_.id -eq 'nodisp' }
Assert-Equal 'nodisp' $nod.displayName "empty display_name falls back to id"
Assert-True ($null -eq $nod.previewWav) "no wav => null preview"

# --- Task 4: grouping ---
$conf = Join-Path ([System.IO.Path]::GetTempPath()) ("ppt_groups_" + [System.Guid]::NewGuid().ToString('N') + ".conf")
Set-Content -Path $conf -Encoding UTF8 -Value @"
sc_*   = StarCraft
peon   = Warcraft
"@
$fx4 = New-Fixture -Enabled $true -DefaultPack 'peon'
Add-Pack -HookDir $fx4 -Id 'peon'   -DisplayName 'Orc Peon'
Add-Pack -HookDir $fx4 -Id 'sc_scv' -DisplayName 'SCV'
Add-Pack -HookDir $fx4 -Id 'duke'   -DisplayName 'Duke'      # matches nothing => Other
$d4 = Dump $fx4 $conf
Assert-Equal 'Warcraft'  (($d4.packs | Where-Object { $_.id -eq 'peon' }).group)   "peon => Warcraft"
Assert-Equal 'StarCraft' (($d4.packs | Where-Object { $_.id -eq 'sc_scv' }).group) "sc_* glob => StarCraft"
Assert-Equal 'Other'     (($d4.packs | Where-Object { $_.id -eq 'duke' }).group)   "unmatched => Other"
Assert-Equal 'StarCraft,Warcraft,Other' (($d4.groupsOrder) -join ',') "group order = config order, Other last"

# --- Task 5: peon.ps1 invocation ---
$fx5 = New-Fixture -Enabled $true -DefaultPack 'peon'
$marker = Join-Path $fx5 'invoked.txt'
Set-Content -Path (Join-Path $fx5 'peon.ps1') -Encoding UTF8 -Value '$args -join " " | Set-Content -Path (Join-Path $PSScriptRoot "invoked.txt"); exit 0'
$res = Invoke-Exe @('--run-peon', $fx5, 'packs', 'use', 'glados')
Assert-Equal 'OK' ($res.Out).Trim() "--run-peon returns OK on exit 0"
Assert-True (Test-Path $marker) "fake peon.ps1 was invoked"
Assert-Equal 'packs use glados' ((Get-Content $marker -Raw).Trim()) "args forwarded to peon.ps1"

$fx5b = New-Fixture   # no peon.ps1 present
$res2 = Invoke-Exe @('--run-peon', $fx5b, 'pause')
Assert-Equal 'FAIL' ($res2.Out).Trim() "--run-peon returns FAIL when peon.ps1 missing"

Write-Host ""
if ($script:fail -gt 0) { Write-Host "$($script:fail) failure(s)." -ForegroundColor Red; exit 1 }
Write-Host "All tests passed." -ForegroundColor Green
