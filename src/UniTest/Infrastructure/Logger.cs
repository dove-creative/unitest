using System;

namespace UniTest
{
    public static class Logger
    {
        public static Action<string> NormalLogger;
        public static Action<string> WarningLogger;
        public static Action<string> ErrorLogger;
        public static Action<Exception> ExceptionLogger;

        static Logger() => ResetDefault();

        public static void ResetDefault()
        {
            NormalLogger = Console.WriteLine;
            WarningLogger = Console.WriteLine;
            ErrorLogger = Console.Error.WriteLine;
            ExceptionLogger = Console.Error.WriteLine;
        }

        public static void Configure(
            Action<string> normalLogger,
            Action<string> warningLogger,
            Action<string> errorLogger,
            Action<Exception> exceptionLogger)
        {
            NormalLogger = normalLogger;
            WarningLogger = warningLogger;
            ErrorLogger = errorLogger;
            ExceptionLogger = exceptionLogger;
        }

        internal static void Log(string message) => NormalLogger?.Invoke(message);
        internal static void LogWarning(string message) => WarningLogger?.Invoke(message);
        internal static void LogError(string message) => ErrorLogger?.Invoke(message);
        internal static void LogException(Exception ex) => ExceptionLogger?.Invoke(ex);
    }
}
