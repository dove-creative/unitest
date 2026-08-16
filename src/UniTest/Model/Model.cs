using System;
using System.Text;
using System.Collections.Generic;

namespace UniTest
{
    public class Model
    {
        // Front
        public object Subject { get; set; }
        public Dictionary<object, SubjectMetadata> MetadataGroup { get; } = new();

        // Metadata
        public bool Sustainable
        {
            get => _sustainable;
            set
            {
                if (value)
                    throw new InvalidOperationException("Setting the model to sustainable is not allowed.");

                _sustainable = false;
            }
        } bool _sustainable = true;

        public bool Continuable
        {
            get => _continuable;
            set
            {
                if (value)
                    throw new InvalidOperationException("Setting the model to continuable is not allowed.");

                Sustainable = false;
                _continuable = false;
            }
        } bool _continuable = true;

        public int? RemainingExecutionCount
        {
            get => _remainingExecutionCount;
            set
            {
                if (!value.HasValue)
                    throw new InvalidOperationException("Unsetting the remaining execution count is not allowed.");

                _remainingExecutionCount = Math.Min(value.Value, _remainingExecutionCount ?? int.MaxValue);

                Sustainable = false;

                if (_remainingExecutionCount <= 0)
                    Continuable = false;
            }
        } int? _remainingExecutionCount = null;

        public int ExecutionCount => _executedLabs.Count;
        List<string> _executedLabs = new();
        public const string Separator = "/";


        // Content
        internal void DoExecute(string labID)
        {
            if (!_continuable)
            { 
                throw new InvalidOperationException($"The model can no longer be tested.\n" +
                    $"Lab ID: {labID}\n" +
                    $"Model: {ToString()}\n" +
                    $"History: {GetExecutionHistory()}");
            }

            if (_remainingExecutionCount.HasValue)
                RemainingExecutionCount--;

            _executedLabs.Add(labID);
        }

        public string GetExecutionHistory() => string.Join(Separator, _executedLabs);

        /// <summary>
        /// Returns a deterministic random integer in the range [0, <paramref name="maxExclusive"/>), 
        /// calculated based on executed lab IDs.
        /// </summary>
        /// <remarks>
        /// Given the same inputs, the method will always return the same output.
        /// </remarks>
        public int GetDeterministicRandom(int maxExclusive, int seed = 0) => GetDeterministicRandom(0, maxExclusive, seed);
        /// <summary>
        /// Returns a deterministic random integer in the range [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>), 
        /// calculated based on executed lab IDs.
        /// </summary>
        /// <remarks>
        /// Given the same inputs, the method will always return the same output.
        /// </remarks>
        public int GetDeterministicRandom(int minInclusive, int maxExclusive, int seed = 0)
        {
            return GetExecutionHistory()
              .GetDeterministicRandom(minInclusive, maxExclusive, seed);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"Subject : {Subject?.ToString() ?? "Null"}, ");
            sb.Append($"ExecutionCount : {ExecutionCount}, ");
            sb.Append($"Continuable : {Continuable}, ");
            sb.Append($"Sustainable : {Sustainable}, ");
            sb.Append($"RemainingExecutionCount : {RemainingExecutionCount?.ToString() ?? "Unlimited"}");

            return sb.ToString().TrimEnd('\r', '\n');
        }
    }
}
