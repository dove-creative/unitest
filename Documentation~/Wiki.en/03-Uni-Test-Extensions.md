# Table of Contents

1. Failure Stop/Re-Execute
2. Node Re-Execution and Debugging
3. Continuous Testing

---

## 1. Failure Stop/Re-Execute

Because UniTest executes every possible test case, the total execution time can grow as the system becomes larger. When a failure occurs during testing, the developer may want to fix the code immediately and then re-run only the failed tests to confirm the fix. To support this, UniTest provides the two features below: Failure Stop and Re-Execute. These two features can be used together. A failure list obtained through Failure Stop can be passed directly into Re-Execute to perform a fast verification cycle.

1. **Failure Stop**: if any test fails, immediately stop executing the remaining tests and return the list of failed tests.
2. **Re-Execute**: re-run only the specified sequence of tests.

The two features are implemented as follows.

1. **Failure Stop**: when the Test Executor detects an exception or assertion failure, it sets the Project flag to `true`. The Project checks that flag in each execution loop and immediately stops the entire execution when a change is detected.
2. **Re-Execute**: build a filter list from the input sequence of test IDs. Then create a new Model instance, delegate test generation to the Test Designer, and re-run the tests by leaving only the generated tests that match the filter list in the execution queue.

---

## 2. Node Re-Execution and Debugging

When a test fails, the developer can identify the main cause through UniTest's XML report. However, in some cases, the report alone is not enough to inspect detailed execution flow or state information. In that situation, the developer can select the desired node from the Node list returned by UniTest and re-run it in the IDE, using breakpoints, variable watches, stack traces, and similar debugging tools to analyze the problem point closely.

The node re-execution procedure is as follows.

1. Call `node.DetachAndRestore()` to create an isolated debug Node without changing the original Node graph.
2. Call `restored.Execute()` on the returned Node to re-run the test after reproducing the same execution history.

This process reproduces the failure situation and allows the cause to be verified in depth at the code level.

---

## 3. Continuous Testing

Because UniTest executes every possible test case, the total execution time grows at roughly the $O(n^2)$ level as the number of test steps increases. As the number of test steps grows, this creates a problem where execution cost rises sharply.

To compensate for this, UniTest provides an option that randomly selects and executes one continuable path from the successful tests instead of executing every possible path. Using this option reduces the total cost to roughly the $O(n)$ level, making it effective in situations where many tests are needed to verify object stability.
