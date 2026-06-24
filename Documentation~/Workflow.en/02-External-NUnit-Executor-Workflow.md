# External NUnit Executor Workflow

This document defines the operational flow for configuring each domain or framework's own External NUnit Executor under its test folder when building and running POCO tests outside the Unity Editor.

This document is not intended to replace the Unity Test Runner. Tests that depend on the Unity lifecycle, Scene, AssetDatabase, PlayMode, or Editor API use the Unity Test Runner or Unity batchmode. Conversely, tests for pure C# logic, NUnit Assert, and code separated from Unity dependencies run quickly from the CLI through a domain-owned external test project.

---

## Table of Contents

1. Position of CLI Test Automation
2. External NUnit Executor Ownership
3. Test Project Configuration
4. Execution Flow
5. Operating Principles

---

## 1. Position of CLI Test Automation

In the UniTest test authoring flow, first distinguish whether the test actually needs the Unity execution environment.

Tests that do not need the Unity execution environment are built and run from the CLI without opening the Unity Editor. This allows implementers to verify changes faster and directly connect test failure results to the test result document.

CLI test automation is placed in the following flow.

1. Write test code based on the state-operation table.
2. Check whether the test code can compile without Unity API.
3. Place an external test project under the test folder owned by the target domain or framework.
4. Link the required runtime sources and test sources into the external test project.
5. Run tests from the CLI and record the results.
6. If a failure occurs, analyze the cause and return to the implementation step or verification step.

---

## 2. External NUnit Executor Ownership

The External NUnit Executor is owned by the tested domain or framework.

The UniTest package provides the test execution model and runtime APIs, but it does not contain external executors for other frameworks. When a domain needs to run its own tests through UniTest, that domain places a separate external test project under its `Tests` folder.

The default layout is as follows.

```text
Packages/
  domain-package/
    Tests/
      ExternalNUnitExecutor/
        ExternalNUnitExecutor.csproj
```

`ExternalNUnitExecutor.csproj` connects test target files with `Link` instead of copying them. This keeps the test code in its original location, while the external executor project is responsible only for the CLI build and test execution path.

---

## 3. Test Project Configuration

The external executor project does not enable Unity-related defines. Therefore, Unity-only code inside test files must be inside Unity define branches such as `UNITY_2017_1_OR_NEWER`, and pure .NET API must be used in branches where Unity is unavailable.

For example, a test file that uses `Console.WriteLine` should explicitly include `using System;` or use the `System.Console.WriteLine(...)` form so that it can compile outside Unity as well.

The basic project configuration follows this flow.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <IsTestProject>true</IsTestProject>
    <DefineConstants>DOMAIN_TESTS;UNITEST;UNITEST_SINGLETHREAD</DefineConstants>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\..\Runtime\**\*.cs" LinkBase="Domain\Runtime" />
    <Compile Include="..\*.cs" LinkBase="Domain\Tests" />
    <Compile Include="..\UniTest\*.cs" LinkBase="Domain\Tests\UniTest" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Node.cs" LinkBase="UniTest" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\TestCase.cs" LinkBase="UniTest" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Infrastructure\*.cs" LinkBase="UniTest\Infrastructure" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Lab\*.cs" LinkBase="UniTest\Lab" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Model\*.cs" LinkBase="UniTest\Model" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Project\*.cs" LinkBase="UniTest\Project" />
    <Compile Include="..\..\..\com.blackthunder.unitest\Runtime\Tools\NodeExtensions.cs" LinkBase="UniTest\Tools" />
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
  </ItemGroup>
</Project>
```

The example above links the domain package's runtime and test sources together with the runtime sources from the sibling `com.blackthunder.unitest` package. Adjust the actual paths, defines, and test folder names to match the domain package structure.

The NUnit framework is restored through the NuGet `NUnit` 3.14.0 package. Therefore, the External NUnit Executor can build with only the .NET SDK and a NuGet restore environment, without the Unity Editor or the Unity project's `Library` cache.

`Microsoft.NET.Test.Sdk` and `NUnit3TestAdapter` are test platform packages that allow `dotnet test` to discover and run this test project. The NUnit API baseline used by the test code is the common API supported by both Unity Test Runner and NuGet NUnit 3.14.0, and the external executor only runs the same test sources in a runner outside Unity.

`NUnit`, `NUnit3TestAdapter`, and `Microsoft.NET.Test.Sdk` are MIT-licensed packages restored from NuGet. This repository does not vendor those DLLs and only declares project references.

---

## 4. Execution Flow

The external executor does not have its own `Program.cs`. Tests are discovered by `NUnit3TestAdapter` and executed by `dotnet test`.

```powershell
dotnet test Packages\domain-package\Tests\ExternalNUnitExecutor\ExternalNUnitExecutor.csproj
```

If restore has already completed and only build and execution need to be repeated, run it as follows.

```powershell
dotnet test --no-restore Packages\domain-package\Tests\ExternalNUnitExecutor\ExternalNUnitExecutor.csproj
```

In the test authoring workflow, include this execution result in the test execution result summary in `03-Results.md`.

---

## 5. Operating Principles

- The external executor is not a tool for verifying whether the Unity Editor runs. It is a tool for quickly iterating test code that does not depend on Unity.
- Test files should clearly branch Unity-only code and pure .NET code whenever possible.
- `ExternalNUnitExecutor` does not own test code. Test code remains in the original module or test folder, and the executor project links and runs those files.
- The UniTest package does not own external test projects for other domains.
- Domain-specific external test projects are owned by each domain or framework's `Tests` folder.
- The external executor runs with NuGet NUnit 3.14.0, but test code uses only common NUnit APIs that can also compile with Unity's NUnit 3.5-based custom DLL.
- General value verification Assert should be written around `Assert.That(..., Is/Has/Does/Contains...)`, and NUnit 4-only APIs or `NUnit.Framework.Legacy` should not be used.
- Exception verification allows `Assert.Throws` / `Assert.ThrowsAsync`, which are supported by both Unity NUnit 3.5 and NuGet NUnit 3.14.0.
- CLI failures are included in the test result document, and depending on the cause, the work returns to the implementation step, verification step, or test step.
- Tests that require Unity API, Scene, AssetDatabase, or PlayMode behavior are not forced through this path and are separated into Unity Test Runner or Unity batchmode verification.
