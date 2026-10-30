using System;
using System.IO;

namespace BlackThunder.UniTest.Samples
{
    internal static class SampleEnvironment
    {
        // Front
        public const int SingleStateDepth = 5;
        public const int MultiStateDepth = 5;
        public const int SingleStateContinuousDepth = 20;
        public const int MultiStateContinuousDepth = 50;

        public const string SingleStateReplayIds = "Ignite/Mount_Null/Mount_Targeted";

        public static readonly string ReportPath
            = Path.Combine(AppContext.BaseDirectory, "SampleOutput");


        // Content
        public static void Configure()
        {
            Directory.CreateDirectory(ReportPath);

            Logger.Configure(
                _ => { },
                Console.Error.WriteLine,
                Console.Error.WriteLine,
                exception => Console.Error.WriteLine(exception));
        }
    }
}
