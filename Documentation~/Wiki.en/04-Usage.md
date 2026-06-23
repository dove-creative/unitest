# Table of Contents

1. Things to Check Before Starting
2. Shortest Start Flow
3. Criteria for Choosing Classes and Methods
4. Tutorial 1. Testing a Single-State Object
5. Tutorial 2. Combining Multi-State Tests
6. Running and Reading Results

---

This document does not explain the implementation structure of UniTest again. It covers the usage flow for moving a state-operation table and UniTest components into actual test code.

Therefore, the explanation order follows the table and model that a test writer must prepare first, the shortest execution flow, and then single-state and multi-state examples, rather than the detailed implementation of internal classes.

## 1. Things to Check Before Starting

### 1-1. The `UNITEST` Symbol Must Be Defined

The `UniTest` assembly is compiled based on the `UNITEST` define constraint. Without this symbol, core types such as `Project<TModel>`, `Model`, and `Lab<TModel>` cannot be used.

Test assemblies usually also need `UNITY_INCLUDE_TESTS`. For example, if a test is executed by the Unity Test Runner, confirm that the test asmdef references `UniTest` and `nunit.framework.dll`, and that the required define constraints match the current project settings.

In other words, if "the test code was written but the type cannot be found", it is better to check asmdef references and define constraints first.

### 1-2. Create the State-Operation Table First

UniTest is not a tool for listing tests line by line directly. It is a tool that generates `Lab` instances available in the current state and automatically expands the following states. Therefore, the two items below must be organized before code.

1. The states the object can have
2. The operations that can be executed in each state and their expected results

Without this table, `CreateLabs` can easily grow into a simple bundle of conditionals. Conversely, when a state-operation table exists, each branch can be moved directly into a `Lab` generation rule.

Here, a state is a classification of conditions that determines how the current system will handle an operation. Classifications that change the result of the same operation, such as `Waiting`, `Running`, and `Finished`, are states. Conditions that only describe the shape of data, such as a list being empty or a particular field being null, are not states.

An operation is a meaningful action that can be called or observed from outside the system. Actions that produce results, such as `Start`, `Stop`, and `Save`, are operations, but simple helpers or internal calculation functions are not placed in the state-operation table as operations.

### 1-3. Store Real State and Expected State Together in the Model

In UniTest, `Model` is not just a place for the object under test. It stores the actual object, `Subject`, mock fields that represent expected state, and assets needed for test execution together.

```csharp
public class Model : UniTest.Model
{
    public SingleStatedKickboard Kickboard
    {
        get => (SingleStatedKickboard)Subject;
        set => Subject = value;
    }

    public Rider rider;
    public bool isDisposed;

    public Rider TargetedRider;
}
```

Here, `Kickboard` is the actual test target, and `rider` and `isDisposed` are the states expected by the test. `Asserter` checks whether these two worlds are the same.

### 1-4. Actual Example Code Locations

The examples in this document are based on the actual test code under `Samples~/Unity/Scripts`.

| Example | File | What This Document Looks At |
| --- | --- | --- |
| Execution entry point | [Manager.cs](../../Samples~/Unity/Scripts/Manager.cs) | `Run`, `RunContinuously`, replay execution method |
| Single-state model | [SingleStateTest/Model.cs](../../Samples~/Unity/Scripts/SingleStateTest/Model.cs) | `Subject`, expected state, asset composition |
| Single-state project | [SingleStateTest/Project.cs](../../Samples~/Unity/Scripts/SingleStateTest/Project.cs) | `CreateLabs`, per-state Lab generation |
| Multi-state model | [MultiStateTest/Model.cs](../../Samples~/Unity/Scripts/MultiStateTest/Model.cs) | Expected values for battery, charging, and mounted state |
| Multi-state entry point | [MultiStateTest/Project.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project.cs) | `MainTestCase` and layered generator entry point |
| Multi-state generators | [Project-Charge.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Charge.cs), [Project-Battery.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Battery.cs), [Project-Kickboard.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Kickboard.cs) | Per-state-layer Lab composition |
| Concrete operation templates | [GetTemplates.cs](../../Samples~/Unity/Scripts/MultiStateTest/GetTemplates.cs) | Actual Act definitions for each `MainTestCase` |
| Objects under test | [SingleStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/SingleStatedKickboard.cs), [MultiStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/MultiStatedKickboard.cs) | Real state transitions of the test target |

