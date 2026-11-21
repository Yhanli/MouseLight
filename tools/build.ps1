<#
.SYNOPSIS
    Builds Better Cursor Light and deploys it to the game's local Mods folder.
.DESCRIPTION
    The build copies the mod into %USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\BetterCursorLight.
    That copy fails while the game is running (it locks the mod files), so this script checks first.
.PARAMETER Configuration
    Release (default) or Debug.
.EXAMPLE
    .\tools\build.ps1
#>
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'BetterCursorLight\BetterCursorLight.csproj'

$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

if (Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue) {
    Write-Error 'Cities: Skylines II is running and locks the mod files. Close the game, then build again.'
    exit 1
}

$output = & $dotnet build $project -c $Configuration 2>&1
$code = $LASTEXITCODE

# The post-processor prints hundreds of lines; show only what matters.
$output | Select-String -Pattern 'error|warning CS|Build succeeded|Build FAILED|Warning\(s\)|Error\(s\)' |
    ForEach-Object { $_.Line } | Select-Object -Unique

if ($code -eq 0) {
    $deployed = Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\BetterCursorLight'
    Write-Host "Deployed to $deployed. Start the game to load it."
}
exit $code
