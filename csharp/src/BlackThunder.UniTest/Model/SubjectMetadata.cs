using System;

namespace BlackThunder.UniTest
{
    public class SubjectMetadata : IReadOnlySubjectMetadata
    {
        // Front
        public object Metadata { get; set; }
        public Type ExpectedExceptionType { get; set; }

        public bool ToUnsustainable { get; set; }
        /// <summary>Stops subsequent Labs after the current Lab's assertions complete.</summary>
        public bool ToUncontinuable { get; set; }
        public int RemainingExecutionCount { get; set; } = -1;


        // Content
        public SubjectMetadata() { }
        public SubjectMetadata(object metadata) => Metadata = metadata;

        public T GetMetadata<T>()
        {
            if (Metadata == null)
                return default;

            if (Metadata is not T)
            {
                throw new InvalidOperationException(
                    $"Metadata type mismatch: expected '{typeof(T).Name}', but found '{Metadata?.GetType().Name ?? "null"}'.");
            }

            return (T)Metadata;
        }

        public SubjectMetadata Copy() => new()
        {
            ExpectedExceptionType = ExpectedExceptionType,
            ToUnsustainable = ToUnsustainable,
            ToUncontinuable = ToUncontinuable,
            RemainingExecutionCount = RemainingExecutionCount
        };

        public void Merge(IReadOnlySubjectMetadata template)
        {
            ExpectedExceptionType ??= template.ExpectedExceptionType;
            ToUnsustainable = ToUnsustainable || template.ToUnsustainable;
            ToUncontinuable = ToUncontinuable || template.ToUncontinuable;

            if (RemainingExecutionCount < 0)
                RemainingExecutionCount = template.RemainingExecutionCount;
            else if (template.RemainingExecutionCount >= 0)
                RemainingExecutionCount = Math.Min(RemainingExecutionCount, template.RemainingExecutionCount);
        }
    }
}