---

## 2. Shortest Start Flow

The simplest usage flow has the following six steps.

1. Create a `Model` that represents the target state under test.
2. Create a test project that inherits from `Project<Model>`.
3. In `CreateLabs(Model model)`, return the `Lab` list that matches the current state.
4. Put Arrange, Act, and Assert into each `Lab`.
5. Execute the test with `Run(...)` or `Execute(...)`.
6. Read the XML report or failed execution history.

```csharp
using UniTest;

public class Project : UniTest.Project<Model>
{
    public override IEnumerable<ILab<Model>> CreateLabs(Model model)
    {
        if (model.Kickboard == null)
        {
            yield return new CompactLab<Model>("Ignite")
            {
                Actor = m =>
                {
                    m.Kickboard = new SingleStatedKickboard();
                    m.rider = null;
                    m.isDisposed = false;
                },
                Asserter = Check
            }.Build();

            yield break;
        }

        // Return follow-up Labs according to the current state.
    }

    private void Check(Model model)
    {
        Assert.AreEqual(model.isDisposed, model.Kickboard.IsDisposed);
        Assert.AreSame(model.rider, model.Kickboard.Rider);
    }
}
```

Execution can be started inside Unity as follows.

```csharp
await new SingleState.Project()
    .Run(
        Configuration.ProjectPath,
        "SingleState",
        depth: 5,
        printResult: true);
```

With this flow alone, starting from `Ignite`, every possible following test in the current state can be expanded automatically up to a fixed depth.

---

## 3. Criteria for Choosing Classes and Methods

UniTest code may look like it has many types at first, but there are not many axes that the user must handle directly.

| Target | When to Use | Role |
| --- | --- | --- |
| `Model` | When the test target and expected state must be carried together | Stores `Subject`, mock state, and execution helper data |
| `Project<TModel>` | When creating the entry point for the full test | Creates the `Lab` list available from the current `Model` |
| `Lab<TModel>` | When metadata, expected exceptions, or extension composition is needed | Defines Arrange, Act, and Assert directly |
| `CompactLab<TModel>` | When writing only simple AAA quickly | Composes `Lab<TModel>` as simple delegates |
| `TestCase` | When passing which operation to create in multi-state tests | Passes selection conditions between lower and upper test generators |
| `Merge(...)` | When combining the current Lab with a concrete operation template | Combines a state condition and a real Act in one Lab |
| `Extend(...)` | When adding a state verification layer on top of an existing Lab | Creates a `CompositeLab` and adds Arrange/Assert layers |
| `Run(...)` | When results and XML should be produced during Unity execution | Handles full execution and XML export together |
| `RunContinuously(...)` | When a long sustainable path is needed instead of every combination | Selects an arbitrary path and performs a long continuous test |
| `Execute(ids)` | When re-running a failed path | Reproduces only the specified execution history |

In practice, it is convenient to think as follows.

- First create `Model` and `Project`.
- Start simple tests with `CompactLab`.
- When expected exceptions, metadata, or composition are needed, expand to `Lab`.
- In multi-state tests, divide test generators with `TestCase`, `Merge`, and `Extend`.

---

## 4. Tutorial 1. Testing a Single-State Object

The first step is testing an object with one state axis, as in `SingleStateTest`. Here, determine whether the kickboard is in the `Idle`, `Mounted`, or `Disposed` state, and create only the operations possible in that state as `Lab` instances.

The actual code is in [SingleStateTest/Model.cs](../../Samples~/Unity/Scripts/SingleStateTest/Model.cs) and [SingleStateTest/Project.cs](../../Samples~/Unity/Scripts/SingleStateTest/Project.cs). The original behavior of the test target can be checked in [SingleStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/SingleStatedKickboard.cs).

The table below is the state-operation table that `SingleStateTest.Project` actually moves into code. The notation rules are the same as the state-operation table in [[00-Unit-Test-Guideline]].

