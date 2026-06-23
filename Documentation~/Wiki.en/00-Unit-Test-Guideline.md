# Overview

An effective unit testing methodology for ensuring system reliability.

---
## Table of Contents

1. Unit Test Overview

2. State - Operation Table
    1. Meaning of State and Operation
    2. System With One State Type
    3. System With Multiple Independent State Types

3. Test Principles

4. Test Methods
    1. AI Tests
    2. Unified State-Based Tests
    3. Multiple Independent State-Based Tests
    4. Continuous Tests

5. Closing

---

## 1. Unit Test Overview

Unit tests detect errors in advance at the module level, increase system stability, make debugging easier, and serve as a principle for future system expansion.

However, tests that are not systematic may fail to guarantee system reliability, while unit tests that are excessively meticulous may cause too much time and cost, reducing development efficiency.

UniTest is designed to solve these problems by clarifying the test range and procedure based on state-operation tables, allowing systematic and efficient unit tests to be performed. This document introduces the unit testing concepts and methodology that form the basis of that design.

| Category | Content |
| --- | --- |
| Overview | Introduces the unit testing concept assumed by UniTest |
| Purpose | Secures system reliability through systematic and efficient tests |
| Implementation | Test design based on state-operation tables |

---

## 2. State - Operation Table

To verify a system systematically, it is important to clearly identify which functions the system can perform in each state.

A state-operation table is a table that places the states a system can have in rows and the functions the system can perform in columns. This makes it possible to check every operation scenario of the system systematically.

### 2-1. Meaning of State and Operation

A state is a classification of conditions that determines how the current system will handle an operation.
- Examples: `Waiting`, `Running`, `Finished`, and so on

>Whether data is simply empty, a particular field is filled, or an internal collection has a certain number of values is not a state by itself. These values are usually handled as test inputs, preconditions, or boundary conditions.
>- Non-examples: `The list is empty`, `The name field is null`, `There are 3 logs`, and so on

An operation is a meaningful action that can be called or observed from outside the system.
- Examples: `Start`, `Stop`, `Save`, `Export`, and so on

>Simple helpers, internal implementation details, and methods that do not create different results depending on state are not placed in the state-operation table as operations.
>- Non-examples: `Format string`, `Sort intermediate collection`, and so on

### 2-2. System With One State Type

In a system with one state type, the states the system can have and the functions it can perform can be expressed in one table. For explanation, this document uses an electric kickboard that a rider can mount as an example.

The kickboard has the following three states.

- Idle: the default state
- Mounted: the rider is mounted
- Disposed: the object has been disposed

The kickboard can also operate or change its own state through the following functions.

- Mount: the rider mounts the kickboard (only licensed riders are allowed)
- Ride: the kickboard starts riding
- Stop: the kickboard stops
- Dismount: the rider ends the ride
- Dispose: the kickboard object is disposed

The state-operation table for this kickboard can be written as follows.

| State | - | Mount | < | < | < | Ride | Dismount | Dispose |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | - | Licensed | Same | Not Licensed | Null | | | |
| | - | | | | | | | |
| Idle | - | **Mounted** | X | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | - | **Disposed** |
| Mounted | - | _InvalidOperation_ | - | _InvalidOperation_ | ^ | `[Ride]` | **Idle** | `<Dismount>` -> `<Dispose>` |
| Disposed | - | _ObjectDisposed_ | X | _ObjectDisposed_ | ^ | _ObjectDisposed_ | - | - |

Table description

- State (left column): displays the states the system can have (Idle, Mounted, Disposed)
    - A column containing only `-` is placed between the state column and the content columns as a separator.
- Function (top row): displays the functions the kickboard can execute
    - One empty row is placed between the function row and the content row as a separator.
- Content
    - `<`: indicates that the item belongs to the same function group as the function on the left. In other words, it is a marker for reading the function column on the left continuously.
    - `^`: indicates that the operation or result in the upper cell is followed as-is. It reads the upper content instead of repeating the same content.
- Cell: displays the operation that occurs when a specific function is executed in a specific state
    - State change: **bold**
    - Unique function: `[bracketed inline block]`
    - Operation execution: `<arrow inline block>`
    - Exception occurrence: _italic_
    - No change: -
    - No state: X

Using a state-operation table in this way makes it possible to systematically check the system's possible operation scenarios.

### 2-3. System With Multiple Independent State Types

For a system with multiple independent state types, the number of states the system can have increases geometrically as the number of state types increases, so it can be difficult to express everything in one table. In this case, create a table for each state type, then create an integrated operation table for functions that can be called, and use that table to match the relevant state.

