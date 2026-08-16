# Overview

An effective unit testing methodology for ensuring system reliability

## Table of Contents

1. Unit Test Overview

2. State - Operation Table
	1. Meaning of State and Operation
	2. Systems with a Single State
	3. Systems with Multiple Independent States

3. Test Principles

4. Test Methods
	1. AI Tests
	2. Unified State-Based Tests
	3. Multiple Independent State-Based Tests
	4. Continuous Tests

5. Closing

## 1. Unit Test Overview

Unit tests detect errors at the module level in advance, improve system stability, make debugging easier, and serve as an existing principle when the system expands later.

However, tests that are not systematic may fail to ensure system reliability, and overly meticulous unit tests can require excessive time and cost, reducing development efficiency.

Uni Test is designed to solve these problems by clarifying the test scope and procedure through state-operation tables and by enabling systematic, efficient unit testing. This document introduces the unit testing concepts and methodology that form the basis of that design.

| Category | Content |
| --- | --- |
| Overview | Introduces the unit testing concept assumed by Uni Test |
| Purpose | Secures system reliability through systematic and efficient tests |
| Implementation | Test design based on state - operation tables |

## 2. State - Operation Table

To verify a system systematically, it is important to clearly understand which functions the system can perform in each state.

A state - operation table organizes the states a system can have as rows and the functions the system can perform as columns, allowing every operation scenario of the system to be checked systematically.

### 2-1. Meaning of State and Operation

A state is a classification of conditions that determines how the current system will handle an operation.
- Examples: `Idle`, `Running`, `Terminated`, and so on

>Whether data is simply empty, whether a specific field is filled, or how many values are in an internal collection is not a state by itself. These values are usually handled as test inputs, preconditions, or boundary conditions.
>- Non-examples: `the list is empty`, `the name field is null`, `there are 3 logs`, and so on

An operation is a meaningful action that can be called or observed from outside the system.
- Examples: `start`, `stop`, `save`, `export`, and so on

>A simple helper, internal implementation detail, or method that does not produce a different result depending on state is not placed as an operation in the state - operation table.
>- Non-examples: `format a string`, `sort an intermediate collection`, and so on

### 2-2. Systems with a Single State

In a system with one state type, the states the system can have and the functions it can perform can be represented in one table. For explanation, this document uses an electric kickboard that a user can ride as an example.

The kickboard has the following three states.

- Idle: the default state
- Mounted: the state where a user is riding
- Disposed: the deleted state

The kickboard can also operate or change its own state through the following functions.

- Mount: a user boards the kickboard (only licensed users are allowed)
- Ride: the kickboard starts moving
- Stop: the kickboard stops
- Dismount: the ride ends
- Dispose: the kickboard object is deleted

The state - operation table for this kickboard is as follows.

| State    | -   | Mount              | <    | <                  | <            | Ride               | Dismount | Dispose                    |
| -------- | --- | ------------------ | ---- | ------------------ | ------------ | ------------------ | -------- | -------------------------- |
|          | -   | Licensed           | Same | Not Licensed       | Null         |                    |          |                            |
|          | -   |                    |      |                    |              |                    |          |                            |
| Idle     | -   | **Mounted**        | X    | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | -        | **Disposed**               |
| Mounted  | -   | _InvalidOperation_ | -    | _InvalidOperation_ | ^            | `[Ride]`           | **Idle** | `<Dismount>` -> `<Dispose>` |
| Disposed | -   | _ObjectDisposed_   | X    | _ObjectDisposed_   | ^            | _ObjectDisposed_   | -        | -                          |

Table description

- State (left column): displays the states the system can have (Idle, Mounted, Disposed)
	- A column that contains only - is inserted between the state column and the content columns as a separator.
- Function (top row): displays the functions the kickboard can execute
	- One empty row is placed between the function row and the content row as a separator.
- Content
	- `<`: indicates that the item belongs to the same function group as the function in the cell to the left. In other words, it continues the function column on the left.
	- `^`: indicates that the operation or result in the cell above is followed as-is. It continues the content above without repeating the same content.
- Cell: displays the behavior when a specific function is executed in each state
	- State change: **bold**
	- Unique function: `[square bracket inline block]`
	- Operation execution: `<angle bracket inline block>`
	- Exception occurrence: _italic_
	- No change: -
	- No state: X

Using a state - operation table in this way allows the possible operation scenarios of the system to be checked systematically.

### 2-3. Systems with Multiple Independent States

For a system with multiple independent state types, the number of possible system states increases exponentially as the number of state types increases, so expressing them in a single table can be difficult. In this case, create a table for each state type, combine them into an operations table for callable functions, and use that table to match the corresponding state.

For explanation, assume the two state types below are added to the previous kickboard.

