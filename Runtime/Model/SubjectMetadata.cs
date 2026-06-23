using System;

namespace UniTest
{
    public class SubjectMetadata
    {
        // Front
        public object Metadata = null;
        public Type ExpectedExceptionType = null;

        public bool ToUnsustainable = false;
        public bool ToUncontinuable = false;
        public int RemainingExecutionCount = -1;


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

        public void Merge(SubjectMetadata template)
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
