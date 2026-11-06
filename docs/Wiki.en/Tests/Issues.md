# Test Issues

This document is the shared list of unresolved defects found through testing and points for user review.
It distinguishes confirmed defects, candidates for further coverage, and issues already resolved.
The baseline is the verification of both full test projects, Unit and Recursion, and the sample CLI at 04:11 Korea time on 2026-11-07.

## Contents

- Current Status
- Unresolved Defects
- Further Coverage Review
- Resolved Defects
    - ISSUE-01 CompositeLab Misclassifies Shared Expected Exceptions
    - ISSUE-02 CompositeLab Misclassifies an Exception Declaration
    - Expected Exception Contract Change

- Resolved Test Issues
- Record Maintenance

## Current Status

| Category | Passed / Total | Findings |
| --- | --- | --- |
| Ordinary unit tests | 590 / 590 | All passed. |
| UniTest-based self-tests | 51 / 51 | CompositeLab passed all 51 scenarios in both execution modes. |
| Total | 641 / 641 | No runtime defects are recorded as unresolved. |

See the [shared results](Result.md) for all modules, and the [CompositeLab self-test results](UniTest/CompositeLab/03.%20Results.md) for detailed evidence of the fixes and reproductions.

## Unresolved Defects

As of 2026-11-07, there are no confirmed unresolved runtime defects.
Verification covered both full test projects and the entire [verify-samples.ps1](../../../.github/scripts/verify-samples.ps1) script.
All 5 sample scenarios, help, invalid commands and extra arguments, interactive input, and I/O error recovery passed.

The following scope could not be executed during this verification.

- [ ] Check CI results for Ubuntu execution.

## Further Coverage Review

The following improvement candidates come from verification boundaries stated in the existing documentation. They are not confirmed defects, and no tests were added for them in this work.
Before choosing further work, check for overlap with existing Project and UniTest verification.

| ID | Target | Current verification boundary | Point to review or strengthen | Evidence |
| --- | --- | --- | --- | --- |
| REVIEW-01 | Project cancellation and failure | PU19·PU20 verify waiting for shutdown but do not cover diagnostic precedence when an Actor fails after cancellation. | Define the final state and exception precedence, then consider a test that reproduces the order with entry and release signals. | [Project verification boundaries](Unit/Project/01.%20Tables.md) |
| REVIEW-02 | ProjectRunner | PU05·PU22 verify false for a missing ID and waiting for shutdown after a time limit. They do not cover every return path, processLimit, or failure JSON output. | Define coverage for termination and return values under a processing-count limit, and for failure report output. | [Project verification boundaries](Unit/Project/01.%20Tables.md) |
| REVIEW-03 | TestDesigner·TestExecutor | Duplicate-execution rejection cases do not wait for the first call to complete, but do not guarantee that it remains at a specific execution point. Some error cases check only exception types, and cancellation after completion checks only the absence of an exception. | Use entry signals to fix the point of a duplicate call during execution, and consider additional assertions for diagnostic fields and preservation of completed state. Also review direct coverage by execution mode. | [Designer plan](Unit/TestDesigner/02.%20Plans.md), [Executor plan](Unit/TestExecutor/02.%20Plans.md) |
| REVIEW-04 | Reporting | RP12 verifies a lone high surrogate. Parameterized coverage of both execution modes is limited to RP03·RP09. | Consider separate inputs for other malformed Unicode combinations and additional execution-mode combinations. | [Reporting plan](Unit/Reporting/02.%20Plans.md) |

## Resolved Defects

The following runtime defects were fixed on 2026-11-07 and verified by rerunning the tests. Do not count them again as unresolved defects.
Reproduction conditions and results before and after the fixes are in the [CompositeLab self-test results](UniTest/CompositeLab/03.%20Results.md).

### ISSUE-01 CompositeLab Misclassifies Shared Expected Exceptions

- Target: CompositeLab.
- Status: Resolved. Last verified by the rerun at 04:01 Korea time on 2026-11-07, passing in both execution modes.
- Impact: When the original and an extension expected the same exception, execution failed as though the expected exception had not occurred even when it had, and skipped subsequent Assert callbacks.
- Cause: The original Lab handled the expected exception internally, preventing CompositeLab from observing its occurrence.
- Action: CompositeLab now receives the original Actor's actual exception and compares it directly with every layer's declaration.
- Scenarios: CL30·CL31·CL51.

### ISSUE-02 CompositeLab Misclassifies an Exception Declaration

- Target: CompositeLab.
- Status: Resolved. Last verified by the rerun at 04:01 Korea time on 2026-11-07, passing in both execution modes.
- Impact: If any layer declared Exception in a composition whose original declared an expected exception, all Assert callbacks ran and execution passed even when no exception occurred.
- Cause: The MissingExpectedException thrown by the original Lab to indicate a missing expected exception was compared with declarations as if it were the actual exception.
- Action: The same fix as ISSUE-01 excludes that MissingExpectedException from the comparison.
- Scenarios: CL47·CL48.

### Expected Exception Contract Change

- Contract: Execution succeeds only when the actual exception matches the declared type on every declaring layer. The user approved this change on 2026-11-07.
- Behavior change: Compositions that previously passed when only one layer matched (CL49·CL50) now fail with MissingExpectedException for the first nonmatching layer.
- Diagnostic improvement: A MissingExpectedException with an actual exception preserves it as InnerException (CL46).
- Revised fix: The earlier bool-return proposal in the result document was not applied because it would also accept other extension declarations, introducing a regression.
- Public signatures are unchanged. The [CHANGELOG](../../../CHANGELOG.md) records the behavior change.

## Resolved Test Issues

The following items were corrected and verified as passing during earlier test improvements. Do not count them again as unresolved defects.

| Item | Completed work | Verification record |
| --- | --- | --- |
| Model zero boundary and terminal state | Corrected the zero-boundary expectation in M01 and strengthened M02·M03 to prepare and verify a terminal state before lowering the limit. | [Self-test results](UniTest/Model/03.%20Results.md), [unit-test results](Unit/Model/03.%20Results.md) |
| Execution guarantees in Model·Lab self-tests | Verified scenario IDs, execution counts, and Success states, and strengthened Lab with actual repeated paths and a wrong-expectation control. | [Model results](UniTest/Model/03.%20Results.md), [Lab results](UniTest/Lab/03.%20Results.md) |
| Node cancellation during replay | Replaced N14's elapsed-time-based cancellation with an Actor entry signal and a release gate, and verified that the new Lab did not execute. | N14 improvement history in the [Node unit-test results](Unit/Node/03.%20Results.md) |
| Node·TestCase failure controls | Isolated the execution context that allowed deliberate internal NUnit failures to contaminate the outer result. Preserved the wrong expectations and failure-detection checks. | [Node self-test results](UniTest/Node/03.%20Results.md), [TestCase self-test results](UniTest/TestCase/03.%20Results.md) |

## Record Maintenance

This document tracks review-item status and next checks; actual commands, results, TRX files, and logs are maintained in each module's result document.
Historical integrated artifacts have been deleted, so use the latest files linked by each module's result document to inspect raw evidence.
When resolving an item, record actual execution evidence in the module's result document before updating this document's status and checklist.
