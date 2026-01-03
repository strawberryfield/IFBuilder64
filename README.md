# IFBuilder64

IFBuilder64 is a tool to build and manage Interactive Fiction (IF) targets and related artifacts for 64-bit platforms. This repository contains the source, tests, and tooling to build, test, and package the project on .NET 10 and Visual Studio.

## Overview
- `IFBuilder64` provides build orchestration, packaging helpers, and utilities for Interactive Fiction projects targeting Commodore 64.
- Designed to integrate with Visual Studio and the `dotnet` CLI for reproducible builds and CI/CD pipelines.

## Key Features
- Cross-platform build using .NET 10 (`dotnet` CLI).
- Visual Studio integration: open solution, build, run, and test with the IDE.
- Automated test project(s) compatible with `dotnet test`.
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

## Running Tests
- Run with CLI:
  - `dotnet test`
- In Visual Studio:
  - Use Test Explorer to discover and run tests.

## Contributing
- Please read `CONTRIBUTING.md` for coding standards, branch naming, commit message format, and PR process.
- This project follows the conventions set in `.editorconfig` — ensure your changes comply before submitting a PR.
- Create feature branches from `main` (or the repository's default branch), add tests for new behavior, and open a pull request describing the change.

## License
- This project is distributed under the GNU Affero General Public License v3 or later (AGPL-3.0+). See the LICENSE file for full terms.
- Copyright (c) 2026 Casasoft — see project headers for author information.

## Support / Contact
- Maintainer: Roberto Ceccarelli — see repository metadata for contact or open an issue for support.

## Notes
- For CI, packaging, and release automation, refer to the repository's workflow and build scripts (if present).
- If `CONTRIBUTING.md` or other docs are missing, please open an issue to request them or submit a PR with additions.
