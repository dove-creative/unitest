# Contributing

UniTest contributions keep runtime behavior, tests, samples, and documentation aligned.

## Shared Conventions

The root [AGENTS.md](AGENTS.md) follows the shared [AGENTS template](blackthunder-conventions/instructions/AGENTS%20템플릿.md). Common guidelines are pinned through the root `blackthunder-conventions` submodule. Initialize it after cloning:

```powershell
git submodule update --init --recursive
```

Use the [C# convention](blackthunder-conventions/conventions/02.%20dove%20CSharp%20convention.md), [C# platform guidance](blackthunder-conventions/instructions/플랫폼%20기준%20-%20CSharp.md), and the instructions relevant to the task. Documentation follows the [documentation convention](blackthunder-conventions/conventions/01.%20dove%20documentation%20convention.md) and [Markdown format](blackthunder-conventions/instructions/문서%20양식.md). Review shared changes before advancing the submodule commit; keep UniTest-specific material in this repository.

## Development Environment

Use the .NET 10 SDK. `Directory.Build.props` sets C# 9, disables nullable annotations, and disables implicit usings for all projects. The runtime targets `netstandard2.1`; tests and samples target `net10.0`.

- Runtime: `src/BlackThunder.UniTest/BlackThunder.UniTest.csproj`
- Unit tests: `tests/BlackThunder.UniTest.Tests.Unit/BlackThunder.UniTest.Tests.Unit.csproj`
- Recursion tests: `tests/BlackThunder.UniTest.Tests.Recursion/BlackThunder.UniTest.Tests.Recursion.csproj`
- Sample: `samples/BlackThunder.UniTest.Samples.csproj`

Tests and samples use source `ProjectReference` entries within this repository. Other repositories consume an exact stable `BlackThunder.UniTest` package from their own local feed.

```powershell
dotnet restore BlackThunder.UniTest.sln
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
```

## Code and Documentation

- Follow `.editorconfig` and `.gitattributes`; text files use LF line endings.
- Write code comments and Git commit titles in English.
- Use explicit private accessibility, underscore-prefixed private fields, PascalCase constants, and Allman blocks.
- Preserve execution order, cancellation/shutdown contracts, Lab ID values, and JSON fields when applying style changes.
- Update affected tests for runtime changes, and update examples and migration guidance for public API changes.
- Do not add runtime dependencies or vendor restored packages and generated output.
- Maintain Korean documentation under `docs/Wiki.ko` and `docs/Workflow.ko`, with matching English documents under `docs/Wiki.en` and `docs/Workflow.en`.
- Keep README examples small; put component responsibilities in implementation documents and tutorials in usage documents. Follow the shared translation guidance when synchronizing languages.

## Verification

For documentation-only changes, inspect links, headings, code fences, blank lines, LF endings, and trailing whitespace. For code changes, build the solution and run the affected NUnit projects; shared runtime changes require both projects.

Agents follow the explicit-test-request rule in [AGENTS.md](AGENTS.md). A user request to run the relevant verification authorizes those tests for the task. CI continues to build and test on its configured triggers.

```powershell
dotnet test BlackThunder.UniTest.sln --configuration Release --no-build --no-restore -m:1
dotnet format whitespace BlackThunder.UniTest.sln --no-restore --verify-no-changes
pwsh -File .github/scripts/verify-samples.ps1
```

The sample checks cover one-shot termination, all scenarios, exit codes, output paths, blank input, help, unknown commands, and recovery from an I/O exception. Record verification results and explain anything not run.

## Stable Packages

The package ID and assembly name are `BlackThunder.UniTest`; the next package version is `0.4.0`. Consumers upgrading from `0.3.0` follow the [migration guide](docs/Wiki.en/05-Migration.md).

After verification, commit the source before preparing a stable distribution package with provenance:

```powershell
$sourceCommit = git rev-parse HEAD
dotnet pack src/BlackThunder.UniTest/BlackThunder.UniTest.csproj --configuration Release -o artifacts/packages -p:RepositoryCommit=$sourceCommit
Get-FileHash artifacts/packages/BlackThunder.UniTest.0.4.0.nupkg -Algorithm SHA256
```

An uncommitted development package is a verification artifact, not a stable release. Keep validated release packages in the consuming repository's `packages` directory, register its local feed, and pin `Version="[0.4.0]"`. Record the version, source commit, SHA-256, and verification results. Never overwrite an existing release at the same version. Public NuGet publishing is not part of this workflow.

## Changes and Pull Requests

Use a short-lived `<username>/<topic>` branch with a lowercase kebab-case topic. Describe the problem, resulting behavior, compatibility implications, documentation changes, and verification results. Keep unrelated worktree changes intact.

## License

Contributions use the repository's MIT license. When adding external material, retain required licenses and notices.
