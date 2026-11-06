$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$deliveryRoot = Join-Path $projectRoot 'Delivery'
New-Item -ItemType Directory -Force -Path $deliveryRoot | Out-Null
$sourceNames = @('Assets', 'Packages', 'ProjectSettings', 'Documentation', 'ThirdPartyNotices', 'README.md', 'LICENSE.md', 'NOTICE.md', 'Build.ps1', 'Run.ps1', 'Package.ps1', '.gitignore')
$sourcePaths = @($sourceNames | ForEach-Object { Join-Path $projectRoot $_ } | Where-Object { Test-Path -LiteralPath $_ })
$sourceArchive = Join-Path $deliveryRoot 'U1_FrostlineRescue_Source.zip'
Compress-Archive -LiteralPath $sourcePaths -DestinationPath $sourceArchive -CompressionLevel Optimal -Force
$buildRoot = Join-Path $projectRoot 'Builds/Windows'
$buildPaths = @(Get-ChildItem -LiteralPath $buildRoot -Force | Where-Object { $_.Name -notlike '*BackUpThisFolder*' -and $_.Name -ne 'Logs' } | ForEach-Object { $_.FullName })
if (-not (Test-Path -LiteralPath (Join-Path $buildRoot 'FrostlineRescue.exe'))) { throw 'No Windows executable to package.' }
$playerArchive = Join-Path $deliveryRoot 'U1_FrostlineRescue_Windows.zip'
Compress-Archive -LiteralPath $buildPaths -DestinationPath $playerArchive -CompressionLevel Optimal -Force
$scriptFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts') -Filter '*.cs')
$applicationFiles = @($scriptFiles | Where-Object { $_.Name -notin @('RuntimeVerification.cs', 'CaptureVerification.cs') })
$applicationLines = ($applicationFiles | ForEach-Object { (Get-Content -LiteralPath $_.FullName | Where-Object { $_.Trim().Length -gt 0 }).Count } | Measure-Object -Sum).Sum
$record = [ordered]@{
    project_id = 'U1'
    project_id_status = 'temporary_user_authorized_pending_team_registration'
    title = '霜岭索道救援'
    source_type = 'new_production'
    direction = 'Unity/C#'
    project_type = 'complete_single_player_game'
    complexity = 'four_missions_physics_and_procedural_assets'
    engine = 'Unity 6000.6.7f1'
    target = 'Windows x64'
    model = 'gpt-6-astra'
    provider = 'OpenAI / Codex'
    concrete_service_snapshot = 'pending_verification'
    account_plan = 'not_observable_pending_verification'
    procurement_alias_verification = '6astra_mapping_pending_procurement_confirmation'
    generation_date = '2026-11-06 Asia/Shanghai'
    session_id = '01a1af64-934e-7b72-8594-5af7bf39863b'
    rollout_record = 'Documentation/开发与生成记录.md'
    subproject_path = '.'
    source_archive = 'U1_FrostlineRescue_Source.zip'
    source_sha256 = (Get-FileHash -LiteralPath $sourceArchive -Algorithm SHA256).Hash
    player_archive = 'U1_FrostlineRescue_Windows.zip'
    player_sha256 = (Get-FileHash -LiteralPath $playerArchive -Algorithm SHA256).Hash
    human_code_modifications = 'none'
    duplicate_cluster = 'U1_alpine_crane_rescue;cross_batch_pending'
    validation_report = 'Documentation/验证报告.md'
    human_playtest = 'not_tested'
    rights_status = 'pending_commercial_legal_review'
    raw_repo_count = 1
    workspace_independent_project_count = 1
    cross_batch_deduplicated_count = $null
    procurement_accepted_project_count = $null
    application_source_files = $applicationFiles.Count
    application_nonblank_lines = $applicationLines
    excluded_from_application_count = @('verification_harness', 'editor_build_code', 'dependencies', 'engine', 'generated_assets_and_config', 'cache', 'builds', 'binary_resources')
}
($record | ConvertTo-Json -Depth 5 -Compress) | Set-Content -LiteralPath (Join-Path $deliveryRoot 'manifest.jsonl') -Encoding utf8
Get-Item -LiteralPath $sourceArchive,$playerArchive | Select-Object Name,Length
