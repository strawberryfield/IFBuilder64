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
