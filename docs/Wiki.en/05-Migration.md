# Migrating to 0.4.0

UniTest 0.4.0 is a breaking release that aligns public namespaces and assembly names with the shared conventions.

## Consumer Changes

| 0.3.0 | 0.4.0 |
| --- | --- |
| `using UniTest;` | `using BlackThunder.UniTest;` |
| Qualified types such as `UniTest.Model` | New names such as `BlackThunder.UniTest.Model` |
| `UniTest.dll` | `BlackThunder.UniTest.dll` |
| `ID` on Lab, CompactLab, CompositeLab, Node, and ILab | `Id` on those types |
| `Lab<TModel>.DefaultID` | `Lab<TModel>.DefaultId` |
| Run's named argument `executionIDs` | `executionIds` |
| `BlackThunder.UniTest` version `[0.3.0]` | `[0.4.0]` with the same package ID |

Existing `ILab<TModel>` implementations must also rename their `ID` property to `Id`. There are no compatibility wrappers for old namespaces or member names. Rebuild consumers against the new package, and update old names held in reflection code, type-name strings, or DLL loading settings.

The package ID and `netstandard2.1` target remain the same. Replace the old DLL for direct references, or add the validated new package to the local feed and pin its exact version for NuGet consumers. Do not overwrite an existing package version.

## Execution Data

Lab identifier strings, slash-separated replay paths, JSON fields, and node relationships retain their existing format. `Id` renames the code identifier without changing stored Lab IDs. Diagnostic strings derived from runtime types, such as qualified exception names, use the new namespace.

JSON reports introduced in 0.3.0 and the post-Assert behavior of `ToUncontinuable` are retained. For migration from the 0.2.0 XML API, see the JSON report section in [Usage](04-Usage.md).

## Repository Development and Samples

Development uses the .NET 10 SDK and C# 9. The solution is now `BlackThunder.UniTest.sln`, and the sample project is `samples/BlackThunder.UniTest.Samples.csproj`. Test and sample directories, assemblies, and namespaces also follow the `BlackThunder.UniTest` hierarchy.

With a command argument, the sample runs once and returns `0` for success or `1` for failure, cancellation, invalid commands, or exceptions. Without arguments, it opens the interactive prompt. Reports go to `SampleOutput` beside the executable, without automatically opening external windows.

The multi-state sample's expected model now reflects the existing subject behavior: an unlicensed rider's mount request still stops charging first. Library execution/cancellation algorithms and the sample subject's behavior are unchanged.
