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

function Dump($hookDir, $groupsConf) {
  $a = @('--dump', $hookDir); if ($groupsConf) { $a += $groupsConf }
  return (& $exe @a | ConvertFrom-Json)
}

function New-Fixture { param([bool]$Enabled = $true, [string]$DefaultPack = 'peon')
  $dir = Join-Path ([System.IO.Path]::GetTempPath()) ("ppt_" + [System.Guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  ($([pscustomobject]@{ enabled = $Enabled; default_pack = $DefaultPack; volume = 0.5 }) | ConvertTo-Json) |
    Set-Content -Path (Join-Path $dir 'config.json') -Encoding UTF8
  return $dir
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

Write-Host ""
if ($script:fail -gt 0) { Write-Host "$($script:fail) failure(s)." -ForegroundColor Red; exit 1 }
Write-Host "All tests passed." -ForegroundColor Green
