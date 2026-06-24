# Table of Contents

1. What to Check Before Starting
2. Shortest Start Flow
3. Criteria for Choosing Classes and Methods
4. Tutorial 1. Testing a Single-State Object
5. Tutorial 2. Combining Multi-State Tests
6. Running and Reading Results

---

This document does not re-explain the implementation structure of Uni Test. Instead, it covers the usage flow for moving state-operation tables and Uni Test components into actual test code.

Therefore, the explanation order follows the table and model that a test author must prepare first, the shortest execution flow, and then single-state and multi-state examples, rather than the detailed implementation of internal classes.

## 1. What to Check Before Starting

### 1-1. The `UNITEST` Symbol Must Be Defined

The `UniTest` assembly is compiled based on the `UNITEST` define constraint. Without this symbol, core types such as `Project<TModel>`, `Model`, and `Lab<TModel>` cannot be used.

Test assemblies usually also need `UNITY_INCLUDE_TESTS`. For example, if a test is executed by the Unity Test Runner, check that the test asmdef references `UniTest` and `nunit.framework.dll`, and that the required define constraints match the current project settings.

In other words, if "I wrote test code but the type cannot be found," it is better to first check asmdef references and define constraints.

### 1-2. Create the State-Operation Table First

Uni Test is not a tool that lists tests directly one line at a time. It creates possible `Lab`s from the current state and automatically extends subsequent states according to the execution history. Therefore, the following two items must be organized before code.

1. States the object can have
2. Operations that can be executed in each state and their expected results

Without this table, `CreateLabs` can easily grow into a simple bundle of conditionals. Conversely, if a state-operation table exists, each branch can be moved directly into a `Lab` generation rule.

Here, state is a classification of conditions that determines how the current system handles an operation. Classifications that change the result of the same operation, such as `waiting`, `running`, and `terminated`, are states. Conditions that only describe data shape, such as a list being empty or a specific field being null, are not states.

An operation is a meaningful action that can be called or observed from outside the system. Actions that produce results, such as `start`, `stop`, and `save`, are operations, but simple helpers or internal calculation functions are not placed as operations in the state-operation table.

### 1-3. Keep Actual State and Expected State Together in Model

In Uni Test, `Model` is not only a place to store the test target object. It stores the actual object `Subject`, mock fields that represent expected state, and assets required for test progress together.

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

Here, `Kickboard` is the actual test target, and `rider` and `isDisposed` are the expected state of the test. `Asserter` checks whether these two worlds match.

### 1-4. Actual Example Code Locations

The examples in this document are based on the actual test code under `Samples~/Unity/Scripts`.

| Example | File | What This Document Shows |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------- |
| Execution entry point | [Manager.cs](../../Samples~/Unity/Scripts/Manager.cs) | `Run`, `RunContinuously`, replay execution methods |
| Single-state model | [SingleStateTest/Model.cs](../../Samples~/Unity/Scripts/SingleStateTest/Model.cs) | `Subject`, expected state, asset composition |
| Single-state project | [SingleStateTest/Project.cs](../../Samples~/Unity/Scripts/SingleStateTest/Project.cs) | `CreateLabs`, state-specific Lab generation |
| Multi-state model | [MultiStateTest/Model.cs](../../Samples~/Unity/Scripts/MultiStateTest/Model.cs) | Expected value composition for battery, charging, and mounted states |
| Multi-state entry point | [MultiStateTest/Project.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project.cs) | `MainTestCase` and hierarchical generator entry point |
| Multi-state generators | [Project-Charge.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Charge.cs), [Project-Battery.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Battery.cs), [Project-Kickboard.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Kickboard.cs) | State-layer-specific Lab composition |
| Actual operation template | [GetTemplates.cs](../../Samples~/Unity/Scripts/MultiStateTest/GetTemplates.cs) | Actual Act definition by `MainTestCase` |
| Test target objects | [SingleStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/SingleStatedKickboard.cs), [MultiStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/MultiStatedKickboard.cs) | Actual state transitions of the test target |

---

## 2. Shortest Start Flow

The simplest usage flow is the following six steps.

