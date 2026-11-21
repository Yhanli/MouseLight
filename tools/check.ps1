<#
.SYNOPSIS
    Repository checks that run without the game: used by GitHub Actions on every push and pull request,
    and runnable locally before opening a PR.
.DESCRIPTION
    - PublishConfiguration.xml is valid, has the required fields and a x.y.z ModVersion.
    - Every listed thumbnail/screenshot exists and is under Paradox Mods' 2.1 MB image limit.
    - Every option in CursorLightSettings.cs has a label and description in CursorLightLocale.cs.
    - The credit to the original Mouse Light author is still in the README and the mod description.
    The mod itself can't be compiled in CI: building needs the game's own DLLs and its Unity post-processor.
.EXAMPLE
    .\tools\check.ps1
#>
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repo 'BetterCursorLight'
$script:errors = 0
$inCi = [bool]$env:GITHUB_ACTIONS

function Fail([string]$file, [string]$message) {
    $script:errors++
    if ($inCi) { Write-Host "::error file=$file::$message" } else { Write-Host "FAIL  $file`: $message" -ForegroundColor Red }
}
function Pass([string]$message) { Write-Host "ok    $message" }

# --- Publish configuration -------------------------------------------------------------------------
$configRel = 'BetterCursorLight/Properties/PublishConfiguration.xml'
$configPath = Join-Path $repo $configRel
try {
    [xml]$config = Get-Content $configPath -Raw -Encoding UTF8
    Pass 'PublishConfiguration.xml is valid XML'
} catch {
    Fail $configRel "Not valid XML: $($_.Exception.Message)"
    $config = $null
}

if ($config) {
    $p = $config.Publish
    foreach ($field in 'DisplayName', 'ShortDescription', 'ModVersion', 'GameVersion', 'Thumbnail') {
        $value = $p.$field.Value
        if ([string]::IsNullOrWhiteSpace($value)) { Fail $configRel "<$field Value=...> is empty" }
    }
    if ($p.ModVersion.Value -notmatch '^\d+\.\d+\.\d+$') {
        Fail $configRel "ModVersion '$($p.ModVersion.Value)' must look like 1.2.3"
    } else {
        Pass "ModVersion $($p.ModVersion.Value)"
    }

    $images = @($p.Thumbnail.Value) + @($p.Screenshot | ForEach-Object { $_.Value }) | Where-Object { $_ }
    foreach ($image in $images) {
        $file = Join-Path $projectDir $image
        if (-not (Test-Path $file)) { Fail $configRel "Image not found: $image"; continue }
        $mb = (Get-Item $file).Length / 1MB
        if ($mb -gt 2.1) { Fail "BetterCursorLight/$image" ('{0:N2} MB is over the 2.1 MB Paradox Mods limit; save it as JPG' -f $mb) }
        else { Pass ('{0} ({1:N2} MB)' -f $image, $mb) }
    }

    # A text-only element comes back as a plain string; one with attributes or children as an XmlElement.
    $longDescription = if ($p.LongDescription -is [string]) { $p.LongDescription } else { $p.LongDescription.InnerText }
    if ($longDescription -notmatch 'Dnikolas991') {
        Fail $configRel 'LongDescription must keep the credit to Dnikolas991 (original Mouse Light author)'
    }
}

# --- Settings have texts ---------------------------------------------------------------------------
$settingsRel = 'BetterCursorLight/CursorLightSettings.cs'
$localeRel = 'BetterCursorLight/CursorLightLocale.cs'
$settings = Get-Content (Join-Path $repo $settingsRel) -Raw -Encoding UTF8
$locale = Get-Content (Join-Path $repo $localeRel) -Raw -Encoding UTF8

# Options are the properties marked [SettingsUISection(...)]; other attributes may sit in between.
$options = [regex]::Matches($settings, '\[SettingsUISection[^\]]*\](?:\s*\[[^\]]*\])*\s*public\s+[\w<>]+\s+(\w+)\s*\r?\n') |
    ForEach-Object { $_.Groups[1].Value }
if (-not $options) { Fail $settingsRel 'No [SettingsUISection] options found (did the file layout change?)' }
foreach ($option in $options) {
    foreach ($kind in 'Label', 'Desc') {
        if ($locale -notmatch "GetOption${kind}LocaleID\(nameof\(CursorLightSettings\.$option\)\)") {
            Fail $localeRel "Option '$option' has no $kind text. Add it for English, Chinese and German."
        }
    }
}
Pass "$(@($options).Count) options checked for label and description texts"

# --- Credit ----------------------------------------------------------------------------------------
$readme = Get-Content (Join-Path $repo 'README.md') -Raw -Encoding UTF8
if ($readme -notmatch 'github\.com/Dnikolas991/MouseLight') {
    Fail 'README.md' 'README must keep the credit link to https://github.com/Dnikolas991/MouseLight'
} else {
    Pass 'Credit to the original author present'
}

if ($script:errors -gt 0) {
    Write-Host "$($script:errors) check(s) failed."
    exit 1
}
Write-Host 'All checks passed.'
exit 0