For explanation, assume that the two state types below are added to the previous kickboard.

- Battery: battery level state
    - Available: the battery is sufficient for normal riding
    - Discharged: the battery is insufficient for normal riding

- Charge State: charging progress state
    - Not Charging: not currently charging
    - Charging: the kickboard is charging

The kickboard can perform the following functions in each state type.

- Checking: check battery status while riding
- Charge: charge after stopping the kickboard
- Stop Charging: stop charging the kickboard
- Mount (function addition): allow Mount only when the battery is Available
- Ride (function addition): Ride is not allowed while Charge State is Charging; start Checking after Ride
- Stop (function addition): stop Checking before Stop
- Dispose (function addition): if Charge State is Charging, stop charging and then Dispose

At this point, the kickboard's full state-operation table can be written as follows.

#### Table Description

- Kickboard table: state-operation table for the system's base state type
- Battery table: state-operation table for the Battery state type
- Charge State table: state-operation table for the Charge State type
- Operations table: table for the functions the user can actually call

#### Function Override Mark

Columns marked with (override) represent overridden functions and control calls to higher-level functions.

For example, when the Battery state type is added to the kickboard, the lower-level function Battery.Mount controls the original higher-level Mount function so that the original Mount can run only when the battery is Available.

#### Operations Table

In a system with multiple independent state types, the user commonly controls several states at once through the completed interface. The Operations table makes it possible to intuitively check which operations affect each state.

The table is read as follows.

1. Check state-type operations from lower level to higher level.
2. If the operation is defined for the lower state type, apply it.
3. If the operation is not defined for the lower state type, apply the operation from the higher state type.

For example, in a kickboard where the additional state types are not implemented, every operation connects to the Kickboard row. By contrast, in a kickboard with Battery and Charge State state types added, operations are checked in the order Charge State, Battery, Kickboard, and only operations that were not redefined in a lower state type are checked in the higher state type.

#### Kickboard

| Kickboard | - | Mount | < | < | < | Ride | Dismount | Dispose |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | - | Licensed | Same | Not Licensed | Null | | | |
| | - | | | | | | | |
| Idle | - | **Mounted** | X | _InvalidOperation_ | `<Dismount>` | _InvalidOperation_ | - | **Disposed** |
| Mounted | - | _InvalidOperation_ | - | _InvalidOperation_ | ^ | `[Ride]` | **Idle** | `<Dismount>` -> `<Dispose>` |
| Disposed | - | _ObjectDisposed_ | X | _ObjectDisposed_ | ^ | _ObjectDisposed_ | - | - |

#### Battery

| Battery | - | Check | < | Mount (override) | Ride (override) |
| --- | --- | --- | --- | --- | --- |
| | - | Battery > 10% | Battery <= 10% | | |
| | - | | | | |
| Available | - | - | **Discharged** | `[base]` | `[base]` -> `[Use Battery]` |
| Discharged | - | **Available** | - | - | - |

#### Charge State

| Charge State | - | Charge | Do Charge | Stop Charging | Mount (override) | Ride (override) | Dispose (override) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| | - | | | | | | |
| Not Charging | - | `<Dismount>` -> **Charging** | X | - | `[base]` | `[base]` | `[base]` |
| Charging | - | - | `[Do Charge]` | **Not Charging** | `<Stop Charging>` -> `[base if available]` | _InvalidOperation_ | `<Stop Charging>` -> `[base]` |
| Disposed | - | _ObjectDisposed_ | X | - | `[base]` | _ObjectDisposed_ | - |

### Operations

| Operations | - | Mount | Ride | Dismount | Charge | Do Charge | Stop Charging | Dispose |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | - | | | | | | | |
| Kickboard | - | Mount | Ride | Dismount | X | X | X | Dispose |
| Battery | - | Mount | Ride | - | X | X | X | Dispose |
| Charge State | - | Mount | Ride | - | Charge | Do Charge | Stop Charging | Dispose |

---
## 3. Test Principles

When designing unit tests, the principles below must be followed.

### Single Verification Principle

A test must not verify two or more operations or state changes at the same time.

This principle verifies the operation of a specific function fully without interference, increasing the reliability of the test for that function and making debugging easier.

#### Poor Verification Example

A case where the Mount and Ride functions of the kickboard are performed consecutively and verified at the same time

>Check whether Kickboard.Ride() works successfully after calling Kickboard.Mount()

In this case, when the test fails, it is difficult to clearly determine which function caused the problem. Even if the test succeeds, it cannot guarantee that each function behaved as intended.

#### Correct Verification Example

1. Verify Mount first.

