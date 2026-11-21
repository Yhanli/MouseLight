# Better Cursor Light: notes for Claude

Cities: Skylines II **code mod** (C#, Unity ECS + HDRP). A light follows the mouse cursor, day and night.
Published on Paradox Mods as **ModId 168458**. Fork of [Dnikolas991/MouseLight](https://github.com/Dnikolas991/MouseLight);
always keep the credit to Dnikolas991 in the README and the mod description.

## Layout

- `BetterCursorLight/` is the mod project (`BetterCursorLight.csproj`, net48, imports the toolchain's `Mod.props`/`Mod.targets` via `%CSII_TOOLPATH%`).
  - `Mod.cs` registers settings, locales and `CursorGlowSystem` (runs in `SystemUpdatePhase.PreTool`).
  - `CursorGlowSystem.cs` handles the mouse raycast (terrain, static objects, nets) and the brightness factor.
  - `CursorLightController.cs` owns the single HDRP spotlight (position, range, lumens, color, specular).
  - `ExposureProbe.cs` reads HDRP's live exposure multiplier via reflection + `AsyncGPUReadback`.
  - `CursorLightSettings.cs` and `CursorLightLocale.cs` hold the options UI and its texts.
  - `Properties/PublishConfiguration.xml` holds the Paradox Mods page (name, descriptions, images, version, changelog).
- `tools/` contains the build, publish and thumbnail scripts.

## Build and test

- Build and deploy: `powershell -File tools\build.ps1`. It refuses to run while the game (`Cities2`) is running, because the game locks the deployed DLLs. If it does, ask the user to close the game.
- The build deploys to `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\BetterCursorLight`. The game loads local mods on its next start.
- `dotnet` is **not on PATH**; use `C:\Program Files\dotnet\dotnet.exe` (the scripts already do).
- There are no automated tests. Behaviour is verified in game by the user: ask for screenshots or read the logs.
  - Mod log: `...\Cities Skylines II\Logs\BetterCursorLight.log`
  - Mod loading: `Logs\Modding.log`; game version: `Player.log` ("Game version:").
- Toolchain requirements (already installed here): the in-game Mod Development Toolchain, Unity 2022.3.62f2 with a Personal license, .NET 8 SDK, and the **.NET 6 runtime** (the ModPostProcessor needs it).

## Conventions

- **Adding a setting** touches four places:
  1. a field and property in `CursorLightSettings.cs` with `[SettingsUISection(GeneralSection, GeneralGroup)]` (plus a slider attribute if numeric),
  2. its default in `SetDefaults()`,
  3. a label and description in **all three languages** (English, Chinese, German) in `CursorLightLocale.cs`, as extra `CreateEntries` parameters and dictionary entries,
  4. the settings table in `README.md`.

  Property order in the class is the order in the options UI.
- **Renaming a setting property resets users' saved value.** Use this deliberately when a value's meaning changes. That's why the colors are `ColorRed`/`ColorGreen`/`ColorBlue` (0–255) instead of the old `Red`/`Green`/`Blue` (0–1).
- Changing a default only affects new installs and "Reset settings"; mention that to the user.
- Comments in new code are in English. The original author's Chinese comments are kept.
- Never commit `bin/`, `obj/` or `Library/` (already in `.gitignore`).

## Brightness model (fragile, tuned in game)

- On-screen brightness = lumens × the camera's exposure multiplier. `GetBrightnessFactor` uses
  `factor = 0.1476 × exposure^-0.933`, fitted to in-game measurements on game 1.6.4:
  night (EV100 0) → 0.175, noon (EV100 14) → about 1500. The user judged both by eye.
- If the exposure can't be read, it falls back to a time-of-day estimate (`DayScale` 2000, `NightScale` 0.175, log blend over `dayLightBrightness`).
- To recalibrate: temporarily log `exposure`, EV100 (`log2(1/(1.2·exposure))`), `dayLightBrightness` and the factor every ~10 s, have the user play through noon, dusk and night and say what looks too bright or too dim, then refit K and P. Remove the periodic log afterwards.
- HDRP internals used via reflection, which a game update can break: `HDRenderPipeline.GetExposureTexture(HDCamera)` (non-public). Public HDRP API used: `HDAdditionalLightData.affectSpecular`.

## Publishing

- Only publish when the user asks; it uploads under their Paradox account. The user usually runs it themselves.
- Update: bump `<ModVersion>`, write `<ChangeLog>`, then `powershell -File tools\publish.ps1` (defaults to `PublishNewVersion`). Description/images only: `-PublishProfile UpdatePublishedConfiguration`.
- Paradox Mods **rejects any image over 2.1 MB**. `publish.ps1` checks this; save large screenshots as JPG.
- `GameVersion` in the publish config should match the current game (`1.6.*` now).
- Thumbnail: `tools/make-thumbnail.ps1` (needs `powershell.exe -STA`, uses WPF).

## Git

- `origin` = the user's fork `Yhanli/MouseLight` (push here); `upstream` = `Dnikolas991/MouseLight`.
- This repo's `user.email` is the GitHub noreply address. GitHub rejects pushes that expose the private email, so don't change it.
- `git push` from the agent shell can't prompt for credentials; run pushes in a terminal tab where Git Credential Manager is signed in.
- Work goes on `main`; the user asks before each commit or push.
