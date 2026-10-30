param(
    [string]$DotnetPath = 'dotnet',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repository = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$sampleDirectory = Join-Path $repository "samples/bin/$Configuration/net10.0"
$verificationDirectory = Join-Path $repository ('artifacts/sample-verification/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $verificationDirectory -Force | Out-Null

foreach ($name in @('BlackThunder.UniTest.Samples.dll', 'BlackThunder.UniTest.Samples.deps.json',
        'BlackThunder.UniTest.Samples.runtimeconfig.json', 'BlackThunder.UniTest.dll')) {
    Copy-Item -LiteralPath (Join-Path $sampleDirectory $name) -Destination $verificationDirectory
}

function Invoke-Sample([string[]]$CommandArguments, [string]$InputText, [int]$ExpectedExitCode) {
    $start = [System.Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $DotnetPath
    $start.ArgumentList.Add((Join-Path $verificationDirectory 'BlackThunder.UniTest.Samples.dll'))
    foreach ($argument in $CommandArguments) { $start.ArgumentList.Add($argument) }
    $start.WorkingDirectory = $verificationDirectory
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if ($CommandArguments.Count -eq 0) {
            $process.StandardInput.Write($InputText)
            $process.StandardInput.Close()
        }
        # One-shot commands must exit even while stdin remains open.
        if (-not $process.WaitForExit(60000)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw "Sample did not exit: $($CommandArguments -join ' ')"
        }
        $result = [pscustomobject]@{
            ExitCode = $process.ExitCode
            Stdout = $stdout.GetAwaiter().GetResult()
            Stderr = $stderr.GetAwaiter().GetResult()
        }
        if ($result.ExitCode -ne $ExpectedExitCode) {
            throw "Unexpected exit code $($result.ExitCode): $($CommandArguments -join ' ')`n$($result.Stdout)`n$($result.Stderr)"
        }
        if ($CommandArguments.Count -gt 0 -and $result.Stdout.Contains('sample>')) {
            throw 'One-shot execution entered the interactive prompt.'
        }
        return $result
    }
    finally { $process.Dispose() }
}

$help = Invoke-Sample @('help') '' 0
if (-not $help.Stdout.Contains('single-replay') -or $help.Stderr.Length -ne 0) { throw 'Invalid help output.' }
$scenarios = [ordered]@{
    'single' = 'SingleState'
    'single-replay' = 'SingleStateReplay'
    'multi' = 'MultiState'
    'single-continuous' = 'SingleStateContinuous'
    'multi-continuous' = 'MultiStateContinuous'
}
foreach ($scenario in $scenarios.GetEnumerator()) {
    $result = Invoke-Sample @($scenario.Key) '' 0
    $reportPath = Join-Path $verificationDirectory "SampleOutput/$($scenario.Value).json"
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if ($report.nodes.Count -lt 1 -or -not $result.Stdout.Contains($reportPath)) { throw "Invalid report: $reportPath" }
    Write-Output "Passed: $($scenario.Key) ($($report.nodes.Count) report nodes)"
}
$unknown = Invoke-Sample @('unknown-command') '' 1
if (-not $unknown.Stderr.Contains('Unknown command') -or -not $unknown.Stdout.Contains('Commands:')) { throw 'Missing command error/help.' }
$extra = Invoke-Sample @('single', 'unexpected') '' 1
if (-not $extra.Stderr.Contains('additional arguments')) { throw 'Missing argument error.' }
$interactive = Invoke-Sample @() "`nhelp`nunknown-command`nexit`n" 0
if ([regex]::Matches($interactive.Stdout, 'sample>').Count -ne 4 -or -not $interactive.Stderr.Contains('Unknown command')) {
    throw 'Interactive blank/help/error/exit behavior failed.'
}

# A file in place of the output directory exercises unexpected I/O failures.
$outputDirectory = Join-Path $verificationDirectory 'SampleOutput'
$savedReports = Join-Path $verificationDirectory 'SavedReports'
foreach ($path in @($outputDirectory, $savedReports)) {
    $resolved = [System.IO.Path]::GetFullPath($path)
    if (-not $resolved.StartsWith($verificationDirectory + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::Ordinal)) {
        throw "Output path escaped the verification directory: $resolved"
    }
}
Move-Item -LiteralPath $outputDirectory -Destination $savedReports
[System.IO.File]::WriteAllText($outputDirectory, 'Output-directory failure fixture.')
$failure = Invoke-Sample @('single') '' 1
if (-not $failure.Stderr.Contains('Unexpected error')) { throw 'Unexpected exception was not reported.' }
$recovery = Invoke-Sample @() "single`nhelp`nexit`n" 0
if ([regex]::Matches($recovery.Stdout, 'sample>').Count -ne 3 -or -not $recovery.Stderr.Contains('Unexpected error')) {
    throw 'Interactive mode did not recover after a scenario exception.'
}
Write-Output 'Passed: help, unknown command, extra arguments, interactive input, and exception recovery.'
