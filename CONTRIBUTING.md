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
- Update tests when changing shared behavior such as `Project<TModel>`, `Node<TModel>`, `Lab<TModel>`, `Model`, `TestCase`, XML reports, or execution replay.

## Documentation Style

English documentation is in `docs/Wiki.en` and `docs/Workflow.en`. Korean documentation is in `docs/Wiki.ko` and `docs/Workflow.ko`. Usage examples are maintained with `samples/UniTest.Samples`.

Keep code identifiers unchanged. Names such as `Project<TModel>`, `CompactLab<TModel>`, `Run(...)`, `RunContinuously(...)`, and `Execute(ids)` should stay as they are.

When changing test authoring or external execution guidance, update the workflow documents together with the README if the contributor-facing flow changes.

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
