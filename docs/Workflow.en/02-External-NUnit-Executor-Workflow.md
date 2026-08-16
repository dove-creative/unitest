# External NUnit Executor Workflow

This document defines the operating flow for each domain or framework to configure its own .NET NUnit project under its test folder and run it repeatedly from the CLI.

## Table of Contents

1. Position of CLI Test Automation
2. Test Project Ownership
3. Test Project Configuration
4. Execution Flow
5. Operating Principles

## 1. Position of CLI Test Automation

Place CLI test automation in the following flow.

1. Write test code based on the state-operation table.
2. Confirm that the test target and test code build as SDK projects.
3. Place an NUnit project under the `tests` folder owned by the target domain or framework.
4. Connect the test target and UniTest with `ProjectReference` items.
5. Run tests from the CLI and record the results.
6. If a failure occurs, analyze the cause and return to the implementation or verification step.

## 2. Test Project Ownership

The tested domain or framework owns its external test project. UniTest provides the execution model and runtime APIs, but does not contain executors for other frameworks in this repository.

Use the following default layout.

```text
domain-framework/
  src/
    Domain/
      Domain.csproj
  tests/
    Domain.Tests/
      Domain.Tests.csproj
```

Keep the test code and project file in the same folder. If multiple test sets contain the same global type names, separate them into distinct test projects.

## 3. Test Project Configuration

Use the following basic project configuration.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Domain/Domain.csproj" />
    <ProjectReference Include="path/to/unitest/src/UniTest/UniTest.csproj" />
  </ItemGroup>
</Project>
```

`Microsoft.NET.Test.Sdk` and `NUnit3TestAdapter` allow `dotnet test` to discover and run the tests. The repository declares package references and does not vendor restored DLLs.

## 4. Execution Flow

`NUnit3TestAdapter` discovers the tests and `dotnet test` executes them.

```powershell
dotnet restore tests/Domain.Tests/Domain.Tests.csproj
dotnet test tests/Domain.Tests/Domain.Tests.csproj --no-restore
```

Include the execution result in the test result summary in `03-Results.md`.

## 5. Operating Principles

- The tested domain or framework owns its test project under `tests`.
- Connect runtime code with `ProjectReference` instead of duplicating it.
- Prefer `Assert.That(..., Is/Has/Does/Contains...)` for value verification.
- Do not use NUnit 4-only APIs or `NUnit.Framework.Legacy`.
- Use `Assert.Throws` or `Assert.ThrowsAsync` for exception verification.
- Include CLI failures in the result document and return to the implementation, verification, or test step based on the cause.
