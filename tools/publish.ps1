<#
.SYNOPSIS
    Publishes Better Cursor Light to Paradox Mods, after checking the things that make publishing fail.
.DESCRIPTION
    Checks that the game is closed and that the thumbnail and every screenshot are under Paradox Mods'
    2.1 MB image limit, shows the version being published, then runs the publish profile.
    Publishing uploads under the Paradox account signed in to the game.
.PARAMETER PublishProfile
    PublishNewVersion (default): upload a new version of the existing mod (ModId 168458).
    UpdatePublishedConfiguration: update only the description, images, tags and links.
    PublishNewMod: create a brand-new mod. Only for a first release; the mod already exists.
.EXAMPLE
    .\tools\publish.ps1
.EXAMPLE
    .\tools\publish.ps1 -PublishProfile UpdatePublishedConfiguration
#>
param(
    [ValidateSet('PublishNewVersion', 'UpdatePublishedConfiguration', 'PublishNewMod')]
    [string]$PublishProfile = 'PublishNewVersion'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repo 'BetterCursorLight'
$project = Join-Path $projectDir 'BetterCursorLight.csproj'
$configPath = Join-Path $projectDir 'Properties\PublishConfiguration.xml'
$maxImageBytes = 2.1MB

$dotnet = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

if (Get-Process -Name 'Cities2' -ErrorAction SilentlyContinue) {
    Write-Error 'Cities: Skylines II is running and locks the mod files. Close the game, then publish again.'
    exit 1
}

[xml]$config = Get-Content $configPath -Raw
$images = @($config.Publish.Thumbnail.Value) + @($config.Publish.Screenshot | ForEach-Object { $_.Value }) |
    Where-Object { $_ }
$tooBig = foreach ($image in $images) {
    $file = Join-Path $projectDir $image
    if (-not (Test-Path $file)) { Write-Error "Image listed in PublishConfiguration.xml not found: $image"; exit 1 }
    $size = (Get-Item $file).Length
    if ($size -gt $maxImageBytes) { '{0} ({1:N2} MB)' -f $image, ($size / 1MB) }
}
if ($tooBig) {
    Write-Error ("Paradox Mods rejects images over 2.1 MB. Re-save these as JPG:`n  " + ($tooBig -join "`n  "))
    exit 1
}

$modId = $config.Publish.ModId.Value
if ($PublishProfile -eq 'PublishNewMod' -and $modId) {
    Write-Error "ModId is already set ($modId). Use PublishNewVersion to update the existing mod."
    exit 1
}
if ($PublishProfile -ne 'PublishNewMod' -and -not $modId) {
    Write-Error 'ModId is empty in PublishConfiguration.xml. Set it before updating an existing mod.'
    exit 1
}

$version = $config.Publish.ModVersion.Value
$tag = "v$version"
$uploadsCode = $PublishProfile -ne 'UpdatePublishedConfiguration'

if ($uploadsCode) {
    # What gets published must match a commit, so the release tag points at the exact code players get.
    if (git -C $repo status --porcelain) {
        Write-Error 'There are uncommitted changes. Commit them first so the published version matches a commit.'
        exit 1
    }
    git -C $repo fetch --quiet --tags origin 2>$null
    if (git -C $repo tag --list $tag) {
        Write-Error "Version $version is already released (tag $tag exists). Bump <ModVersion> and write a <ChangeLog> first."
        exit 1
    }
}

Write-Host "Publishing '$($config.Publish.DisplayName.Value)' version $version with $PublishProfile (ModId: $modId)"
& $dotnet publish $project "/p:PublishProfile=$PublishProfile" 2>&1 |
    Select-String -Pattern 'error|Mod published|publishing process|Could not publish|Auto logged in|Build FAILED' |
    ForEach-Object { $_.Line }
$code = $LASTEXITCODE
if ($code -ne 0) { exit $code }

if ($uploadsCode) {
    # Tag the published commit; pushing the tag makes GitHub Actions create the GitHub Release.
    git -C $repo tag -a $tag -m "Better Cursor Light $version (published to Paradox Mods)"
    git -C $repo push origin $tag
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Published, but pushing tag $tag failed. Run: git push origin $tag"
    } else {
        Write-Host "Tagged $tag and pushed it; GitHub Actions will create the release."
    }
}
exit 0
