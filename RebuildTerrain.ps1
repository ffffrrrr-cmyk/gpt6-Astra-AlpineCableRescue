param([switch]$Capture, [string]$ShotFolder = 'rebuild')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$editorPath = 'E:/Program Files/6000.6.7f1/Editor/Unity.exe'
$buildLog = Join-Path $projectRoot 'Logs/terrain-rebuild.log'
$buildArguments = '-batchmode -nographics -quit -projectPath "{0}" -executeMethod TerrainBuild.Build -logFile "{1}"' -f $projectRoot, $buildLog
$editorProcess = Start-Process -FilePath $editorPath -ArgumentList $buildArguments -WindowStyle Hidden -PassThru -Wait
if ($editorProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath $buildLog -Pattern 'TERRAIN_BUILD_PASS' -Quiet)) { throw "Build failed: $buildLog" }
$executable = Join-Path $projectRoot 'Builds/TerrainRevision/FrostlineRescue.exe'
Write-Output $executable
if ($Capture) {
    if ($ShotFolder -notmatch '^[a-zA-Z0-9_-]+$') { throw 'ShotFolder must be a simple folder name.' }
    $output = Join-Path $projectRoot "shots/new/$ShotFolder"
    $captureLog = Join-Path $projectRoot "Logs/terrain-capture-$ShotFolder.log"
    $captureArguments = '--capture --capture-output "{0}" -screen-fullscreen 0 -logFile "{1}"' -f $output, $captureLog
    $gameProcess = Start-Process -FilePath $executable -ArgumentList $captureArguments -WindowStyle Normal -PassThru -Wait
    if ($gameProcess.ExitCode -ne 0) { throw "Capture failed: $captureLog" }
    Write-Output $output
}
