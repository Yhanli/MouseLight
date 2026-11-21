# Contributing to Better Cursor Light

Thanks for helping. Fixes, features and translations are all welcome.

## Before you start

- **Bugs:** open an issue with your game version, mod version, steps to reproduce, and `BetterCursorLight.log` if there is an error (see below).
- **Bigger features:** open an issue first to talk it over, so your work fits the mod.
- **Small fixes and translations:** just open a pull request.

## Setting up

You need Cities: Skylines II on Windows.

1. In the game, open **Options → Modding** and install the **Mod Development Toolchain**. It installs the .NET SDK, Unity and the mod templates, and sets the `CSII_*` environment variables the project uses.
   - If it gets stuck at **"Activating license"**, install **Unity Hub**, sign in, add a free **Personal** license (Preferences → Licenses), then run the toolchain install again.
2. Install the **.NET 6 runtime** (`winget install Microsoft.DotNet.Runtime.6`). The toolchain's post-processor needs it.
3. If `dotnet restore` can't find packages, add the NuGet source: `dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org`.
4. Fork this repo and clone your fork.

## Building and testing

```powershell
.\tools\build.ps1
```

- **Close the game first.** The game locks the mod files, and the build copies the mod into `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\BetterCursorLight`.
- Start the game; local mods load automatically. **Disable the Paradox Mods copy** of Better Cursor Light in your playset, or both copies will run.
- Test day, dusk and night, and in the shadows of tall buildings.
- Logs are in `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\` (`BetterCursorLight.log`, `Modding.log`).

Before opening a PR, run the checks that CI runs:

```powershell
.\tools\check.ps1
```

## Code guidelines

- **Adding an option** touches four places:
  1. a property in `CursorLightSettings.cs` with `[SettingsUISection(GeneralSection, GeneralGroup)]`,
  2. its default in `SetDefaults()`,
  3. a label and description in **English, Chinese and German** in `CursorLightLocale.cs` (machine translation is fine for a first pass; say so in the PR),
  4. the settings table in `README.md`.

  CI fails if an option has no texts.
- **Don't rename existing option properties** unless the setting's meaning changes. Renaming resets every player's saved value.
- Keep the light cheap: it runs every frame. Don't allocate per frame, and don't log every frame.
- Write new comments in English. Keep the style of the surrounding code (see `.editorconfig`).
- Don't change `ModVersion`, `ChangeLog` or `ModId`. The maintainer sets those when releasing.

## Pull requests

- One change per PR, with a clear title.
- For visual changes, include **before/after screenshots** (day and night if relevant).
- Say what you tested in game, and on which game version.
- CI ("Checks") must pass. It can't compile the mod (that needs the game), so the maintainer builds and tests it in game before merging.

## Releases (maintainer)

1. Bump `<ModVersion>` and write `<ChangeLog>` in `BetterCursorLight/Properties/PublishConfiguration.xml`, then commit.
2. Close the game and run `.\tools\publish.ps1`. It uploads to Paradox Mods, tags `vX.Y.Z` and pushes the tag. GitHub Actions then creates the GitHub Release.

## License of your contribution

By opening a pull request, you agree that your contribution is licensed under the [MIT License](LICENSE), the same as the rest of Better Cursor Light's own work (see [NOTICE.md](NOTICE.md)). Only submit work you wrote yourself or are allowed to share under MIT.

## Credit

Better Cursor Light is based on [Mouse Light](https://github.com/Dnikolas991/MouseLight) by Dnikolas991. Keep that credit in the README and the mod description; CI checks for it.
