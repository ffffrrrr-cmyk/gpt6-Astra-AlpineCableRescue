$ErrorActionPreference = 'Stop'
$gamePath = Join-Path $PSScriptRoot 'Builds/Windows/FrostlineRescue.exe'
if (-not (Test-Path -LiteralPath $gamePath)) { throw 'Build missing. Run Build.ps1 with an activated Unity 6000.6.7f1 Editor first.' }
Start-Process -FilePath $gamePath -WorkingDirectory (Split-Path -Parent $gamePath)