1. Create a `Model` that represents the test target state.
2. Create a test project that inherits `Project<Model>`.
3. Return the list of `Lab`s appropriate for the current state from `CreateLabs(Model model)`.
4. Put Arrange, Act, and Assert into each `Lab`.
5. Run the tests with `Run(...)` or `Execute(...)`.
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

        // Continue returning Labs executable according to the current state.
    }

    private void Check(Model model)
    {
        Assert.AreEqual(model.isDisposed, model.Kickboard.IsDisposed);
        Assert.AreSame(model.rider, model.Kickboard.Rider);
    }
}
```

Execution can be started in Unity as follows.

```csharp
await new SingleState.Project()
    .Run(
        Configuration.ProjectPath,
        "SingleState",
        depth: 5,
        printResult: true);
```

With only this flow, tests can start from `Ignite` and automatically expand every possible subsequent test from the current state to a fixed depth.

---

## 3. Criteria for Choosing Classes and Methods

At first glance, Uni Test code may look like it has many types, but there are not many axes that the user must directly hold.

| Target | When to Use | Role |
| --- | --- | --- |
| `Model` | When the test target and expected state must be carried together | Stores `Subject`, mock state, and execution helper data |
| `Project<TModel>` | When creating the entry point for the whole test | Creates the list of possible `Lab`s from the current `Model` |
| `Lab<TModel>` | When metadata, expected exceptions, or extension composition is needed | Defines Arrange, Act, and Assert directly |
| `CompactLab<TModel>` | When writing simple AAA quickly | Composes `Lab<TModel>` in a simple delegate form |
| `TestCase` | When passing which operation to create in a multi-state test | Passes selection conditions between lower/upper test generators |
| `Merge(...)` | When combining an actual operation template into the current Lab | Combines state conditions and the actual Act into one Lab |
| `Extend(...)` | When adding a state verification layer on top of an existing Lab | Creates a `CompositeLab` and adds Arrange/Assert layers |
| `Run(...)` | When XML results are needed during Unity execution | Handles full execution and XML export together |
| `RunContinuously(...)` | When a long continuable path is needed instead of every combination | Selects an arbitrary path and performs a long continuous test |
| `Execute(ids)` | When a failed path needs to be re-run | Reproduces only the specified execution history |

In practice, it is convenient to think as follows.

- First create `Model` and `Project`.
- Start simple tests with `CompactLab`.
- Expand to `Lab` when expected exceptions, metadata, or composition becomes necessary.
- In multi-state tests, divide test generators with `TestCase`, `Merge`, and `Extend`.

---

## 4. Tutorial 1. Testing a Single-State Object

The first step is to test an object with one state axis, as in `SingleStateTest`. Here, the test determines whether the kickboard is in `Idle`, `Mounted`, or `Disposed`, and creates only the operations possible in that state as `Lab`s.

The actual code is in [SingleStateTest/Model.cs](../../Samples~/Unity/Scripts/SingleStateTest/Model.cs) and [SingleStateTest/Project.cs](../../Samples~/Unity/Scripts/SingleStateTest/Project.cs). The original behavior of the test target can be checked in [SingleStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/SingleStatedKickboard.cs).

The table below is the state-operation table that `SingleStateTest.Project` actually moves into code. The notation rules are the same as the state-operation table in [[00-Unit-Test-Guideline]].

| State    | -   | Mount              | <                  | <    | <                  | <            | Ride               | Dismount | Dispose                    |
| -------- | --- | ------------------ | ------------------ | ---- | ------------------ | ------------ | ------------------ | -------- | -------------------------- |
|          | -   | Licensed           | Targeted           | Same | Not Licensed       | Null         |                    |          |                            |
|          | -   |                    |                    |      |                    |              |                    |          |                            |
| Idle     | -   | **Mounted**        | **Mounted**        | X    | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | -        | **Disposed**               |
| Mounted  | -   | _InvalidOperation_ | _InvalidOperation_ | -    | _InvalidOperation_ | ^            | `[Ride]`           | **Idle** | `<Dismount>` -> `<Dispose>` |
| Disposed | -   | _ObjectDisposed_   | _ObjectDisposed_   | X    | _ObjectDisposed_   | ^            | _ObjectDisposed_   | -        | -                          |

The `Targeted` column is the path where the current rider is not `TargetedRider`, and the path where the same rider is passed again is read as the `Same` column. Each cell in the table is converted into one or more `Lab`s inside `CreateLabs`. For example, `Mount` `Licensed` in the `Idle` row is a success Lab, while `Not Licensed` is a Lab that verifies the expected exception and then no longer extends the path.

### 4-1. Determine the Current State in Code

`CreateLabs` looks at the current `Model` and creates subsequent tests. Therefore, a function that determines the current state reliably is needed first.

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

If state determination is gathered in one place, later `Mount`, `Ride`, `Dismount`, and `Dispose` tests can be split using the same standard.

### 4-2. Create the Starting State Separately

At first, the test target object does not exist. In this case, create an `Ignite` Lab, create the object, and initialize the expected state.

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

This Lab becomes the actual starting point of the test graph. Nodes created afterward replay this `Ignite` execution history and then attach the next Lab to create independent subsequent states.

### 4-3. Add Only the Operations Possible in Each State

For example, `Mount` has different expected results depending on the current state. In the `Idle` state, a licensed user can board, and an unlicensed user should cause an exception.

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

`ToUncontinuable` means that the same path will not be extended further after this test. Attaching it to an operation that expects failure or to a semantically terminated path can reduce unnecessary subsequent test generation.

### 4-4. Change Mock First and Verify State Changes

For operations that change state, such as `Dismount`, first change the expected state in `Arranger`, execute the actual operation in `Actor`, and then compare the two in `Asserter`.

```csharp
labs.Add(new("Dismount")
{
    Arranger = model => model.rider = null,
    Actor = model => model.Kickboard.Dismount(),
    Asserter = Check
});
```

This pattern is the most frequently used form in Uni Test.

1. Update the expected state first.
2. Execute the operation on the actual object.
3. Check whether the expected state and actual state are the same.

### 4-5. Limit Follow-Up Verification Immediately After Termination

Even after an object is `Dispose`d, several operations can still be checked. However, there is no need to extend tests infinitely in the terminated state, so use `RemainingExecutionCount`.

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

This allows follow-up verification to continue only as much as needed after dispose, then stops path expansion afterward.

---

## 5. Tutorial 2. Combining Multi-State Tests

The second step is to test an object with several independent states, as in `MultiStateTest`. For example, a kickboard has not only a mounted state, but also a battery state and a charging state. Writing every combination directly in one function makes the code rapidly more complex.

Uni Test solves this problem by dividing the test generators hierarchically.

The actual code is divided into the following files.

| Role | File |
| --- | --- |
| Model and expected state | [MultiStateTest/Model.cs](../../Samples~/Unity/Scripts/MultiStateTest/Model.cs) |
| Overall test entry point | [MultiStateTest/Project.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project.cs) |
| Charging state generator | [Project-Charge.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Charge.cs) |
| Battery state generator | [Project-Battery.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Battery.cs) |
| Base kickboard state generator | [Project-Kickboard.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Kickboard.cs) |
| Actual operation template | [GetTemplates.cs](../../Samples~/Unity/Scripts/MultiStateTest/GetTemplates.cs) |
| Test target object | [MultiStatedKickboard.cs](../../Samples~/Unity/Scripts/Kickboard/MultiStatedKickboard.cs) |

The base tables for the multi-state example follow the state-operation table format in `00-Unit-Test-Guideline` as-is. However, the Usage document does not gather the tables in one place. Instead, it places each table next to the test generator that moves that table into code.

The design order moves from the base state to the extension states. First, create the kickboard's base mounted state, add battery conditions on top of it, and finally refine the charging state so that it controls the overall operation once more.

When reading a table as code, first map it as follows.

| Table Position | Corresponding Code |
| --------- | -------------------- |
| Top function column | The first value of `TestCase`. For example, `Mount` is `MainTestCase.Mount`. |
| Lower columns grouped by `<` | The second value of `TestCase`. For example, `Licensed` under `Mount` is the string `"Licensed"`. |
| Left state row | State determined from the current `Model`. For example, values such as `Idle`, `Mounted`, `Available`, and `Charging`. |
| Cell content | The `Lab` to create when that state and operation condition meet. |
| `[base]` | Means passing to the next more basic generator. In code, this is usually connected with `Extend(CreateLabs_...)`. |
| Actual operations such as `[Ride]` and `<Dispose>` | Connected to the actual `Actor` Lab created by `GetTemplates`. |

`GetTemplates` is not a function that determines state. It is a function that creates templates that execute actual operations. `CreateLabs_Kickboard`, `CreateLabs_Battery`, and `CreateLabs_Charge` decide "what state is this now" and "what result is expected in this cell," and `GetTemplates` attaches the actual `Actor` at the end.

For example, the `Mount` / `Licensed` path continues as follows.

1. The `Mount` column in the `Operations` table becomes `MainTestCase.Mount`.
2. The `Licensed` column under `Mount` in the `Kickboard` table becomes `"Licensed"`.
3. `Licensed()` in `CreateLabs_Kickboard` looks at the current state row and creates the expected result.
4. `GetTemplates(tc)` receives the same `TestCase` and creates the `Mount_Licensed` template.
5. `Merge(GetTemplates(tc))` combines the expected-state Lab and the actual-call Lab into one.

In other words, when reading the table, a name such as `Licensed` should be understood as a path name that leads to the actual operation template in `GetTemplates`, rather than as an actual function name.

### 5-1. Kickboard Table and CreateLabs_Kickboard

The `Kickboard` table handles the base mounted state at the top. The code layer for this table is `CreateLabs_Kickboard` in [Project-Kickboard.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Kickboard.cs).

| Kickboard | -   | Mount              | <    | <                  | <            | Ride               | Dismount | Dispose                    |
| --------- | --- | ------------------ | ---- | ------------------ | ------------ | ------------------ | -------- | -------------------------- |
|           | -   | Licensed           | Same | Not Licensed       | Null         |                    |          |                            |
|           | -   |                    |      |                    |              |                    |          |                            |
| Idle      | -   | **Mounted**        | X    | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | -        | **Disposed**               |
| Mounted   | -   | _InvalidOperation_ | -    | _InvalidOperation_ | ^            | `[Ride]`           | **Idle** | `<Dismount>` -> `<Dispose>` |
| Disposed  | -   | _ObjectDisposed_   | X    | _ObjectDisposed_   | ^            | _ObjectDisposed_   | -        | -                          |

`Licensed` is not a separate user operation, but a lower condition under the `Mount` column. Therefore, in the code below, `MainTestCase.Mount` chooses the table's `Mount` column, and `"Licensed"` chooses the `Licensed` lower column below it.

```csharp
if (testCase.Confineable(0, out var _tc, MainTestCase.Mount)) // Mount column in the table
{
    if (_tc.Confineable(1, out tc, "Licensed")) // Licensed column under Mount
        Licensed(); // Creates the Idle / Mounted / Disposed cells of the Licensed column.

    if (_tc.Confineable(1, out tc, "Same") && model.rider != null)
        Same();

    if (_tc.Confineable(1, out tc, "NotLicensed"))
        NotLicensed();

    if (_tc.Confineable(1, out tc, "Null"))
        Null();
}
```

Inside `Licensed()`, the left state row is checked again. For example, if the current state is `Idle`, the cell where the `Idle` row and the `Mount` / `Licensed` column meet in the table is `**Mounted**`. Therefore, it fills the expected rider first and then merges with the actual `Mount_Licensed` template.

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

That is, `Licensed()` is not a function that performs the actual boarding. It decides which `Lab` to create by current state in the `Licensed` column of the table, and the actual call is handled by `Mount_Licensed` created by `GetTemplates(tc)`.

### 5-2. Battery Table and CreateLabs_Battery

The `Battery` table adds battery conditions on top of the base kickboard operations. The code layer for this table is `CreateLabs_Battery` in [Project-Battery.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Battery.cs).

| Battery    | -   | Check         | <              | Mount (override) | Ride (override)            |
| ---------- | --- | ------------- | -------------- | ---------------- | -------------------------- |
|            | -   | Battery > 10% | Battery <= 10% |                  |                            |
|            | -   |               |                |                  |                            |
| Available  | -   | -             | **Discharged** | `[base]`         | `[base]` -> `[Use Battery]` |
| Discharged | -   | **Available** | -              | -                | -                          |

Here, `[base]` means passing to `CreateLabs_Kickboard`, the base kickboard state layer. Conversely, when `Mount` or `Ride` is `-` in `Discharged`, it does not go down to the base kickboard state. It executes only the actual operation template and checks whether the state is maintained.

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

The `Battery` layer handles only the conditions it can judge. If the battery is sufficient, `Mount`, `Ride`, `Dismount`, and `Dispose` decisions from the base kickboard are needed, so it passes the test to the next generator.

### 5-3. Charge State Table and CreateLabs_Charge

The `Charge State` table adds charging state on top of battery conditions. The code layer for this table is `CreateLabs_Charge` in [Project-Charge.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project-Charge.cs).

| Charge State | -   | Charge                      | Do Charge     | Stop Charging    | Mount (override)                          | Ride (override)    | Dispose (override)           |
| ------------ | --- | --------------------------- | ------------- | ---------------- | ----------------------------------------- | ------------------ | ---------------------------- |
|              | -   |                             |               |                  |                                           |                    |                              |
| Not Charging | -   | `<Dismount>` -> **Charging** | X             | -                | `[base]`                                  | `[base]`           | `[base]`                     |
| Charging     | -   | -                           | `[Do Charge]` | **Not Charging** | `<Stop Charging>` -> `[base if available]` | _InvalidOperation_ | `<Stop Charging>` -> `[base]` |
| Disposed     | -   | _ObjectDisposed_            | X             | -                | `[base]`                                  | _ObjectDisposed_   | -                            |

Here, `[base]` means passing to the next state layer, `CreateLabs_Battery`. For example, in the `Ride (override)` column, the `Charging` row is `_InvalidOperation_`, so this layer directly creates an exception Lab. The `Not Charging` row is `[base]`, so it sends the test down to the battery layer.

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

`Confineable` checks whether a test can be created with the corresponding condition. Flows not directly handled by this layer are gathered with `ConfineableExcept` and sent down to the next step, `CreateLabs_Battery`.

### 5-4. Operations Table and Overall Entry Point

The `Operations` table shows how the state layers created so far connect to user operations. The code layer for this table is `MainTestCase` and `CreateLabs` in [MultiStateTest/Project.cs](../../Samples~/Unity/Scripts/MultiStateTest/Project.cs).

| Operations   | -   | Mount | Ride | Dismount | Charge | Do Charge | Stop Charging | Dispose |
| ------------ | --- | ----- | ---- | -------- | ------ | --------- | ------------- | ------- |
|              | -   |       |      |          |        |           |               |         |
| Kickboard    | -   | Mount | Ride | Dismount | X      | X         | X             | Dispose |
| Battery      | -   | Mount | Ride | -        | X      | X         | X             | Dispose |
| Charge State | -   | Mount | Ride | -        | Charge | Do Charge | Stop Charging | Dispose |

In multi-state tests, each layer must know "which operation is currently being created." For this, the columns of the `Operations` table are defined as `MainTestCase`. For example, the user-visible `Mount` column becomes `MainTestCase.Mount` in code, and lower conditions such as `Licensed` in the `Kickboard` table become the `"Licensed"` value at the next index.

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

This value is stored in `TestCase` and moves between lower and upper generators. The design explanation moves from `Kickboard` toward `Charge State`, but the actual execution entry point first enters `CreateLabs_Charge`, which controls operations from the outermost layer.

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

The core of this structure is that `CreateLabs` does not need to know the whole list. Each state layer judges only the condition it owns and passes the rest to the next generator.

### 5-5. Separate Actual Operations into Templates

In multi-state tests, it is better to separate state conditions and actual operations. [GetTemplates.cs](../../Samples~/Unity/Scripts/MultiStateTest/GetTemplates.cs) creates base Labs with actual `Actor`s. When the `Mount` / `Licensed` path seen earlier reaches the end, it becomes the `Mount_Licensed` template here.

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

The state generator takes this template and combines it with the condition of the current state. This reduces the scattering of actual operation definitions across several state branches.

### 5-6. Use Merge and Extend Separately

`Merge` is used when combining the current Lab and an operation template into one Lab.

```csharp
labs.AddRange(new Lab<Model>("charging", arranger: Check)
    .Merge(GetTemplates(tc)));
