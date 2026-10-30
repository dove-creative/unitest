# UniTest Usage Samples

This .NET 10 console application demonstrates single-state tests, composed state tests, replay, and continuous execution with UniTest.

## Run

From the repository root, start the interactive prompt or run one command and exit:

```powershell
dotnet run --project samples/BlackThunder.UniTest.Samples.csproj
dotnet run --project samples/BlackThunder.UniTest.Samples.csproj -- single
```

## Commands

| Command | Purpose | Report |
| --- | --- | --- |
| `single` | Explore the single-state model to depth 5 | `SingleState.json` |
| `single-replay` | Replay `Ignite/Mount_Null/Mount_Targeted` | `SingleStateReplay.json` |
| `multi` | Explore the combined rider, battery, and charging states to depth 5 | `MultiState.json` |
| `single-continuous` | Follow a single-state path for up to 20 steps | `SingleStateContinuous.json` |
| `multi-continuous` | Follow a multi-state path for up to 50 steps | `MultiStateContinuous.json` |
| `help` | Show command descriptions | None |
| `exit` | Close the app | None |

One-shot execution returns `0` on success and `1` for a failed or cancelled scenario, unknown command, extra arguments, or unexpected exception. Command errors go to standard error. Commands do not accept additional arguments.

Interactive execution prints `sample>`, ignores blank input, and continues after command errors. `exit` or end-of-input closes the prompt. A continuous scenario may finish before its depth limit when no sustainable branch remains.

## Output

Reports are UTF-8 JSON under `AppContext.BaseDirectory/SampleOutput`. A Release build normally writes them under `samples/bin/Release/net10.0/SampleOutput`. The sample prints each report path and does not open external windows. Normal domain logs are suppressed; warnings and errors remain visible.

The charging model accounts for a non-null mount request stopping charging before rider validation, including an unlicensed rider's expected exception. The sample keeps the domain's existing behavior and verifies it through the composed model.

## Verification

After a Release solution build, run the command, output, exit-code, and interactive recovery checks:

```powershell
pwsh -File .github/scripts/verify-samples.ps1
```

The checks run copies of the compiled sample under the ignored `artifacts/sample-verification` directory. They exercise all five scenarios and an output-directory failure without changing source files.
