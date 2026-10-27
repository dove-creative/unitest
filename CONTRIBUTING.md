# Contributing

Thank you for contributing to UniTest. This document describes the basic standards for proposing changes or opening pull requests.

## Basic Principles

- Keep behavior, tests, samples, and documentation in sync. Public API changes should update usage examples and wiki documentation together.
- Preserve the `netstandard2.1` library and .NET 9 test and sample targets unless a change explicitly requires a compatibility break.
- If a verification step could not be run, mention the reason in the pull request.

## Development Environment

UniTest uses the .NET SDK and a standard repository layout.

- Runtime library: `src/UniTest/UniTest.csproj`
- Unit tests: `tests/UniTest.Test.UnitTest/UniTest.Test.UnitTest.csproj`
- Recursion tests: `tests/UniTest.Test.RecursionTest/UniTest.Test.RecursionTest.csproj`
- Console sample: `samples/UniTest.Samples/UniTest.Samples.csproj`

Develop UniTest in its independent repository, such as `Projects/unitest`. Tests and samples in this repository retain their source `ProjectReference` so changes can be verified before release. Other repositories consume a validated stable `BlackThunder.UniTest` package from their own local `packages/` feed and pin the exact version; they do not build a sibling UniTest checkout.

Restore and build the root solution before submitting a code change.

```powershell
dotnet restore UniTest.sln
dotnet build UniTest.sln --no-restore
```

## Code Style

- C# files and documentation files use LF line endings.
- Code comments are written in English.
- Nullable annotations remain disabled for compatibility with existing consumers.
- Do not add new dependencies unless the change explicitly requires them.
- Do not vendor restored NuGet packages or generated build output.
- Update tests when changing shared behavior such as `Project<TModel>`, `Node<TModel>`, `Lab<TModel>`, `Model`, `TestCase`, JSON reports, or execution replay.

## Documentation Style

English documentation is in `docs/Wiki.en` and `docs/Workflow.en`. Korean documentation is in `docs/Wiki.ko` and `docs/Workflow.ko`. Usage examples are maintained with `samples/UniTest.Samples`.

Keep code identifiers unchanged. Names such as `Project<TModel>`, `CompactLab<TModel>`, `Run(...)`, `RunContinuously(...)`, and `Execute(ids)` should stay as they are.

When changing test authoring or external execution guidance, update the workflow documents together with the README if the contributor-facing flow changes.

## Stable Packages

The library package ID is `BlackThunder.UniTest`, currently version `0.3.0`. The assembly name, namespace, and `netstandard2.1` target remain unchanged. Packages are stored in consumer repositories; publishing to a public NuGet feed is not part of this workflow.

After verification passes, commit the library changes and package that source commit with repository provenance.

```powershell
$sourceCommit = git rev-parse HEAD
dotnet pack src/UniTest/UniTest.csproj --configuration Release -o artifacts/packages -p:RepositoryCommit=$sourceCommit
Get-FileHash artifacts/packages/BlackThunder.UniTest.0.3.0.nupkg -Algorithm SHA256
```

Copy the validated `.nupkg` into the consumer's `packages/` folder, register that local feed in its root `NuGet.config`, and use `Version="[0.3.0]"` in its `PackageReference`. Record the version, source commit, SHA-256, and verification results in the consumer repository. Only validated first-party release packages are tracked; restored packages and generated build output remain untracked. Do not overwrite an existing package at the same version: increment the version for any replacement.

## Tests

Run the applicable verification for the changed area.

- Documentation-only changes: check links, terminology, line endings, and trailing whitespace.
- Runtime changes: build the root solution and run both NUnit projects.
- Unit test changes: run `tests/UniTest.Test.UnitTest/UniTest.Test.UnitTest.csproj`.
- Recursion test changes: run `tests/UniTest.Test.RecursionTest/UniTest.Test.RecursionTest.csproj`.
- Sample changes: build or run `samples/UniTest.Samples/UniTest.Samples.csproj`.

Briefly include verification results in the pull request.

## Branch Naming

Create a short-lived branch from the latest `main` using `<username>/<topic>`. Use lowercase kebab-case for the topic when possible.

Examples:

- `dove/wiki-locale`
- `dove/test-runner-fix`
- `dove/readme-install`

## Pull Request

A pull request should include why the change was made, the main changes, verification results, and whether documentation was updated.

## License

Contributed code is distributed under this repository's MIT license. When bringing in external code or materials, check the original license and notice requirements and add a separate notice file if needed.
