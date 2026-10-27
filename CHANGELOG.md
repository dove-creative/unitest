# Changelog

This file records public changes to UniTest.

The format follows Keep a Changelog, and version numbers follow Semantic Versioning.

## [Unreleased]

## [0.3.0] - 2026-10-27

### Changed

- **Behavior change:** `ToUncontinuable` now takes effect after the current Lab's assertions finish. Lab, CompactLab, and CompositeLab run their current assertions, including after an expected exception, and stop only subsequent execution. Existing tests that relied on skipped assertions must review those assertions; public signatures are unchanged.
- **Breaking:** replace XML reports with independent `TestReport` snapshots containing a flat `nodes` array linked by `nodeId` and `parentId`. Replace `Node.Report` with `GetReport()`, `ExportXml` with `ExportJson`, and `GetFailedReports().OuterXml` with `GetFailedReports().ToJson()`. Reports are UTF-8 `.json` files; no XML compatibility API is retained.
- Store model descriptions, execution histories, and exception details as diagnostic values; serialize only after execution completes. `Model.ToString()` failures are recorded separately without losing execution errors or children.
- Preserve ancestor paths in reports for selected subtrees and detached debug nodes. Failure reports retain the full report's node IDs and keep failures and their ancestors, or only the first node when no failures exist.
- Keep `netstandard2.1` with no package dependencies. Reports are written by a built-in JSON writer, so the assembly can still be copied into hosts that do not restore NuGet dependencies, such as Unity. Existing runner output conditions and window-opening behavior remain unchanged.
- `Execute(depth)` and `ExecuteContinuously` now propagate non-cancellation errors that were not recorded on a Node, matching `Execute(ids)`, instead of logging them and returning a partial tree. Failures from `CreateLabs` and Lab execution are still recorded on Nodes and returned. The `Run` helpers log these errors and return `false`.
- Construct the root Node inside each entry point's error handling, so a failing `TModel` constructor propagates through the same path and releases cancellation resources.

### Removed

- **Behavior change:** remove clipboard copying from report export. `Run` and `RunContinuously` no longer copy reports to the clipboard through `clip.exe`, and `ExportJson` no longer has a `copyToClipboard` parameter. `printResult` still opens the saved report folder and file.

### Fixed

- Apply `ToUncontinuable` from any CompositeLab layer after all successful assertions, including flags set during Assert. Assertion failures remain failures and stop remaining assertions.
- Include ID, depth, and status in `Node.ToString()` diagnostics without invoking the Model's `ToString()`.
- Correct the executor's duplicate-execution error message and remove the unused test field warning.
- Allow spaces, Unicode, and punctuation in Lab IDs by storing IDs as JSON values. Reject `/` at Node construction with an argument error because it is reserved for replay paths (F4).
- Preserve completed children when a `CreateLabs` iterator fails (F5), remove shared XML document mutation (R2), and write consistent UTF-8 report bytes (F7).
- Preserve the complete replay path when repeatedly restoring a detached node or appending descendants to it, without modifying the original result.
- Propagate non-cancellation errors from `Execute(ids)`, including unknown Lab IDs, instead of returning a partial result that appears successful. The `Run` helper logs these errors and returns `false`.
- Replace the busy coordinator loop in `Execute(depth)` with cancellation-aware `SemaphoreSlim` notifications for queued work and completed workers, reducing idle CPU usage in both execution modes.
- Publish worker completion only after the worker Task has finished, so the coordinator observes its final state and exceptions before removing it from tracking.
- Wait for all started workers before `Execute(depth)` returns after failure or cancellation, and observe worker faults that occur during shutdown before disposing cancellation resources.
- Request cancellation only once when a runner limit is exceeded while waiting for workers to finish.

### Added

- Add GitHub Actions CI for Windows and Ubuntu on pushes, pull requests, and manual runs: restore, Release build, full NUnit tests, and per-platform TRX artifacts with .NET SDK 9.0.x. Samples are built but not launched; no package publishing is configured.

## [0.2.0] - 2026-10-15

### Added

- Added `ExecutionMode.ThreadPool` and `ExecutionMode.Inline`, selected through protected constructors on `Project<TModel>` and `CompactProject<TModel>` and exposed through a read-only `ExecutionMode` property.
- Added stable `BlackThunder.UniTest` NuGet packaging with MIT licensing, README, and repository metadata for repository-local consumer feeds.

### Fixed

- Made worker cancellation tests control the execution stage before cancelling, removing scheduling races without changing runtime cancellation behavior.

### Changed

- Converted the active library, tests, sample, and documentation to a .NET-only repository layout.
- Added a `netstandard2.1` library project and `net9.0` NUnit test and console sample projects.
- Replaced source-linked sample compilation with a project reference to `src/UniTest/UniTest.csproj`.
- Replaced the `UNITEST_SINGLETHREAD` build symbol with per-project execution mode selection. Existing parameterless construction defaults to `ThreadPool`; `Inline` yields asynchronously and avoids an additional `Task.Run` without guaranteeing a fixed OS thread.
- Updated consumer installation and external NUnit workflows to pin stable package versions instead of referencing a UniTest development checkout.

### Removed

- Removed Unity package metadata, assembly definitions, asset metadata, and the Unity scene sample.

## [0.1.0] - 2026-06-23

### Added

- Packaged UniTest as an embedded Unity package under `Packages/com.blackthunder.unitest`.
- Added package metadata for Unity and Native C# samples.
- Added Korean package documentation under `Documentation~/Wiki.ko`.
- Added English package documentation under `Documentation~/Wiki.en`.

### Changed

- Moved UniTest runtime code from the legacy Assets plugin location to `Runtime`.
- Moved UniTest framework tests from the legacy Assets plugin location to `Tests`.
- Moved Unity and Native C# samples to `Samples~`.
- Added README Git URL installation guidance.

### Fixed

- Corrected the `TestCase.Confineable(...)` out parameter name from `confied` to `confined` before the first release.