| State | - | Mount | < | < | < | < | Ride | Dismount | Dispose |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | - | Licensed | Targeted | Same | Not Licensed | Null | | | |
| | - | | | | | | | | |
| Idle | - | **Mounted** | **Mounted** | X | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | - | **Disposed** |
| Mounted | - | _InvalidOperation_ | _InvalidOperation_ | - | _InvalidOperation_ | ^ | `[Ride]` | **Idle** | `<Dismount>` -> `<Dispose>` |
| Disposed | - | _ObjectDisposed_ | _ObjectDisposed_ | X | _ObjectDisposed_ | ^ | _ObjectDisposed_ | - | - |

The `Targeted` column is the path when the current rider is not `TargetedRider`, and the path that passes the same rider again is read as the `Same` column. Each cell in the table is converted into one or more `Lab` instances inside `CreateLabs`. For example, `Mount` `Licensed` in the `Idle` row is a successful Lab, while `Not Licensed` is a Lab that verifies the expected exception and then does not expand the path further.

### 4-1. Detect the Current State in Code

`CreateLabs` creates following tests by looking at the current `Model`. Therefore, first a function is needed to determine the current state reliably.

```csharp
enum KickboardState
{
    Idle,
    Mounted,
    Disposed,
}

KickboardState GetState(Model model)
{
    if (model.Kickboard.IsDisposed)
        return KickboardState.Disposed;

    if (model.Kickboard.Rider != null)
        return KickboardState.Mounted;

    return KickboardState.Idle;
}
```

If state detection is gathered in one place, the `Mount`, `Ride`, `Dismount`, and `Dispose` tests can all be divided by the same standard.

### 4-2. Create the Starting State Separately

At first, there is no object under test. In this case, create an `Ignite` Lab to create the object and initialize the expected state.

```csharp
if (model.Kickboard == null)
{
    labs.Add(new("Ignite")
    {
        Actor = m =>
        {
            m.Kickboard = new();

            m.rider = null;
            m.isDisposed = false;
            m.TargetedRider = new(true, "Targeted Rider");
        },
        Asserter = Check
    });

    return labs.Select(l => l.Build());
}
```

This Lab becomes the real starting point of the test graph. Later generated Nodes replay this `Ignite` execution history, then attach the next Lab to create an independent following state.

### 4-3. Add Only Operations Possible for Each State

For example, `Mount` has different expected results depending on the current state. In the `Idle` state, a licensed user can mount, and an unlicensed user must raise an exception.

```csharp
case KickboardState.Idle:
    labs.Add(new("Mount_Licensed")
    {
        Arranger = m => m.rider = new Rider(true),
        Actor = m => m.Kickboard.Mount(m.rider),
        Asserter = Check
    });

    labs.Add(new("Mount_NotLicensed")
    {
        Actor = m => Assert.Throws<InvalidOperationException>(
            () => m.Kickboard.Mount(new Rider(false))),
        ToUncontinuable = true
    });
    break;
```

`ToUncontinuable` means that this path will not be expanded further after this test. When attached to an operation that expects failure or a semantically finished path, it can reduce unnecessary following test generation.

### 4-4. Change the Mock First, Then Verify State Changes

For an operation that changes state, such as `Dismount`, first change the expected state in `Arranger`, then execute the actual operation in `Actor`, then compare the two in `Asserter`.

```csharp
labs.Add(new("Dismount")
{
    Arranger = model => model.rider = null,
    Actor = model => model.Kickboard.Dismount(),
    Asserter = Check
});
```

This pattern is the most frequently used form in UniTest.

1. First update the expected state.
2. Execute the operation on the real object.
3. Check whether the expected state and the actual state are the same.

### 4-5. Limit Follow-Up Verification Immediately After Termination

Even after the object has been `Dispose`d, several operations can still be checked. However, there is no need to expand tests infinitely in a terminated state, so use `RemainingExecutionCount`.

```csharp
labs.Add(new("Dispose")
{
    Arranger = model =>
    {
        model.isDisposed = true;
        model.rider = null;
    },
    Actor = model => model.Kickboard.Dispose(),
    Asserter = Check,
    RemainingExecutionCount = 2
});
```

This allows following verification to continue only as much as needed after dispose, and then stops path expansion.

