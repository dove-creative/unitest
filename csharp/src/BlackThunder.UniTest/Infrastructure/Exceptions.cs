using System;

namespace BlackThunder.UniTest
{
    /// <summary>
    /// Thrown when an execution fails unexpectedly during a test run.
    /// </summary>
    public class ExecutionException : Exception
    {
        public ExecutionException() : this(string.Empty, null) { }

        public ExecutionException(string message) : this(message, null) { }

        public ExecutionException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Thrown when an expected exception was not thrown during a test execution.
    /// </summary>
    public class MissingExpectedException : Exception
    {
        public Type ExpectedExceptionType { get; }

        public MissingExpectedException(Type expectedExceptionType)
            : this(string.Empty, expectedExceptionType, null) { }

        public MissingExpectedException(string message, Type expectedExceptionType)
            : this(message, expectedExceptionType, null) { }

        public MissingExpectedException(string message, Type expectedExceptionType, Exception innerException)
            : base(message, innerException) => ExpectedExceptionType = expectedExceptionType;
    }

    /// <summary>
    /// Thrown when a test case collection is empty or not properly populated.
    /// </summary>
    public class TestCaseAbsentException : ArgumentException
    {
        public TestCaseAbsentException() : this(null, null) { }

        public TestCaseAbsentException(string paramName) : this(paramName, null) { }

        public TestCaseAbsentException(string paramName, Exception innerException)
            : base("Cannot proceed: the required test state is absent.", paramName, innerException) { }
    }

    /// <summary>
    /// Thrown when an attempt is made to execute a test case that has not been defined.
    /// </summary>
    public class UndefinedTestCaseException : InvalidOperationException
    {
        public UndefinedTestCaseException() : base("Cannot execute undefined test case.") { }

        public UndefinedTestCaseException(object testCase) : this(testCase, null) { }

        public UndefinedTestCaseException(object testCase, Exception innerException)
            : base($"Cannot execute undefined test case '{testCase}'.", innerException) { }
    }

    /// <summary>
    /// Thrown when a test case is executed in an invalid or incompatible model state.
    /// </summary>
    public class InvalidTestException : InvalidOperationException
    {
        public InvalidTestException() : base("Cannot execute test due to invalid case or state.") { }

        public InvalidTestException(object currentCase, object currentModelState)
            : this(currentCase, currentModelState, null) { }

        public InvalidTestException(object currentCase, object currentModelState, Exception innerException)
            : base($"Cannot execute test case '{currentCase}' in state '{currentModelState}'.", innerException) { }
    }
}
