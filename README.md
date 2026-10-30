# UniTest

[한국어](README.ko.md)

UniTest is a .NET testing framework that generates and verifies execution paths from model states and available actions.

Define the actions and expected results for each state instead of listing every path manually. UniTest explores subsequent states using the execution history.

## Features

- State-based path generation: create the Labs available in the current Model and explore subsequent states.
- AAA verification: separate preparation, actual behavior, and verification with Arrange, Act, and Assert.
- Independent state composition: combine tests across state axes with TestCase, Merge, and Extend.
- Failure reproduction: replay saved Lab ID paths or investigate failures using detached debug nodes.
- Continuous execution and JSON reports: inspect long execution paths and save independent diagnostic snapshots after execution.

## Requirements and Installation

The library targets `netstandard2.1` and has no external runtime package dependencies. Building, testing, and running samples in this repository requires the .NET 10 SDK. All projects use C# 9.

Keep the validated `BlackThunder.UniTest.0.4.0.nupkg` in the consuming repository's `packages` directory. Add the following root `NuGet.config`, then add the package. Packages are distributed through repository-local files and are not published to a public NuGet feed.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

```powershell
dotnet add package BlackThunder.UniTest --version "[0.4.0]"
```

The package ID, namespace, and DLL name are `BlackThunder.UniTest`, `BlackThunder.UniTest`, and `BlackThunder.UniTest.dll`, respectively. Existing consumers should follow the [0.4.0 migration guide](docs/Wiki.en/05-Migration.md). Hosts that load DLLs directly use the DLL under `lib/netstandard2.1`.

## Quick Start

Run the following code in a console project that references the package. It creates a real Counter and a Model with its expected value, then verifies the Increment path to depth 3.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackThunder.UniTest;

namespace Example.CounterTests
{
    public class Counter
    {
        public int Value { get; private set; }
        public void Increment() => Value++;
    }

    public class CounterModel : Model
    {
        public Counter Counter => (Counter)Subject;
        public int ExpectedValue;

        public CounterModel() => Subject = new Counter();
    }

    public class CounterProject : CompactProject<CounterModel>
    {
        public override IEnumerable<CompactLab<CounterModel>> CreateCompactLabs(CounterModel model)
        {
            yield return new CompactLab<CounterModel>("Increment",
                arranger: current => current.ExpectedValue++,
                actor: current => current.Counter.Increment(),
                asserter: current =>
                {
                    if (current.Counter.Value != current.ExpectedValue)
                        throw new InvalidOperationException("Counter state mismatched.");
                });
        }
    }

    public static class Program
    {
        public static async Task Main()
        {
            var root = await new CounterProject().Execute(3);
            Console.WriteLine(root.GetReport().ToJson());
        }
    }
}
```

For runnable examples with multiple actions and combined states, see the [sample guide](samples/README.md).

```powershell
dotnet run --project samples/BlackThunder.UniTest.Samples.csproj
dotnet run --project samples/BlackThunder.UniTest.Samples.csproj -- single
```

Without arguments, the app opens the `sample>` prompt. With a command argument, it runs once and exits. Reports are saved in `SampleOutput` beside the sample executable.

## Main APIs

- `Model`: holds the actual subject, expected state, and execution history.
- `Project<TModel>` and `CompactProject<TModel>`: generate state-specific Labs and execute paths.
- `Lab<TModel>`, `CompactLab<TModel>`, and `CompositeLab<TModel>`: define and compose preparation, actions, and assertions.
- `TestCase`, `Merge`, and `Extend`: combine conditions and actions across state axes.
- `Execute` and `ExecuteContinuously`: execute full paths, ID paths, or continuous paths.
- `GetReport`, `GetFailedReports`, and `ExportJson`: inspect execution results and save JSON.

## Documentation

- [Testing methodology](docs/Wiki.en/00-Unit-Test-Guideline.md): state-action tables and test scope
- [Overview](docs/Wiki.en/01-Overview.md): purpose, core structure, and representative flow
- [Implementation](docs/Wiki.en/02-Implementations.md): component responsibilities, execution, and shutdown
- [Extensions](docs/Wiki.en/03-Uni-Test-Extensions.md): failure stopping, replay, and continuous execution
- [Usage](docs/Wiki.en/04-Usage.md): API selection and single-state/multi-state tutorials
- [Test authoring workflow](docs/Workflow.en/01-Test-Authoring-Workflow.md): agent-assisted authoring and result recording
- [External NUnit execution](docs/Workflow.en/02-External-NUnit-Executor-Workflow.md): CLI verification in consuming projects
- [Contributing](CONTRIBUTING.md): shared conventions, development environment, and package verification

## Tests

```powershell
dotnet restore BlackThunder.UniTest.sln
dotnet build BlackThunder.UniTest.sln --configuration Release --no-restore -m:1
dotnet test BlackThunder.UniTest.sln --configuration Release --no-build --no-restore -m:1
pwsh -File .github/scripts/verify-samples.ps1
```

[CI](.github/workflows/ci.yml) runs the same build, full tests, and sample CLI checks on Windows and Ubuntu. TRX results are retained for seven days; packages are not published.

## License

Distributed under the MIT license. See [LICENSE.md](LICENSE.md).
