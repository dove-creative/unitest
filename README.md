# UniTest

[Korean README](README.ko.md)

UniTest is a Unity/C# testing framework for verifying systems where each action changes which tests can run next.

Define each action as a `Lab`, and UniTest automatically expands executable test paths from the current `Model` state and execution history.

## Agent-Assisted CLI Automation

UniTest provides test authoring and external execution automation workflows so agent-assisted test writing and repeated CLI verification can continue on the same model structure.

AI agents first check the `Documentation~/Workflow.en` entry in [Documentation](#documentation) for the actual working procedure when they are responsible for UniTest-based test authoring or repeated CLI verification.

## Features

- State-based test generation: creates test flows for the current state in `Project<TModel>.CreateLabs(...)`.
- AAA execution units: composes Arrange, Act, and Assert flows with `Lab<TModel>` and `CompactLab<TModel>`.
- Path expansion: `Node<TModel>` preserves the execution history and independently creates the next test state.
- Multi-state composition: combines multiple state axes hierarchically with `TestCase`, `Merge(...)`, and `Extend(...)`.
- Replay and continuous execution: selects full paths, long single paths, or failed path replay with `Run(...)`, `RunContinuously(...)`, and `Execute(ids)`.
- XML reports: saves and reviews execution results and failed paths as XML.

## Installation

UniTest can be used as a folder-based Unity package in Unity projects, or by directly including the runtime source in Native C# projects.

### Use In Unity

In Package Manager, use `Add package from git URL` with the following URL.

```text
https://github.com/dove-creative/unitest.git#v0.1.0
```

For local development or embedded package usage, place the package directly.

1. Place this folder at `Packages/com.blackthunder.unitest` in a Unity project.
2. Add `UNITEST` to Player Settings > Scripting Define Symbols.
3. Reference the `UniTest` asmdef from test assemblies or sample assemblies.

The `UniTest` runtime asmdef uses the `UNITEST` define constraint. Without this symbol, core types such as `Project<TModel>`, `Model`, and `Lab<TModel>` are not compiled.

The Unity sample can be imported from the Package Manager `Samples` area as `Unity Usage`, or inspected inside the package at `Samples~/Unity`.

### Use In Native C#

There is no separate NuGet package yet. In Native C# projects, keep this package folder as a source dependency and include the `Runtime` source in compilation.

```xml
<ItemGroup>
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Infrastructure/**/*.cs" LinkBase="UniTest/Infrastructure" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Lab/**/*.cs" LinkBase="UniTest/Lab" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Model/**/*.cs" LinkBase="UniTest/Model" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Project/**/*.cs" LinkBase="UniTest/Project" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Tools/**/*.cs" LinkBase="UniTest/Tools" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/Node.cs" Link="UniTest/Node.cs" />
  <Compile Include="path/to/com.blackthunder.unitest/Runtime/TestCase.cs" Link="UniTest/TestCase.cs" />
</ItemGroup>
```

The Native C# sample runs without Unity APIs.

```powershell
cd Samples~/NativeCSharp
dotnet run --project UniTest.NativeCSharp.Samples.csproj
```

After running it, enter one of `single`, `single-replay`, `multi`, `single-continuous`, `multi-continuous`, `help`, or `exit` at the `sample>` prompt. Reports are saved under `UniTest/Samples/NativeCSharp` in the sample app output folder.

## Quick Start

The example below carries the counter's current value and expected value together, then automatically expands the available `Increment` and `Decrement` paths to depth 3.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniTest;

public sealed class Counter
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }

    public void Decrement()
    {
        Value--;
    }
}

public sealed class CounterModel : Model
{
    public Counter Counter
    {
        get => (Counter)Subject;
        set => Subject = value;
    }

    public int ExpectedValue;
}