---

## 5. Tutorial 2. Combining Multi-State Tests

The second step is testing an object with several independent states, as in `MultiStateTest`. For example, a kickboard has not only mounted state but also battery state and charging state. If every combination is written directly in one function, the code becomes complex very quickly.

UniTest solves this problem by dividing test generators hierarchically.

The actual code is divided into the following files.

| Role | File |
| --- | --- |
| Model and expected state | [MultiStateTest/Model.cs](../../Samples~/Unity/Scripts/MultiStateTest/Model.cs) |
| Full test entry point | [MultiStateTest/Project.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project.cs) |
| Charging state generator | [Project-Charge.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Charge.cs) |
| Battery state generator | [Project-Battery.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Battery.cs) |
| Base kickboard state generator | [Project-Kickboard.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Kickboard.cs) |
| Concrete operation template | [GetTemplates.cs](../../Samples~/Unity/Scripts/MultiStateTest/GetTemplates.cs) |
| Object under test | [MultiStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/MultiStatedKickboard.cs) |

The base table for the multi-state example follows the state-operation table format from `00-Unit-Test-Guideline` as-is. However, this Usage document does not place every table in one location. Instead, each table is placed next to the test generator that moves that table's layer into code.

The design order descends from the base state to the extended states. First create the kickboard's base mounted state, add the battery condition on top of it, and finally specify how the charging state controls the entire operation once more.

When reading the tables as code, first map the following correspondences.

| Table Position | Corresponding Code |
| --- | --- |
| Top function column | The first value of `TestCase`. For example, `Mount` is `MainTestCase.Mount`. |
| Subcolumns grouped by `<` | The second value of `TestCase`. For example, `Licensed` under `Mount` is the string `"Licensed"`. |
| Left state row | The state determined from the current `Model`. For example, values such as `Idle`, `Mounted`, `Available`, and `Charging`. |
| Cell content | The `Lab` to create when that state and operation condition meet. |
| `[base]` | Pass to the next generator that is more basic. In code, this is usually connected with `Extend(CreateLabs_...)`. |
| Real operations such as `[Ride]` or `<Dispose>` | Connected to the actual `Actor` Lab created by `GetTemplates`. |

`GetTemplates` is not a function that determines state. It is a function that creates templates that execute actual operations. `CreateLabs_Kickboard`, `CreateLabs_Battery`, and `CreateLabs_Charge` decide "what state is this now" and "what result is expected in this cell", while `GetTemplates` attaches the actual `Actor` at the end.

For example, the `Mount` / `Licensed` path proceeds as follows.

1. The `Mount` column in the `Operations` table becomes `MainTestCase.Mount`.
2. The `Licensed` column under `Mount` in the `Kickboard` table becomes `"Licensed"`.
3. `Licensed()` in `CreateLabs_Kickboard` looks at the current state row and creates the expected result.
4. `GetTemplates(tc)` receives the same `TestCase` and creates the `Mount_Licensed` template.
5. `Merge(GetTemplates(tc))` combines the expected-state Lab and the actual-call Lab into one.

In other words, when reading the table, a name like `Licensed` is not so much the actual function name as it is the path name that continues to the actual operation template in `GetTemplates`.

### 5-1. Kickboard Table and CreateLabs_Kickboard

The `Kickboard` table handles the base mounted state at the top. The code layer for this table is `CreateLabs_Kickboard` in [Project-Kickboard.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Kickboard.cs).

| Kickboard | - | Mount | < | < | < | Ride | Dismount | Dispose |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | - | Licensed | Same | Not Licensed | Null | | | |
| | - | | | | | | | |
| Idle | - | **Mounted** | X | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | - | **Disposed** |
| Mounted | - | _InvalidOperation_ | - | _InvalidOperation_ | ^ | `[Ride]` | **Idle** | `<Dismount>` -> `<Dispose>` |
| Disposed | - | _ObjectDisposed_ | X | _ObjectDisposed_ | ^ | _ObjectDisposed_ | - | - |

`Licensed` is not a separate user operation. It is a lower-level condition under the `Mount` column. Therefore, in the code below, `MainTestCase.Mount` selects the `Mount` column in the table, and `"Licensed"` selects the `Licensed` subcolumn under it.

