# IFBuilder64

IFBuilder64 is a tool to build and manage Interactive Fiction (IF) targets and related artifacts for 64-bit platforms. This repository contains the source, tests, and tooling to build, test, and package the project on .NET 10 and Visual Studio.

## Overview
- `IFBuilder64` provides build orchestration, packaging helpers, and utilities for Interactive Fiction projects targeting Commodore 64.
- Designed to integrate with Visual Studio and the `dotnet` CLI for reproducible builds and CI/CD pipelines.

## Key Features
- Cross-platform build using .NET 10 (`dotnet` CLI).
- Visual Studio integration: open solution, build, run, and test with the IDE.
- Clear coding conventions enforced via `.editorconfig`.

## Requirements
- .NET 10 SDK installed (match the project's TargetFramework).
- Visual Studio (recommended) or any editor that supports .NET 10.
- Optional: Git for cloning and contribution workflows.

## Getting Started (quick)
1. Clone the repository:
   - `git clone https://github.com/strawberryfield/IFBuilder64.git`
2. Open the solution:
   - In Visual Studio: File → Open → Project/Solution → select the `.sln`.
   - Or use the CLI: `cd IFBuilder64` then `dotnet restore`.
3. Build:
   - `dotnet build`
4. Run (if the repo contains runnable projects):
   - `dotnet run --project <ProjectPath>` or run via Visual Studio.

## Graphical Editor
The `Editor` project is a cross-platform desktop application (Avalonia) to create and edit games without writing the JSON by hand: map with drag-and-drop rooms and doors, tabs for items, verbs, messages and game data, live validation with one-click fixes, and compilation (F5) through the same builder used by the command line. The interface is available in Italian and English.

- Run from source: `dotnet run --project Editor`
- Publish (framework-dependent, requires the .NET 10 runtime): `dotnet publish Editor -c Release -r <rid> --self-contained false -o artifacts/editor-<rid>` with `<rid>` = `win-x64`, `linux-x64`, `osx-x64` or `osx-arm64`
- User guide: see the "Uso dell'editor grafico" / "Using the graphical editor" sections of the manuals in this repository.
- Third-party licenses: `Editor/THIRD-PARTY-NOTICES.txt` (also shown in Help → About).

### Linux requirements
On Linux the editor uses Avalonia's X11 backend, which loads native system libraries that minimal installations (e.g. a fresh WSL distribution) may not include. A missing library shows up at startup as an error like `libICE.so.6: cannot open shared object file`. On Debian/Ubuntu install them with:

```bash
sudo apt update
sudo apt install -y libice6 libsm6 libx11-6 libx11-xcb1 libxext6 libxrandr2 \
    libxcursor1 libxi6 libxrender1 libfontconfig1 libfreetype6
```

- The .NET 10 runtime is also required (`sudo apt install -y dotnet-runtime-10.0` or see the Microsoft documentation for your distribution).
- Under WSL a graphical server is needed: WSLg (included in Windows 11) provides it; `echo $DISPLAY` should print something like `:0`.
- If another `.so` is reported missing, install the package that provides it (e.g. `libSM.so.6` → `libsm6`).

### macOS notes
> **Untested:** the maintainer has no Mac available, so the following notes are based on how .NET and macOS usually behave and have not been verified. Reports and corrections are welcome.

- **Apple Silicon only.** The release package is `osx-arm64` (M1 and later). On Intel Macs build from source (`dotnet run --project Editor`) or publish with `-r osx-x64`.
- **.NET 10 runtime.** Install the Arm64 .NET 10 runtime from the Microsoft download page (the `.pkg` installer puts it in `/usr/local/share/dotnet`, where the executables look for it). If the runtime is installed elsewhere (e.g. via Homebrew), set `DOTNET_ROOT` to its folder.
- **Gatekeeper.** The executables are not signed with an Apple Developer ID nor notarized, so macOS blocks files downloaded from the internet ("cannot be opened because the developer cannot be verified"). After extracting the zip, remove the quarantine attribute from the whole folder:
  ```bash
  xattr -dr com.apple.quarantine /path/to/IFBuilder64-osx-arm64
  ```
  Alternatively, after the first blocked attempt use System Settings → Privacy & Security → "Open Anyway"; this may have to be repeated for each native library.
- **No `.app` bundle.** The editor is a plain executable: start it from Terminal with `./Casasoft.IFBuilder64.Editor` (double-clicking it in Finder also opens a Terminal window). There is no Dock/Launchpad integration.
- **"Killed: 9" at startup.** Apple Silicon refuses to run unsigned native code. If the executables are terminated immediately, apply an ad-hoc signature:
  ```bash
  codesign --force --sign - Casasoft.IFBuilder64.Editor IFBuilder64
  ```
- **Permissions.** The zip stores the executables with mode `0755`; if they are not executable after extraction, run `chmod +x Casasoft.IFBuilder64.Editor IFBuilder64`.

## Tests
- `dotnet test Tests/Tests.csproj` (xUnit v3, including headless Avalonia UI tests).

## Projects
- `Common`: JSON game model and language templates.
- `Builder`: build engine that turns a game into C64 files (used by the CLI and the editor).
- `IFBuilder64`: command-line tool.
- `SeqConverter`: legacy SEQ-to-JSON converter.
- `Editor`: graphical editor.
- `Tests`: automated tests.

## Contributing
- This project follows the conventions set in `.editorconfig` — ensure your changes comply before submitting a PR.
- Create feature branches from `main` (or the repository's default branch), add tests for new behavior, and open a pull request describing the change.

## License
- This project is distributed under the GNU Affero General Public License v3 or later (AGPL-3.0+). See the [LICENSE](LICENSE.txt) file for full terms.
- Copyright (c) 2026 Casasoft — Roberto Ceccarelli

## Support / Contact
- Maintainer: [Roberto Ceccarelli](https://strawberryfield.altervista.org/robertoceccarelli-fotografia-informatica.php?lang=en) — see repository metadata for contact or open an issue for support.
