using System;
using System.IO;

namespace UniTest.Samples.NativeCSharp
{
    internal static class SampleEnvironment
    {
        public const int ProcessLimit = -1;
        public const int TimeLimit = -1;

        public const int SingleStateDepth = 5;
        public const int MultiStateDepth = 5;
        public const int SingleStateContinuousDepth = 20;
        public const int MultiStateContinuousDepth = 50;

        public const string SingleStateReportName = "SingleState";
        public const string SingleStateReplayReportName = "SingleStateReplay";
        public const string MultiStateReportName = "MultiState";
        public const string SingleStateReplayIDs = "Ignite/Mount_Null/Mount_Targeted";

        public static readonly string ReportPath
            = Path.Combine(AppContext.BaseDirectory, "UniTest", "Samples", "NativeCSharp");

        public static void Configure()
        {
            Directory.CreateDirectory(ReportPath);

            Logger.Configure(
                Console.WriteLine,
                Console.WriteLine,
                Console.Error.WriteLine,
                exception => Console.Error.WriteLine(exception));
        }
    }
}