- Battery: battery remaining state
	- Available: the battery is sufficient and normal riding is possible
	- Discharged: the battery is insufficient and normal riding is difficult

- Charge State: charging progress state
	- Not Charging: not currently charging
	- Charging: the kickboard is charging

The kickboard can perform the following functions in each state type.

- Checking: check the battery state while riding
- Charge: charge after stopping the kickboard
- Stop Charging: end kickboard charging
- Mount (function addition): allow Mount only when Battery is Available
- Ride (function addition): disallow Ride while Charge State is Charging, and start Checking after Ride
- Stop (function addition): end Checking before Stop
- Dispose (function addition): if Charge State is Charging, stop charging and then Dispose

At this point, the full state - operation tables for the kickboard can be written as follows.

#### Table Description

- Kickboard table: state-operation table for the base state type of the system
- Battery table: state-operation table for the Battery state type
- Charge State table: state-operation table for the Charge State state type
- Operations table: table for functions the user can actually call

#### Function Override Marker

A column marked with (override) represents an overridden function and controls the call to the upper function.

For example, when the Battery state type is added to the kickboard, the lower function Battery.Mount controls the original upper function Mount so that the original Mount can execute only when the battery is Available.

#### Operations Table

In a system with multiple independent states, users commonly control several states at the same time through the completed interface. The Operations table makes it intuitive to see which operations affect each state.

Read the table as follows.

1. Check operations by state type from lower to upper.
2. If the operation is defined in the lower state type, apply it.
3. If the operation is not defined in the lower state type, apply the operation from the upper state type.

For example, in a kickboard without additional state types, all operations connect to the Kickboard row. On the other hand, in a kickboard where Battery and Charge State have been added, operations are checked in the order Charge State, Battery, Kickboard, and only operations that are not redefined by a lower state type are checked in the upper state type.

#### Kickboard

| Kickboard | -   | Mount              | <    | <                  | <            | Ride               | Dismount | Dispose                    |
| --------- | --- | ------------------ | ---- | ------------------ | ------------ | ------------------ | -------- | -------------------------- |
|           | -   | Licensed           | Same | Not Licensed       | Null         |                    |          |                            |
|           | -   |                    |      |                    |              |                    |          |                            |
| Idle      | -   | **Mounted**        | X    | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | -        | **Disposed**               |
| Mounted   | -   | _InvalidOperation_ | -    | _InvalidOperation_ | ^            | `[Ride]`           | **Idle** | `<Dismount>` -> `<Dispose>` |
| Disposed  | -   | _ObjectDisposed_   | X    | _ObjectDisposed_   | ^            | _ObjectDisposed_   | -        | -                          |

#### Battery

| Battery    | -   | Check         | <              | Mount (override) | Ride (override)            |
| ---------- | --- | ------------- | -------------- | ---------------- | -------------------------- |
|            | -   | Battery > 10% | Battery <= 10% |                  |                            |
|            | -   |               |                |                  |                            |
| Available  | -   | -             | **Discharged** | `[base]`         | `[base]` -> `[Use Battery]` |
| Discharged | -   | **Available** | -              | -                | -                          |

#### Charge State

| Charge State | -   | Charge                      | Do Charge     | Stop Charging    | Mount (override)                          | Ride (override)    | Dispose (override)           |
| ------------ | --- | --------------------------- | ------------- | ---------------- | ----------------------------------------- | ------------------ | ---------------------------- |
|              | -   |                             |               |                  |                                           |                    |                              |
| Not Charging | -   | `<Dismount>` -> **Charging** | X             | -                | `[base]`                                  | `[base]`           | `[base]`                     |
| Charging     | -   | -                           | `[Do Charge]` | **Not Charging** | `<Stop Charging>` -> `[base if available]` | _InvalidOperation_ | `<Stop Charging>` -> `[base]` |
| Disposed     | -   | _ObjectDisposed_            | X             | -                | `[base]`                                  | _ObjectDisposed_   | -                            |

### Operations

| Operations | -   | Mount | Ride | Dismount | Charge | Do Charge | Stop Charging | Dispose |
| ---------- | --- | ----- | ---- | -------- | ------ | --------- | ------------- | ------- |
|            | -   |       |      |          |        |           |               |         |
| Kickboard  | -   | Mount | Ride | Dismount | X      | X         | X             | Dispose |
| Battery    | -   | Mount | Ride | -        | X      | X         | X             | Dispose |
| Charge State | -   | Mount | Ride | -        | Charge | Do Charge | Stop Charging | Dispose |

## 3. Test Principles

When designing unit tests, the following principles must be followed.

### Single Verification Principle

A test must not verify two or more operations or state changes at the same time.

This principle verifies the behavior of a specific function completely without interference, increasing the reliability of that function's test and making debugging easier.

