[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "artifacts/publish"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$app = Join-Path $root "src/TitanOptimizer.App/TitanOptimizer.App.csproj"
$publishPath = Join-Path $root $Output

if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
}

New-Item -ItemType Directory -Path $publishPath -Force | Out-Null

dotnet publish $app `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishPath `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

$archive = Join-Path $root "artifacts/TitanOptimizer-$Runtime.zip"
if (Test-Path $archive) {
    Remove-Item $archive -Force
}
Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $archive
Write-Host "Created $archive"
