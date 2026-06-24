# Contributing

Thank you for contributing to UniTest. This document describes the basic standards for proposing changes or opening pull requests.

## Basic Principles

- Keep behavior, tests, samples, and documentation in sync. Public API changes should update usage examples and wiki documentation together.
- Preserve the Unity and Native C# usage story when changing runtime APIs or samples.
- If a test or verification step could not be run, mention the reason in the pull request.

## Development Environment

UniTest assumes a folder-based package structure inside a Unity project.

Basic checklist:

- The `Packages/com.blackthunder.unitest` folder exists inside a Unity project.
- The Unity project defines `UNITEST` so the `UniTest` runtime asmdef is compiled.
- Package test assemblies use `UNITY_INCLUDE_TESTS` with the target test symbol, either `UNITEST_TEST_UT` or `UNITEST_TEST_RT`.
- Tests run on Unity Test Framework and NUnit.
- Native C# usage includes the `Runtime` source directly. There is no separate NuGet package yet.

## Code Style

- C# files and documentation files use LF line endings.
- Code comments are written in English.
- Do not use nullable syntax for Unity compatibility.
- Do not add new dependencies unless the change explicitly requires them.
- Do not vendor restored NuGet packages or generated build output.
- Update tests when changing shared behavior such as `Project<TModel>`, `Node<TModel>`, `Lab<TModel>`, `Model`, `TestCase`, XML reports, or execution replay.

## Documentation Style

English documentation is in `Documentation~/Wiki.en` and `Documentation~/Workflow.en`. Korean documentation is in `Documentation~/Wiki.ko` and `Documentation~/Workflow.ko`. Usage examples are maintained together with the `Samples~/Unity` and `Samples~/NativeCSharp` samples.

Keep code identifiers unchanged. For example, names such as `Project<TModel>`, `CompactLab<TModel>`, `Run(...)`, `RunContinuously(...)`, and `Execute(ids)` should stay as they are.

When changing test authoring or external execution guidance, update the workflow documents together with the README if the contributor-facing flow changes.

## Tests

Run the applicable verification for the changed area.

- Documentation-only changes: check links, terminology, line endings, and trailing whitespace.
- Runtime code changes: run the package tests in Unity Test Framework with `UNITEST`, `UNITY_INCLUDE_TESTS`, and the matching test asmdef symbol enabled.
- Unit test assembly changes: verify the `UNITEST_TEST_UT` path.
- Recursion test assembly changes: verify the `UNITEST_TEST_RT` path.
- Native C# sample changes: build or run the relevant project under `Samples~/NativeCSharp`.
- Workflow or external executor guidance changes: verify the documented paths and keep the External NUnit Executor owned by the target domain or framework.

Briefly include verification results in the pull request.

## Branch Naming

When submitting changes through a pull request, create a short-lived branch from the latest `main`.

Use this format:

- `<username>/<topic>`

Use lowercase kebab-case for `<topic>` when possible.

Examples:

- `dove/wiki-locale`
- `dove/test-runner-fix`
- `dove/readme-install`
- `dove/native-sample`

Avoid vague long-lived branch names such as `<username>/work`, `<username>/update`, or `<username>/main`.

## Pull Request

A pull request should include:

- Why the change was made
- Main changes
- Verification that was run, or verification that could not be run and why
- Whether documentation was updated

## License

Contributed code is considered distributed under this repository's MIT license. When bringing in external code or materials, check the original license and notice requirements, and add a separate notice file if needed.