public sealed class CounterProject : Project<CounterModel>
{
    public override IEnumerable<ILab<CounterModel>> CreateLabs(CounterModel model)
    {
        if (model.Counter == null)
        {
            yield return new CompactLab<CounterModel>("Ignite")
            {
                Actor = m =>
                {
                    m.Counter = new Counter();
                    m.ExpectedValue = 0;
                },
                Asserter = Check
            }.Build();

            yield break;
        }

        yield return new CompactLab<CounterModel>("Increment")
        {
            Arranger = m => m.ExpectedValue++,
            Actor = m => m.Counter.Increment(),
            Asserter = Check
        }.Build();

        yield return new CompactLab<CounterModel>("Decrement")
        {
            Arranger = m => m.ExpectedValue--,
            Actor = m => m.Counter.Decrement(),
            Asserter = Check
        }.Build();
    }

    private static void Check(CounterModel model)
    {
        if (model.Counter.Value != model.ExpectedValue)
            throw new InvalidOperationException("Counter state mismatched.");
    }
}

public static class CounterRunner
{
    public static Task<bool> RunAsync()
    {
        return new CounterProject()
            .Run(
                Path.Combine(AppContext.BaseDirectory, "UniTestReports"),
                "Counter",
                depth: 3,
                printResult: true);
    }
}
```

This example shows the following flow.

- `Model` carries both the real target, `Counter`, and the expected state, `ExpectedValue`.
- `CreateLabs(...)` creates `Ignite` from the starting state, then creates `Increment` and `Decrement` from later states.
- `Run(...)` executes the available paths and outputs an XML report.

In a Unity project, the same pattern can be called from a MonoBehaviour or Editor test entry point.

## Main APIs

- `Model`: stores the test target `Subject`, execution history, sustainability status, and state string to write into reports.
- `Project<TModel>`: generates the available `Lab` list from the current `Model` and executes the test graph.
- `Lab<TModel>`: an AAA test unit that includes metadata, expected exceptions, and sustainability settings.
- `CompactLab<TModel>`: a helper for quickly writing a simple AAA flow with delegates.
- `ILab<TModel>`: the execution-unit interface shared by `Lab` and `CompositeLab`.
- `TestCase`: passes which action and child condition should be generated in multi-state tests.
- `Merge(...)`: combines a state-condition Lab and an actual-action template into one Lab.
- `Extend(...)`: adds another state layer's Arrange/Assert around an existing Lab.
- `Run(...)`: executes a full path or a specified execution history and outputs an XML report.
- `RunContinuously(...)`: deterministically selects one available path and runs a long continuous execution.
- `Execute(ids)`: re-executes only the execution history separated by `/`.

## Documentation

Detailed documentation is available in `Documentation~/Wiki.en`.

- [00-Unit-Test-Guideline.md](Documentation~/Wiki.en/00-Unit-Test-Guideline.md): state-action table writing guidelines
- [01-Overview.md](Documentation~/Wiki.en/01-Overview.md): feature purpose and overall flow
- [02-Implementations.md](Documentation~/Wiki.en/02-Implementations.md): implementation structure and execution units
- [03-Uni-Test-Extensions.md](Documentation~/Wiki.en/03-Uni-Test-Extensions.md): extension APIs and composition patterns
- [04-Usage.md](Documentation~/Wiki.en/04-Usage.md): usage examples and call guidelines

AI agents follow the workflow documents in `Documentation~/Workflow.en` when they write UniTest-based tests or organize domain-owned POCO test execution paths outside Unity. They first check the test authoring mode and documentation record flow, then apply the External NUnit Executor flow only to tests that do not require the Unity execution environment.

- [01-Test-Authoring-Workflow.md](Documentation~/Workflow.en/01-Test-Authoring-Workflow.md): test authoring, planning, and result recording flow
- [02-External-NUnit-Executor-Workflow.md](Documentation~/Workflow.en/02-External-NUnit-Executor-Workflow.md): domain-owned external NUnit executor setup flow

Korean documentation is available in `Documentation~/Wiki.ko`.

## Tests

Test code is in the `Tests` folder and uses Unity Test Framework with NUnit.

To run the package's own tests in Unity, enable `UNITEST`, `UNITY_INCLUDE_TESTS`, and the target test asmdef symbol, either `UNITEST_TEST_UT` or `UNITEST_TEST_RT`. If the package is used as a separated package, also check the Unity project's testables settings and test asmdef references.

## License

UniTest is distributed under the MIT license. See [LICENSE.md](LICENSE.md) for details.