#### Incorrect Verification Example

Performing Mount and Ride on the kickboard in sequence and verifying both functions at the same time

>After calling Kickboard.Mount(), check whether Kickboard.Ride() succeeds

In this case, when the test fails, it is difficult to clearly identify which function caused the problem. Even if the test succeeds, it cannot guarantee that each function worked as intended.

#### Correct Verification Example

1. First verify Mount.

>After calling Kickboard.Mount(), check whether the state changed to Mounted

2. Verify Ride separately based on the verified Mount.

>ex) Check whether Ride() succeeds only when the state is Mounted

This makes the cause clear when the test fails and ensures that test success means the corresponding function worked as intended.

## 4. Test Methods

Systematic unit tests play an important role in securing system reliability. The state - operation table introduced above can be used to verify the system systematically.

However, testing every case in a system requires a large amount of time and resources. Especially when the system is large or continuous tests are performed, tests can instead become an obstacle to development.

Therefore, when designing tests, it is important to choose an appropriate testing method for the situation and purpose. This part introduces various test execution options and methods so that developers can choose the right testing method for the situation.

### 4-1. Test Options

Many system functions are designed on the premise of intended situations, and in unintended situations they are designed to ignore input or throw exceptions.

Because these situations are usually unlikely by design, when time is limited, a strategy can be chosen that omits tests for unintended situations.

However, this can create the risk of not verifying how the system behaves in extreme situations. In particular, deployed code should in principle be tested in every situation to ensure stability.

The following is an example of applying each test option to three test situations.

| Situation | A | B | C |
| --- | --- | --- | --- |
| Check whether charging proceeds when the kickboard calls Charge while Discharged | O | O | O |
| Check whether the call is ignored when the kickboard calls Charge while Charging | O | O | X |
| Check whether an error is returned when the kickboard calls Charge while Disposed | O | X | X |

#### A. Test Every Case

Check whether the function returns the intended result in every possible state of the system

- Applicable situations
	- When thorough system stability is required, such as immediately before release
	- When behavior verification in extreme situations is important (for example, a linear algebra processing system)

- Strengths and weaknesses
	- Thorough verification is possible
	- Resource and time consumption is high

#### B. Test Only Cases That Do Not Throw Exceptions

Check whether normal behavior is performed in situations where the call is allowed

- Applicable situations
	- When major function behavior must be verified quickly in the early development stage
	- When time and resources are limited

- Strengths and weaknesses
	- Efficient and fast verification is possible
	- Errors can be difficult to discover in advance because exception situations are insufficiently verified

#### C. Test Only Cases Corresponding to Intended Behavior

Check the operation result only in situations where the call is allowed

- Applicable situations
	- When simple behavior verification is needed at the prototype stage

- Strengths and weaknesses
	- Low resource consumption and fast verification are possible
	- Exception situation verification is completely omitted, so reliability is low

Each test option has different strengths and weaknesses depending on the test scope and purpose, so developers need to choose an appropriate option for the project situation. Also, mixing two or more options as needed can be a desirable choice for improving efficiency.

>ex) Test based on option B, but verify behavior for Disposed once for each state

### 4-2. Test Methods

If tests are designed by applying test options based on the state-operation table, the system can be verified systematically and effectively. However, when time is extremely limited, when the system has two or more independent states, or when continuous function tests are needed, excessive time and resources may be consumed by tests.

This part introduces test design methods using state-operation tables and efficient test execution strategies for various systems, helping developers choose an appropriate testing method for the situation and use resources effectively.

#### 0. AI-Generated Tests

AI-generated tests are a method in which the implemented system code is given to generative AI and the AI is asked through a prompt to write test code, allowing the AI to independently generate test code with full authority over the tests.

This method has the following advantages.

- Basic behavior can be verified quickly for simple functions or in the early development stage.
- Tests are possible even when there is no state-operation table, so development time can be saved.

However, AI-generated tests also have the following limitations.

- Because the AI may not understand the developer's intent exactly, the tests may be inaccurate or incomplete.
- Reliability can be low for complex business logic or cases requiring systematic tests.

Therefore, AI-generated tests are appropriate as a temporary verification tool in situations where formal tests are difficult to perform, such as the early development stage.

#### 1. Single State-Based Tests

Single state-based tests are a test execution method for a system with one state type. Based on a state-based table, this method verifies whether objects in each state behave as intended for operations the object can perform.

The process of designing option A tests for a kickboard with one state type (the option that checks whether functions return the intended result in every possible state of the system) is as follows.

##### 1-1. State Verification

Before starting the tests, check whether the kickboard transitions to the intended states. This is necessary for designing tests that follow the single verification principle.