```

In this case, the template's `Actor` is executed in the `charging` state, and the verification of the current Lab is also performed together inside the same Lab.

Conversely, `Extend` is used when adding the current state layer on top of another Lab that has already been created.

```csharp
labs.AddRange(new Lab<Model>("available", asserter: Check)
    .Extend(CreateLabs_Kickboard(model, tc)));
```

The result of `Extend` is `CompositeLab`. The execution order can be understood as follows.

1. Execute the existing Lab's Arrange.
2. Execute the extension Lab's Arrange.
3. Execute the existing Lab's Act.
4. Execute the extension Lab's Assert.
5. Execute the existing Lab's Assert.

In other words, the actual operation is executed in only one place, while surrounding state layers add preparation and verification.

---

## 6. Running and Reading Results

### 6-1. Run All Paths

The most common execution method is `Run(...)`. It expands every possible path up to the specified depth, and if `printResult` is enabled, it stores and opens the XML report.

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

`processLimit` is the limit on the number of executed Nodes, and `timeLimit` is the time limit in seconds. If both are `-1`, there is no limit.

### 6-2. Re-Run Only Failed Paths

If the failed execution history is known, it can be re-run as a string. Execution history is a list of Lab IDs separated by `/`.

```csharp
const string History = "Ignite/Mount_Null/Mount_Targeted";

