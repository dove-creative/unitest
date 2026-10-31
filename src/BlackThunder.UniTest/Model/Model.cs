using System;
using System.Collections.Generic;
using System.Text;

namespace BlackThunder.UniTest
{
    public class Model
    {
        // Front
        public object Subject { get; set; }
        public Dictionary<object, SubjectMetadata> MetadataGroup { get; } = new();

        public bool Sustainable
        {
            get => _sustainable;
            set
            {
                if (value)
                    throw new InvalidOperationException("Setting the model to sustainable is not allowed.");

                _sustainable = false;
            }
        }

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
        }

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
        }

        public int ExecutionCount => _executedLabs.Count;
        public const string Separator = "/";

        // Internal
        private bool _sustainable = true;
        private bool _continuable = true;
        private int? _remainingExecutionCount = null;
        private readonly List<string> _executedLabs = new();


        // Content
        internal void DoExecute(string labId)
        {
            if (!_continuable)
            {
                throw new InvalidOperationException($"The model can no longer be tested.\n" +
                    $"Lab ID: {labId}\n" +
                    $"Model: {ToString()}\n" +
                    $"History: {GetExecutionHistory()}");
            }

            if (_remainingExecutionCount.HasValue)
                RemainingExecutionCount--;

            _executedLabs.Add(labId);
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
