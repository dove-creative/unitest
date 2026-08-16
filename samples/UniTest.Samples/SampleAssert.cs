using System;
using System.Collections.Generic;

namespace UniTest.Samples.NativeCSharp
{
    internal static class SampleAssert
    {
        public static void IsNotNull(object value, string message = null)
        {
            if (value == null)
                throw new InvalidOperationException(message ?? "Expected value to be non-null.");
        }

        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message ?? $"Expected '{expected}', but was '{actual}'.");
        }

        public static void AreSame(object expected, object actual, string message = null)
        {
            if (!ReferenceEquals(expected, actual))
                throw new InvalidOperationException(message ?? "Expected both references to be the same instance.");
        }

        public static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Expected exception '{typeof(TException).Name}', but was '{exception.GetType().Name}'.",
                    exception);
            }

            throw new InvalidOperationException($"Expected exception '{typeof(TException).Name}', but no exception was thrown.");
        }

        public static void DoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Expected no exception, but was '{exception.GetType().Name}'.",
                    exception);
            }
        }
    }
}
