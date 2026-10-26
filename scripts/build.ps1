param([switch]$Publish)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sdkCandidates = @(
    (Join-Path $projectRoot '.tools\dotnet\dotnet.exe'),
    (Join-Path (Join-Path (Split-Path -Parent $projectRoot) 'kzn_notch') '.tools\dotnet\dotnet.exe'),
    (Join-Path (Split-Path -Parent (Split-Path -Parent $projectRoot)) '.tools\dotnet\dotnet.exe')
)
$dotnet = ($sdkCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)
if (-not $dotnet) { $dotnet = 'dotnet' }
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.tools\cli'
$packageCaches = @(
    (Join-Path $projectRoot '.tools\nuget'),
    (Join-Path (Join-Path $projectRoot '..\..') '.tools\nuget'),
    (Join-Path (Join-Path (Split-Path -Parent $projectRoot) 'kzn_notch') '.tools\nuget')
)
$env:NUGET_PACKAGES = ($packageCaches | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1)
if (-not $env:NUGET_PACKAGES) { $env:NUGET_PACKAGES = Join-Path $projectRoot '.tools\nuget' }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
& $dotnet build (Join-Path $projectRoot 'KznStore.sln') -c Release --nologo -m:1 -nr:false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Kzn Store build failed.' }
if ($Publish) {
    $output = Join-Path $projectRoot 'artifacts\KznStore-win-x64'
    $project = [xml](Get-Content -LiteralPath (Join-Path $projectRoot 'KznStore.csproj'))
    $version = $project.Project.PropertyGroup.Version | Select-Object -First 1
    & $dotnet publish (Join-Path $projectRoot 'KznStore.csproj') -c Release -r win-x64 --self-contained true -o $output --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Kzn Store publish failed.' }
    Compress-Archive -Path (Join-Path $output '*') -DestinationPath (Join-Path $projectRoot "artifacts\KznStore-$version-win-x64.zip") -Force
}