```csharp
if (testCase.Confineable(0, out var _tc, MainTestCase.Mount)) // Mount column in the table
{
    if (_tc.Confineable(1, out tc, "Licensed")) // Licensed column under Mount
        Licensed(); // Creates cells for Idle / Mounted / Disposed in the Licensed column.

    if (_tc.Confineable(1, out tc, "Same") && model.rider != null)
        Same();

    if (_tc.Confineable(1, out tc, "NotLicensed"))
        NotLicensed();

    if (_tc.Confineable(1, out tc, "Null"))
        Null();
}
```

Inside `Licensed()`, the left state row is checked again. For example, if the current state is `Idle`, the cell where the `Idle` row meets the `Mount` / `Licensed` column is `**Mounted**`. Therefore, it first fills the expected rider and then merges with the actual `Mount_Licensed` template.

```csharp
void Licensed()
{
    if (state == State.Idle)
    {
        labs.AddRange(new Lab<Model>
        {
            ID = "idle",
            Arranger = (m, md) => m.rider = (Rider)md.Metadata,
            Asserter = Check
        }.Merge(GetTemplates(tc))); // GetTemplates(tc) -> Mount_Licensed
    }
    else if (state == State.Mounted)
    {
        labs.AddRange(new Lab<Model>("mounted",
            expectedExceptionType: typeof(InvalidOperationException),
            toUncontinuable: true)
            .Merge(GetTemplates(tc)));
    }
    else if (state == State.Disposed)
    {
        labs.AddRange(new Lab<Model>("disposed",
            asserter: Check,
            expectedExceptionType: typeof(ObjectDisposedException),
            toUncontinuable: true)
            .Merge(GetTemplates(tc)));
    }
}
```

That is, `Licensed()` is not the function that executes the actual mount. It decides which `Lab` to create for each current state in the table's `Licensed` column, and the actual call is handled by `Mount_Licensed`, which is created by `GetTemplates(tc)`.

### 5-2. Battery Table and CreateLabs_Battery

The `Battery` table adds battery conditions on top of the base kickboard behavior. The code layer for this table is `CreateLabs_Battery` in [Project-Battery.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Battery.cs).

| Battery | - | Check | < | Mount (override) | Ride (override) |
| --- | --- | --- | --- | --- | --- |
| | - | Battery > 10% | Battery <= 10% | | |
| | - | | | | |
| Available | - | - | **Discharged** | `[base]` | `[base]` -> `[Use Battery]` |
| Discharged | - | **Available** | - | - | - |

Here, `[base]` means passing to `CreateLabs_Kickboard`, the base kickboard state layer. Conversely, when `Mount` or `Ride` is `-` in `Discharged`, it does not go down to the base kickboard state and instead only executes the actual operation template to confirm that the state is preserved.

```csharp
if (testCase.Confineable(0, out var _tc, MainTestCase.Mount)) // Mount (override) column in the Battery table
{
    if (!model.Kickboard.Available) // Discharged row
    {
        labs.AddRange(new Lab<Model>("discharged", asserter: Check)
            .Merge(GetTemplates(_tc))); // '-' cell in the table: execute only the Mount template without going down to base.
    }
    else // Available row
    {
        labs.AddRange(new Lab<Model>("available", asserter: Check)
            .Extend(CreateLabs_Kickboard(model, _tc))); // [base] cell in the table
    }
}
```

The `Battery` layer handles only the conditions it can determine. If the battery is sufficient, the base kickboard's `Mount`, `Ride`, `Dismount`, and `Dispose` decisions are needed, so it passes to the next generator.

### 5-3. Charge State Table and CreateLabs_Charge

The `Charge State` table adds the charging state on top of the battery conditions. The code layer for this table is `CreateLabs_Charge` in [Project-Charge.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Charge.cs).

| Charge State | - | Charge | Do Charge | Stop Charging | Mount (override) | Ride (override) | Dispose (override) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| | - | | | | | | |
| Not Charging | - | `<Dismount>` -> **Charging** | X | - | `[base]` | `[base]` | `[base]` |
| Charging | - | - | `[Do Charge]` | **Not Charging** | `<Stop Charging>` -> `[base if available]` | _InvalidOperation_ | `<Stop Charging>` -> `[base]` |
| Disposed | - | _ObjectDisposed_ | X | - | `[base]` | _ObjectDisposed_ | - |

