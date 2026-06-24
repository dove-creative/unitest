# UniTest Test Authoring Workflow

This document defines the workflow an agent follows when writing or reinforcing UniTest-based tests.

---

## Table of Contents

1. Select Mode
2. Prepare the Test Documentation Folder
3. Write the State-Operation Table
4. Write the Test Plan
5. Write Test Code
6. Build and Run
7. Record Results and Integration Tests

---

## 1. Select Mode

When starting test authoring work, first check the mode specified by the designer. If no mode is specified, use safe mode as the default.

| Mode | Response to Failure or Design Error |
| --- | --- |
| Safe mode | When a test fails, report the cause and required fix proposal to the designer and wait. |
| Auto mode | When a test fails, directly fix the test code or target code. Report to the designer and wait only when there is a major design error that requires an API change. |
| Delegation mode | Even when an API change is required, the agent modifies the code and documents by itself, then reports the changes and results after the work is complete. |

Mode is the criterion that determines the scope of failure response. The basic flow for writing state-operation tables, test plans, test code, and execution result records remains the same in every mode.

---

## 2. Prepare the Test Documentation Folder

Create a `UniTests` folder in the corresponding module folder of the wiki. If the module already has a `Tests` folder, continue managing test documents under that folder.

Test documents are basically split into the following files.

| File | Role |
| --- | --- |
| `01-Tables.md` | State-operation tables by module |
| `02-Plans.md` | Test plans that move state-operation tables into code |
| `03-Results.md` | Records of major issues and results found during execution |

---

## 3. Write the State-Operation Table

Write the state-operation table for each module or unit in `01-Tables.md`. If an existing `01-Tables.md` file exists, review and reinforce its content based on the current implementation and public API.

The format and meaning of the state-operation table follow the [UniTest State-Operation Guideline](../Wiki.en/00-Unit-Test-Guideline.md).

---

## 4. Write the Test Plan

Before writing or modifying test code based on the state-operation table, create `02-Plans.md`. `02-Plans.md` is a planning document that organizes which cells of the table will be moved into which test files and test functions.

If the total number of test cells in the table is 200 or fewer, write every test. If it exceeds 200, report the expected number of tests to the designer and wait briefly in safe mode and auto mode. In delegation mode, divide the test scope into detailed items and perform each item as an independent test in a separate folder.

Write the test plan according to the following criteria.

- If possible, separate tests into files by object.
- When using the UniTest framework, write a high-level plan for how the test steps and file structure will be authored.
- When not using the UniTest framework, write a high-level plan and list of tests to execute based on the table.
- Separate unit tests and integration tests, and plan integration tests around representative use cases and failure scenarios.

---

## 5. Write Test Code

Write or modify test files according to the test plan. Test code is placed under the `UniTests` folder below the corresponding module or unit folder. If the module already has a `Tests` folder, create and use a `UniTests` folder under that folder.

Even when domain-specific POCO tests must run outside Unity, do not add the test project inside the UniTest package. The corresponding domain folder owns its own external test project.

Separate the test assembly into a `ModuleName.Tests.UniTest.asmdef` file and configure it so that it can be detected by the Unity Editor's Test Runner.

If the test target framework is POCO, compose tests so that they avoid the Unity framework when possible. Tests that do not need Unity API, Scene, AssetDatabase, PlayMode, or Editor API can be configured as `dotnet test` build and execution targets through the [External NUnit Executor workflow](02-External-NUnit-Executor-Workflow.md). Tests that require the Unity execution environment should not be forced into POCO tests, and should instead be separated into Unity Test Runner or Unity batchmode verification.

Write a short comment on each test function so that it is clear which step or cell of `01-Tables.md` and `02-Plans.md` the test corresponds to.

After writing tests, compare the completed tests with the plan document.

- If a test is missing from the plan, add the test.
- If tests were added or changed during implementation, reflect them in `02-Plans.md`.
- If the test code does not match the current table, first update the standard in the table or plan, then align the code.

---

## 6. Build and Run

Build and run the test code. If automatic build is impossible or the current environment requires Unity Test Runner execution, ask the designer to run the tests.

Choose the execution path according to the following criteria.

| Target | Execution Path |
| ------------------------------- | -------------------------------------------------------------------------------------------------- |
| Tests requiring Unity API or Editor environment | Unity Test Runner or Unity batchmode |
| UniTest-based POCO tests | `dotnet test`-based [External NUnit Executor workflow](02-External-NUnit-Executor-Workflow.md), which restores NuGet `NUnit` 3.x and runs the tests |

When a failure occurs during test execution, first check which cell of the table and which item of the plan the failed test corresponds to.

After a failure, report, fix, or wait according to the mode selected in section 1.

---

## 7. Record Results and Integration Tests

If major issues that occurred during execution need to be recorded, write them in `03-Results.md`.

Record the following.

- Build or execution command
- Success and failure results
- Constraints of the Unity environment or CLI environment
- Failure cause and follow-up action
- Reinforcement of consistency between table, plan, and code

When module or unit tests are complete, perform integration tests that include representative use cases and failure scenarios. The execution, reporting, and waiting method for integration tests also follows the mode selected in section 1, and required results are recorded together in `03-Results.md`.
