using System;

namespace BlackThunder.UniTest
{
    /// <summary>
    /// Read-only view of a Lab's metadata template. Copy it to get the metadata an execution can change.
    /// </summary>
    public interface IReadOnlySubjectMetadata
    {
        object Metadata { get; }
        Type ExpectedExceptionType { get; }

        bool ToUnsustainable { get; }
        bool ToUncontinuable { get; }
        int RemainingExecutionCount { get; }

        SubjectMetadata Copy();
    }
}
