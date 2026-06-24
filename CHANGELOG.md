# Changelog

This file records public changes to UniTest.

The format follows Keep a Changelog, and version numbers follow Semantic Versioning.

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