>After calling Kickboard.Mount(), check whether the state changed to Mounted

2. Verify Ride separately based on the verified Mount.

>ex) Check whether Ride() succeeds only when the state is Mounted

This makes it possible to identify the cause clearly when a test fails and to guarantee that a successful test means the relevant function behaved as intended.

---

## 4. Test Methods

Systematic unit tests play an important role in securing system reliability. At this point, the state-operation table introduced above can be used to verify the system systematically.

However, testing every case in a system requires a lot of time and resources. In particular, when the system is large or continuous testing is performed, tests can act as a factor that interferes with development.

Therefore, when designing tests, it is important to choose an appropriate test method according to the situation and purpose. This part introduces several test execution options and methods so that developers can choose a suitable test method depending on the situation.

### 4-1. Test Options

Many system functions are designed under the assumption of intended situations, and in unintended situations they are designed to ignore input or throw exceptions.

These situations are usually unlikely to occur by design, so when time is limited, a strategy can be chosen that skips tests for unintended situations.

However, this can create a risk of not verifying how the system behaves in extreme situations. In particular, deployed code should, in principle, perform tests for every situation to guarantee stability.

The following table shows examples of applying each test option to three test situations.

| Situation | A | B | C |
| --- | --- | --- | --- |
| Check whether charging proceeds when Charge is called while the kickboard is Discharged | O | O | O |
| Check whether the call is ignored when Charge is called while the kickboard is Charging | O | O | X |
| Check whether an error is returned when Charge is called while the kickboard is Disposed | O | X | X |

#### A. Test Every Case

Check whether the function returns the intended result in every possible state of the system.

- Applicable situations
    - When thorough system stability is required, such as immediately before deployment
    - When operation verification in extreme situations is important (for example, a linear algebra processing system)

- Pros and cons
    - Thorough verification is possible
    - Consumes a lot of resources and time

#### B. Test Only Non-Exceptional Cases

Check whether normal operation is performed in situations where the call is allowed.

- Applicable situations
    - When the main behavior of a function must be verified quickly in the early stage of development
    - When time and resources are limited

- Pros and cons
    - Efficient and fast verification is possible
    - It is difficult to discover errors in advance because exceptional situations are not sufficiently verified

#### C. Test Only Intended Behavior

Check the operation result only in situations where the call is allowed.

- Applicable situations
    - When simple behavior verification is needed in the prototype stage

- Pros and cons
    - Consumes few resources and allows fast verification
    - Reliability is low because exceptional situation verification is completely omitted

Each test option has different pros and cons depending on the test range and purpose, so developers need to choose an appropriate option according to the project situation. Also, mixing two or more options as needed can be a desirable choice for improving efficiency.

>ex) Test based on option B, but verify the behavior for Disposed once for each state

### 4-2. Test Styles

If tests are designed by applying test options based on the state-operation table, the system can be verified systematically and effectively. However, when time is extremely limited, when a system has two or more independent states, or when continuous function testing is required, tests can consume too much time and resources.

This part introduces test design methods using state-operation tables and efficient test execution strategies for various systems, helping developers choose an appropriate test method for the situation and use resources effectively.

#### 0. AI-Generated Tests

AI-generated testing is a method where the code of the implemented system is given to generative AI, and the AI is asked through a prompt to write test code so that the AI independently generates test code with full authority over the tests.

This method has the following advantages.

- It can quickly verify basic behavior for simple functions or in the early stage of development.
- Because tests can be created even without a state-operation table, development time can be saved.

However, AI-generated testing also has the following limitations.

- Because the AI may not understand the developer's intent accurately, tests can be inaccurate or incomplete.
- Reliability may be low for complex business logic or when systematic tests are required.

Therefore, AI-generated testing is appropriate as a temporary verification tool in situations where formal tests are difficult to perform, such as the early stage of development.

#### 1. Single-State-Based Testing

Single-state-based testing is a test execution method for a system with one state type. Based on the state-based table, it checks whether objects belonging to each state operate as intended for the operations the object can perform.

The process for designing option A tests (the option that checks whether functions return intended results in every possible state of the system) for a kickboard with one state type is as follows.

##### 1-1. State Verification

Before starting the tests, verify that the kickboard transitions to the intended states. This is necessary for test design that follows the Single Verification Principle.

1. Create a kickboard and verify that it is in the Idle state.
2. Execute the Mount function on the kickboard in the Idle state and check whether it transitions to the Mounted state.
3. Dispose the kickboard in the Idle state and check whether it transitions to the Disposed state.

The state verification process can also be integrated with the function verification process described later.
In that case, be careful that the test does not violate the Single Verification Principle by changing state through an unverified function.

