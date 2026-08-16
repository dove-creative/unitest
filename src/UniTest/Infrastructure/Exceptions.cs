using System;

namespace UniTest
{
    /// <summary>
    /// Thrown when an execution fails unexpectedly during a test run.
    /// </summary>
    public class ExecutionException : Exception
    {
        public ExecutionException()
            : this(string.Empty, null) { }

        public ExecutionException(string message)
            : this(message, null) { }

        public ExecutionException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    /// <summary>
    /// Thrown when an expected exception was not thrown during a test execution.
    /// </summary>
    public class MissingExpectedException : Exception
    {
        public Type ExpectedExceptionType { get; }

        public MissingExpectedException(Type expectedExecptionType)
            : this(string.Empty, expectedExecptionType, null) { }

        public MissingExpectedException(string message, Type expectedExecptionType)
            : this(message, expectedExecptionType, null) { }

        public MissingExpectedException(string message, Type expectedExecptionType, Exception innerException)
            : base(message, innerException) => ExpectedExceptionType = expectedExecptionType;
    }

    /// <summary>
    /// Thrown when a test case collection is empty or not properly populated.
    /// </summary>
    public class TestCaseAbsentException : ArgumentException
    {
        public TestCaseAbsentException()
            : this(null, null) { }

        public TestCaseAbsentException(string paramName)
            : this(paramName, null) { }

        public TestCaseAbsentException(string paramName, Exception innerException)
            : base("Cannot proceed: the required test state is absent.", paramName, innerException) { }
    }

    /// <summary>
    /// Thrown when an attempt is made to execute a test case that has not been defined.
    /// </summary>
    public class UndefinedTestCaseException : InvalidOperationException
    {
        public UndefinedTestCaseException()
            : base("Cannot execute undefined test case.") { }

        public UndefinedTestCaseException(object testCase)
            : this(testCase, null) { }

        public UndefinedTestCaseException(object testCase, Exception innerException)
            : base($"Cannot execute undefined test case '{testCase}'.", innerException) { }
    }

    /// <summary>
    /// Thrown when a test case is executed in an invalid or incompatible model state.
    /// </summary>
    public class InvalidTestException : InvalidOperationException
    {
        public InvalidTestException()
            : base("Cannot execute test due to invalid case or state.") { }

        public InvalidTestException(object currentCase, object currentModelState)
            : this(currentCase, currentModelState, null) { }

        public InvalidTestException(object currentCase, object currentModelState, Exception innerException)
            : base($"Cannot execute test case '{currentCase}' in state '{currentModelState}'.", innerException) { }
    }

    /// <summary>
    /// Exception class for exception check
    /// </summary>
    public class ProbeException : Exception
    {
        public ProbeException(string message) : base(message) { }
        public ProbeException() : this("Probe Exception") { }
    }
}
