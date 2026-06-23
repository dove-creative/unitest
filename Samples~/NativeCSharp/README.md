# Native C# Usage Sample

This sample runs the UniTest single-state and multi-state examples without using Unity APIs.

## Run

```powershell
dotnet run --project UniTest.NativeCSharp.Samples.csproj
```

Enter a command at the `sample>` prompt. Empty input asks again without running a scenario, and `exit` closes the sample app.

```text
single
single-replay
multi
single-continuous
multi-continuous
help
exit
```

Reports are written under the sample app output directory at `UniTest/Samples/NativeCSharp`.
