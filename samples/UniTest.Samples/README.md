# UniTest Usage Sample

This .NET 9 console application runs the UniTest single-state and multi-state examples.

## Run

```powershell
dotnet run --project UniTest.Samples.csproj
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