await new SingleState.Project()
    .Run(
        Configuration.ProjectPath,
        "SingleStateReplay",
        History,
        printResult: true);
```

This method is used when reproducing only the path where the problem occurred quickly, without re-running the full combination.

### 6-3. Long Continuous Tests

If every combination is executed, the number of tests can grow sharply. In this case, `RunContinuously(...)` can select one continuable path and execute it for a long time.

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

This method is suitable when the goal is to check whether the state remains stable inside a long execution flow rather than to ensure completeness across every combination.

### 6-4. Debug Again at the Code Level

If the XML report alone is not enough to find the cause, a desired Node can be restored from the execution result and re-executed for IDE debugging.

```csharp
var root = await new SingleState.Project()
    .Execute("Ignite/Mount_Null/Mount_Targeted");

var restored = root.GetLastNode().DetachAndRestore();
restored.Execute();
```

`DetachAndRestore()` creates a separated Node that does not modify the original Node graph. After that, placing a breakpoint on `restored.Execute()` lets the same execution history be reproduced and the problem point be inspected again.

### 6-5. Read the XML Report

The `Run(...)` family internally exports the Node's XML report. When execution succeeds, the full report is saved. When it fails, a report that keeps only the failed Nodes is saved.

The three parts to focus on in the report are as follows.

- Node name: indicates which Lab was executed.
- `History`: execution history up to the corresponding Node.
- `Model`: a string used to read the test target and expected state at that point.

Therefore, it is important to write `Model.ToString()` in an easy-to-read form. This is because the XML report is ultimately a tool for reading "what state it was in and which Lab was executed" on the failed path.

---

In summary, the Uni Test usage flow is as follows.

1. Create the state-operation table first.
2. Keep the actual object and expected state together in `Model`.
3. Create `Lab`s that match the current state in `Project.CreateLabs`.
4. For a single state, start with state branches and `CompactLab`.
5. For multiple states, divide generators with `TestCase`, `Merge`, and `Extend`.
6. Choose the execution method that fits the purpose among `Run`, `RunContinuously`, and `Execute(ids)`.

While the previous documents explain the structure and implementation method of Uni Test, this document is responsible for the first usage flow that moves that structure into actual test code.