Here, `[base]` means passing to the next state layer, `CreateLabs_Battery`. For example, in the `Ride (override)` column, the `Charging` row is `_InvalidOperation_`, so this layer creates an exception Lab directly. The `Not Charging` row is `[base]`, so it sends the operation down to the battery layer.

```csharp
if (testCase.Confineable(0, out tc, MainTestCase.Ride)) // Ride (override) column in the Charge State table
    Ride();

void Ride()
{
    if (model.charging) // Charging row: _InvalidOperation_
    {
        labs.AddRange(new Lab<Model>("charging",
            expectedExceptionType: typeof(InvalidOperationException),
            toUncontinuable: true)
            .Merge(GetTemplates(tc)));
    }
    else // Not Charging row: [base]
    {
        labs.AddRange(new Lab<Model>("notCharging")
            .Extend(CreateLabs_Battery(model, tc)));
    }
}
```

`Confineable` checks whether a test can be created with that condition. Flows that are not handled directly in this layer are collected with `ConfineableExcept` and sent down to the next step, `CreateLabs_Battery`.

### 5-4. Operations Table and Full Entry Point

The `Operations` table shows how the state layers created so far connect to user operations. The code layer for this table is `MainTestCase` and `CreateLabs` in [MultiStateTest/Project.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project.cs).

| Operations | - | Mount | Ride | Dismount | Charge | Do Charge | Stop Charging | Dispose |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | - | | | | | | | |
| Kickboard | - | Mount | Ride | Dismount | X | X | X | Dispose |
| Battery | - | Mount | Ride | - | X | X | X | Dispose |
| Charge State | - | Mount | Ride | - | Charge | Do Charge | Stop Charging | Dispose |

In multi-state tests, each layer must know "which operation is currently being created". For this, the columns of the `Operations` table are defined as `MainTestCase`. For example, the user-visible `Mount` column becomes `MainTestCase.Mount` in code, and a lower-level condition such as `Licensed` in the `Kickboard` table becomes the `"Licensed"` value at the next index.

```csharp
public enum MainTestCase
{
    Create,
    Mount,
    Ride,
    Dismount,
    Charge,
    DoCharge,
    StopCharging,
    Dispose,
}
```

This value is placed in `TestCase` and moves between lower and upper generators. The design explanation descends from `Kickboard` toward `Charge State`, but the actual execution entry point first enters `CreateLabs_Charge`, which controls the operation from the outermost layer.

```csharp
public override IEnumerable<ILab<Model>> CreateLabs(Model model)
{
    if (model.Subject == null)
    {
        foreach (var lab in CreateLabs_Charge(model, new(MainTestCase.Create)))
            yield return lab;

        yield break;
    }

    foreach (MainTestCase testCase in Enum.GetValues(typeof(MainTestCase)))
    {
        if (testCase == MainTestCase.Create)
            continue;

        foreach (var lab in CreateLabs_Charge(model, new(testCase)))
            yield return lab;
    }
}
```

The core of this structure is that `CreateLabs` does not need to know the entire list. Each state layer determines only the conditions it owns, and passes the rest to the next generator.

### 5-5. Separate Concrete Operations Into Templates

In multi-state tests, it is better to separate state conditions and concrete operations. [GetTemplates.cs](../../Samples~/Unity/Scripts/MultiStateTest/GetTemplates.cs) creates base Labs that have the actual `Actor`. When the `Mount` / `Licensed` path described earlier reaches the end, it becomes the `Mount_Licensed` template here.

```csharp
if (testCase.Confineable(0, MainTestCase.Mount))
{
    if (testCase.Confineable(1, "Licensed")) // Mount / Licensed column in the Kickboard table
        yield return new()
        {
            ID = "Mount_Licensed",
            SetMetadata = _ => new Rider(true),
            Actor = (m, md) => m.Kickboard.Mount((Rider)md.Metadata)
        };
}
```

The state generator takes this template and combines it with the current state's condition. This reduces the scattering of real operation definitions across many state branches.

