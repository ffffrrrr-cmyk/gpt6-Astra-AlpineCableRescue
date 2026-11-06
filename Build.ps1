param(
    [string]$UnityEditor = 'E:\Program Files\6000.6.7f1\Editor\Unity.exe',
    [switch]$VerifyOnly
)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw "Unity editor not found: $UnityEditor" }
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot 'Logs') | Out-Null
$method = if ($VerifyOnly) { 'ProjectBuilder.Verify' } else { 'ProjectBuilder.Build' }
$logPath = Join-Path $projectRoot 'Logs/build.log'
$editorArguments = @('-batchmode', '-nographics', '-quit', '-projectPath', "`"$projectRoot`"", '-executeMethod', $method, '-logFile', "`"$logPath`"")
$buildProcess = Start-Process -FilePath $UnityEditor -ArgumentList $editorArguments -WindowStyle Hidden -PassThru -Wait
if ($buildProcess.ExitCode -ne 0) { throw "Unity exited with $($buildProcess.ExitCode). See $logPath" }
Write-Output "Completed $method. Log: $logPath"