1. Create a kickboard and verify that it is in the Idie state.
2. Execute Mount on a kickboard in the Idle state and check whether the kickboard transitions to the Mount state.
3. Dispose a kickboard in the Idle state and check whether the kickboard transitions to the Disposed state.

The state verification process can also be integrated with the function verification process described later.
In this case, be careful that the test does not violate the single verification principle by changing state through an unverified function.

##### 1-2. Function Verification

Verify the functions written in the top row of the table in order.
The cases for verifying the Dismount function are as follows.

1. Verify Dismount in the Idle state
	- Arrange: create the kickboard
	- Act: execute the kickboard's Dismount method
	- Assert: verify that the kickboard is still in the Idle state

2. Verify Dismount in the Mounted state
	- Arrange: create the kickboard, then execute Mount to transition the kickboard to the Mounted state
			 (because the state verification test confirmed that the Mounted transition works normally, this operation does not violate the single verification principle)
	- Act: execute the kickboard's Dismount method
	- Assert: verify that the kickboard returned to the Idle state

3. Verify Dismount in the Disposed state
	- Arrange: create the kickboard, then execute Dispose to transition the kickboard to the Disposed state
	- Act: execute the kickboard's Dismount method
	- Assert: verify that the kickboard returns an ObjectDisposedException

##### 1-3. Other Verification

Verify special cases that were not tested.
For example, to verify the case where the same person boards again, perform Mount -> Dismount -> Mount and test whether the function works as intended.


Meanwhile, if the state-operation tables for a system with multiple independent states are combined into one table, tests can be designed relatively simply in the same way as a system with a single state.

However, this method makes it difficult to test various cases thoroughly, and as the number of states increases, combining the tables into one table becomes harder. Therefore, it can be appropriate when low-level verification or temporary verification is needed.

#### 2. Multiple Independent State-Based Tests

Multiple independent state-based tests are a test execution method for a system with two or more state types. They are performed by verifying the object's upper state types in order.

The process of designing tests for a kickboard with multiple independent state types is as follows.

##### 2-1. Base Function Tests

Run tests while assuming the kickboard is an object that implements only the base functions.

##### 2-2. Battery Function Tests

Use the kickboard's Battery state type table to run tests only for the related functions.
At this time, functions such as Checking that cannot be called directly are tested by using the Operations table or already verified upper functions.

##### 2-3. Other Tests

Verify scenarios among those using both types simultaneously that were not tested in the previous steps. For example, to verify boarding immediately after charging, perform Stop Charging -> Mount and test whether the function works as intended.

###### Notes

Standalone tests of upper functions can produce valid results only when lower functions are written in accordance with the LSP principle. Otherwise, some settings may need to be adjusted for valid tests.

#### 3. Continuous Tests

Continuous tests check whether a system functions as intended when its functions are executed continuously. Based on the object's state-operation table, they execute every operation that the initially given object can perform, then repeat the same work for each state that is generated afterward.

Because of this characteristic, continuous tests have the nature of integration tests that verify the combined effects between functions as a whole, rather than unit tests that verify each function type one by one.

The process of designing continuous tests for a kickboard with multiple independent state types is as follows.

##### 3-1. Create the Kickboard

Create the kickboard object to test. At this time, the kickboard is in the Idle state.

##### 3-2. Run the Test

Clone the kickboard, then execute in order the operations that the kickboard can perform from the Operations table for each kickboard.

##### 3-3. Check the Result

After checking whether each test behaved as expected, repeat item 2 for kickboards that are in a state where testing can continue.

##### Strengths and Limits of Continuous Tests

###### Strengths

Continuous tests can verify a system thoroughly in a very wide range of scenarios, securing high system reliability.

###### Limits

The number of cases to test increases, and as the number of continuous test iterations and system functions increases, the number of tests grows exponentially. This can create a major burden in test authoring and execution time.

##### Optimizing Continuous Tests

To overcome the limits of continuous tests, the following methods can be used.

- Test automation: automate tests to reduce authoring time, and use a multithreaded environment to reduce test execution time.
- Test prioritization: assign priorities according to test importance. For example, key scenarios can be tested first, while less important scenarios can be omitted or run fewer times.
- Omit extreme cases: exclude scenarios that are too extreme or cases that are extremely unlikely to occur from tests.

## 5. Closing

The unit testing guidelines introduced in this document aim to secure system stability and reliability through systematic unit tests based on state-operation tables, and to help developers design and execute tests effectively.

Test options, single-state and multi-state based tests, continuous tests, and other methodologies can be combined according to each project's situation and purpose. Through this, the time and resources spent on testing can be optimized while keeping the system in a reliable state.

Unit tests are not merely a tool for detecting errors. They check system reliability, verify interactions between complex functions, and provide a solid foundation for future expansion. Through these guidelines, the author expects developers to perform more systematic and effective tests.
