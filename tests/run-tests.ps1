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
# pack whose only sound is an mp3 (no wav) — preview must still resolve
$mp3p = Join-Path (Join-Path $fx3 'packs') 'mp3only'
New-Item -ItemType Directory -Force -Path (Join-Path $mp3p 'sounds') | Out-Null
($([pscustomobject]@{ display_name = 'MP3 Only' }) | ConvertTo-Json) |
  Set-Content -Path (Join-Path $mp3p 'openpeon.json') -Encoding UTF8
Set-Content -Path (Join-Path $mp3p 'sounds\clip.mp3') -Value 'x' -Encoding ASCII
$d3 = Dump $fx3 $null
$ids = @($d3.packs | ForEach-Object { $_.id })
Assert-True ($ids -contains 'peon' -and $ids -contains 'glados') "packs discovered"
$peon = $d3.packs | Where-Object { $_.id -eq 'peon' }
Assert-Equal 'Orc Peon' $peon.displayName "display_name read"
Assert-Equal 'True'     $peon.isCurrent   "current pack flagged"
Assert-True ($peon.previewSound -like '*a_first.wav') "first sound chosen for preview"
$nod = $d3.packs | Where-Object { $_.id -eq 'nodisp' }
Assert-Equal 'nodisp' $nod.displayName "empty display_name falls back to id"
Assert-True ($null -eq $nod.previewSound) "no sound => null preview"
$mp3 = $d3.packs | Where-Object { $_.id -eq 'mp3only' }
Assert-True ($mp3.previewSound -like '*clip.mp3') "mp3-only pack resolves preview"

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

# --- Task 6: icon rendering ---
$iconOut = (Invoke-Exe @('--icon-selftest')).Out
Assert-Equal '16x16 16x16 16x16' ("$iconOut").Trim() "icons render at 16x16 for all 3 states"

# --- Toggle sets only top-level enabled, never the nested tts.enabled ---
# Regression: peon-ping's pause/resume regex flips every "enabled" in the file,
# silently re-enabling TTS. The tray edits the parsed root flag instead.
$fxT = Join-Path ([System.IO.Path]::GetTempPath()) ("ppt_tts_" + [System.Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $fxT | Out-Null
$cfgT = Join-Path $fxT 'config.json'
@'
{
  "enabled": true,
  "default_pack": "peon",
  "volume": 0.5,
  "tts": { "enabled": true, "voice": "default" }
}
'@ | Set-Content -Path $cfgT -Encoding UTF8

Assert-Equal 'OK' ((Invoke-Exe @('--set-enabled', 'false', $fxT)).Out).Trim() "--set-enabled false returns OK"
$afterOff = Get-Content $cfgT -Raw | ConvertFrom-Json
Assert-Equal 'False' "$($afterOff.enabled)"     "mute sets top-level enabled=false"
Assert-Equal 'True'  "$($afterOff.tts.enabled)" "mute leaves nested tts.enabled untouched"

Assert-Equal 'OK' ((Invoke-Exe @('--set-enabled', 'true', $fxT)).Out).Trim() "--set-enabled true returns OK"
$afterOn = Get-Content $cfgT -Raw | ConvertFrom-Json
Assert-Equal 'True' "$($afterOn.enabled)"     "unmute sets top-level enabled=true"
Assert-Equal 'True' "$($afterOn.tts.enabled)" "unmute leaves nested tts.enabled untouched"

# --- Task 8: install/uninstall round-trip (temp dirs, no launch) ---
$instDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ppt_inst_" + [System.Guid]::NewGuid().ToString('N'))
$startDir = Join-Path ([System.IO.Path]::GetTempPath()) ("ppt_start_" + [System.Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $startDir | Out-Null
& (Join-Path $root 'install.ps1') -InstallDir $instDir -StartupDir $startDir -NoLaunch | Out-Null
Assert-True (Test-Path (Join-Path $instDir 'PeonPingTray.exe')) "install copied exe"
Assert-True (Test-Path (Join-Path $instDir 'peonping-groups.conf')) "install copied groups.conf"
Assert-True (Test-Path (Join-Path $startDir 'PeonPingTray.lnk')) "install created startup shortcut"
& (Join-Path $root 'uninstall.ps1') -InstallDir $instDir -StartupDir $startDir | Out-Null
Assert-True (-not (Test-Path $instDir)) "uninstall removed install dir"
Assert-True (-not (Test-Path (Join-Path $startDir 'PeonPingTray.lnk'))) "uninstall removed shortcut"

Write-Host ""
if ($script:fail -gt 0) { Write-Host "$($script:fail) failure(s)." -ForegroundColor Red; exit 1 }
Write-Host "All tests passed." -ForegroundColor Green
