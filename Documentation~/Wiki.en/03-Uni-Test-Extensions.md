# Table of Contents

1. Failure Stop/Re-Execute
2. Node Re-Execution and Debugging
3. Continuous Tests

## 1. Failure Stop/Re-Execute

Because Uni Test executes every possible test case, the total execution time can become longer as the system grows. If a failure occurs during testing, the developer may want to immediately fix the code and then re-run only the failed tests to check the result of the fix. To support this, Uni Test provides the following two functions: Failure Stop and Re-Execute. The two functions can be used together, and the failure list obtained from Failure Stop can be passed directly as Re-Execute input for a fast verification cycle.

1. **Failure Stop**: if any test fails, immediately stop executing the remaining tests and return the list of failed tests.
2. **Re-Execute**: re-run only the specified set of tests.

The two functions are implemented as follows.

1. **Failure Stop**: when Test Executor detects an exception, Assert failure, or similar problem, it sets the Project flag to `true`. Project checks that flag in each execution loop and immediately stops the full execution when it detects a change.
2. **Re-Execute**: build a filtering list based on the input sequence of test IDs. After that, create a new Model instance, delegate test generation to TestDesigner, and re-run tests by leaving only the tests that match the filter list in the execution queue.

## 2. Node Re-Execution and Debugging

When a test fails, the developer can check the main cause through Uni Test's XML report. However, in some cases, the report alone may not be enough to inspect detailed execution flow or state information. In that case, the developer can select the desired node from the Node list returned by Uni Test and re-run it in the IDE, using breakpoints, variable watches, stack traces, and similar tools to analyze the problem point closely.

The node re-execution procedure is as follows.

1. Call `node.DetachAndRestore()` to create a separated debug Node that does not change the original Node graph.
2. Call `restored.Execute()` on the returned Node to re-run the test after reproducing the same execution history.

Through this process, the failure situation can be reproduced and the cause can be verified deeply at the code level.

## 3. Continuous Tests

Because Uni Test executes every possible test case, the total execution time increases at an $O(n^2)$ level as the number of test steps grows. This causes execution cost to rise sharply when many test steps exist.

To compensate for this, Uni Test provides an option that does not execute every possible path, but instead arbitrarily selects and executes one continuable path among successful tests. Using this option can reduce the total time to an $O(n)$ level, making it possible to respond effectively to situations where object stability must be verified through many tests.