### 5-6. Use Merge and Extend Separately

`Merge` is used when combining the current Lab and an operation template into one Lab.

```csharp
labs.AddRange(new Lab<Model>("charging", arranger: Check)
    .Merge(GetTemplates(tc)));
```

In this case, the template's `Actor` is executed in the `charging` state, and the current Lab's verification is also performed inside the same Lab.

Conversely, `Extend` is used when adding the current state layer on top of another already-created Lab.

```csharp
labs.AddRange(new Lab<Model>("available", asserter: Check)
    .Extend(CreateLabs_Kickboard(model, tc)));
```

The result of `Extend` is a `CompositeLab`. The execution order can be understood as follows.

1. Execute the existing Lab's Arrange.
2. Execute the extension Lab's Arrange.
3. Execute the existing Lab's Act.
4. Execute the extension Lab's Assert.
5. Execute the existing Lab's Assert.

That is, the actual operation is executed in only one place, and the surrounding state layers add preparation and verification.

---

## 6. Running and Reading Results

### 6-1. Full Path Execution

The most common execution method is `Run(...)`. It expands every possible path up to the specified depth, and if `printResult` is enabled, it saves and opens the XML report.

```csharp
await new SingleState.Project()
    .Run(
        Configuration.ProjectPath,
        "SingleState",
        depth: 5,
        processLimit: -1,
        timeLimit: -1,
        printResult: true);
```

`processLimit` is the limit on the number of executed Nodes, and `timeLimit` is the time limit in seconds. If both are `-1`, they are unlimited.

### 6-2. Re-Run Only a Failed Path

If the failed execution history is known, it can be re-run as a string. The execution history is a list of Lab IDs separated by `/`.

```csharp
const string History = "Ignite/Mount_Null/Mount_Targeted";

await new SingleState.Project()
    .Run(
        Configuration.ProjectPath,
        "SingleStateReplay",
        History,
        printResult: true);
```

This method is used when reproducing only the path where the problem occurred quickly, without running the full combination again.

### 6-3. Long Continuous Test

If every combination is executed, the number of tests can increase sharply. In that case, use `RunContinuously(...)` to select one sustainable path and run it for a long time.

```csharp
await new MultiState.Project()
    .RunContinuously(
        Configuration.ProjectPath,
        "MultiState",
        depth: 50,
        processLimit: -1,
        timeLimit: -1,
        printResult: true);
```

This method is suitable when the goal is to check whether state remains stable during a long execution flow, rather than the completeness of all combinations.

### 6-4. Debug Again at Code Level

If it is difficult to find the cause from the XML report alone, restore the desired Node from the execution result and re-run it for IDE debugging.

```csharp
var root = await new SingleState.Project()
    .Execute("Ignite/Mount_Null/Mount_Targeted");

var restored = root.GetLastNode().DetachAndRestore();
restored.Execute();
```

`DetachAndRestore()` creates an isolated Node that does not change the original Node graph. After that, setting a breakpoint on `restored.Execute()` allows the problem point to be inspected again after reproducing the same execution history.

### 6-5. Reading the XML Report

The `Run(...)` family internally exports the Node's XML report. If the run succeeds, it saves the full report. If it fails, it saves a report containing only the failed Node.

The three parts to look at in particular are as follows.

- Node name: indicates which Lab was executed.
- `History`: the execution history that reached that Node.
- `Model`: the string for reading the test target and expected state at that point.

Therefore, it is important to write `Model.ToString()` in a readable way. The XML report is ultimately a tool for reading "what state was this, and which Lab was executed" on a failed path.

---

In summary, the UniTest usage flow is as follows.

1. Create the state-operation table first.
2. Store the real object and expected state together in `Model`.
3. Generate `Lab` instances that match the current state in `Project.CreateLabs`.
4. For a single state, start with state branches and `CompactLab`.
5. For multiple states, divide generators with `TestCase`, `Merge`, and `Extend`.
6. Choose an execution method that fits the purpose among `Run`, `RunContinuously`, and `Execute(ids)`.

If the previous documents explain UniTest's structure and implementation method, this document is responsible for the first usage flow that moves that structure into actual test code.