##### 1-2. Function Verification

Verify the functions written in the top row of the table in order.
The cases for verifying the Dismount function are as follows.

1. Dismount verification in the Idle state
    - Arrange: create the kickboard
    - Act: execute the kickboard's Dismount method
    - Assert: verify that the kickboard is still in the Idle state

2. Dismount verification in the Mounted state
    - Arrange: create the kickboard and execute the Mount method to transition it to the Mounted state
      (Because the state verification test confirmed that the Mounted transition works normally, this operation does not violate the Single Verification Principle.)
    - Act: execute the kickboard's Dismount method
    - Assert: verify that the kickboard returned to the Idle state

3. Dismount verification in the Disposed state
    - Arrange: create the kickboard and execute the Dispose method to transition it to the Disposed state
    - Act: execute the kickboard's Dismount method
    - Assert: verify that the kickboard returns ObjectDisposedException

##### 1-3. Other Verification

Verify special cases that were not tested.
For example, when verifying the case where the same person mounts again, perform Mount -> Dismount -> Mount and test whether the function behaves as intended.

Meanwhile, if a system with multiple independent states is combined into one state-operation table, a test can be designed relatively simply in the same way as a system with one state type.

However, this method makes it difficult to test various cases thoroughly, and as the number of states increases, combining the tables into one becomes difficult. Therefore, it can be suitable when lower-level verification or temporary verification is needed.

#### 2. Multiple Independent State-Based Testing

Multiple independent state-based testing is a test execution method for a system with two or more state types. It is performed by verifying the object's higher-level state types in order.

The process for designing tests for a kickboard with multiple independent state types is as follows.

##### 2-1. Basic Function Test

Proceed with testing by assuming the kickboard is an object that implements only the basic functions.

##### 2-2. Battery Function Test

Use the Battery state type table of the kickboard to test only the related functions.
At this time, functions that cannot be called directly, such as Checking, are tested through methods such as using the Operations table or already verified higher-level functions.

##### 2-3. Other Tests

Verify scenarios among those using both types at the same time that were not tested in earlier stages. For example, when verifying mounting immediately after charging, perform Stop Charging -> Mount and test whether the function behaves as intended.

###### Notes

Independent tests of higher-level functions produce valid results only when lower-level functions are written in compliance with the LSP principle. Otherwise, some settings may need to be adjusted to make the tests valid.

#### 3. Continuous Testing

Continuous testing checks whether the system functions as intended when system functions are executed continuously. Based on the object's state-operation table, it executes all operations that the initially given object can perform, then repeats the same work for each subsequently created state.

Because of this characteristic, continuous testing has the nature of integration testing, verifying the combined interactions among functions as a whole, rather than unit testing that verifies each function type one by one.

The process for designing continuous tests for a kickboard with multiple independent state types is as follows.

##### 3-1. Create Kickboard

Create the kickboard object to be tested. At this time, the kickboard is in the Idle state.

##### 3-2. Proceed With Testing

After cloning the kickboard, execute the operations the kickboard can perform from the Operations table in order for each kickboard.

##### 3-3. Check Results

After checking whether the kickboard behaved as expected in each test, repeat item 2 for kickboards whose state allows testing to continue.

##### Advantages and Limitations of Continuous Testing

###### Advantages

Continuous testing can thoroughly verify the system across a very wide variety of scenarios, securing high system reliability.

###### Limitations

The number of test cases grows, and as the number of continuous test iterations and system functions increases, the number of tests increases geometrically. This can create a large burden on test writing and execution time.

##### Optimizing Continuous Testing

The following methods can be used to overcome the limitations of continuous testing.

- Test automation: automate tests to reduce writing time, and use a multithreaded environment to reduce test execution time.
- Test prioritization: assign priorities according to test importance. For example, key scenarios can be tested first, and less important scenarios can be skipped or run fewer times.
- Omit extreme cases: scenarios that are too extreme or cases that are extremely unlikely to occur are excluded from tests.

---

## 5. Closing

The unit test guideline introduced in this document aims to secure system stability and reliability through systematic unit tests based on state-operation tables, and to help developers design and perform tests effectively.

The various methodologies, such as test options, single-state and multiple-state-based tests, and continuous tests, can be combined according to each project's situation and purpose. Through this, the time and resources spent on testing can be optimized while keeping the system in a reliable state.

Unit testing is not merely a tool for detecting errors. It checks system reliability, verifies interactions among complex functions, and provides a solid foundation for future expansion. Through this guideline, the author expects developers to perform more systematic and effective tests.
