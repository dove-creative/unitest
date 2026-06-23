using System;
using System.Threading.Tasks;
using MultiProject = UniTest_Test.MultiState.Project;
using SingleProject = UniTest_Test.SingleState.Project;

namespace UniTest.Samples.NativeCSharp
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            SampleEnvironment.Configure();
            PrintUsage();

            if (args.Length > 0)
                await RunCommand(args[0]);

            while (true)
            {
                Console.Write("sample> ");
                var input = Console.ReadLine();
                if (input == null)
                    return 0;

                var command = input.Trim();
                if (command.Length == 0)
                    continue;

                if (string.Equals(command, "exit", StringComparison.OrdinalIgnoreCase))
                    return 0;

                await RunCommand(command);
            }
        }

        private static async Task RunCommand(string input)
        {
            var command = input.Trim().ToLowerInvariant();
            if (command.Length == 0)
                return;

            try
            {
                switch (command)
                {
                    case "single":
                        await RunSingleStateAsync();
                        break;

                    case "single-replay":
                        await RunSingleStateReplayAsync();
                        break;

                    case "multi":
                        await RunMultiStateAsync();
                        break;

                    case "single-continuous":
                        await RunSingleStateContinuouslyAsync();
                        break;

                    case "multi-continuous":
                        await RunMultiStateContinuouslyAsync();
                        break;

                    case "help":
                        PrintUsage();
                        return;

                    default:
                        Console.WriteLine($"[UniTest NativeCSharp Sample] Unknown command: {input}");
                        PrintUsage();
                        return;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"[UniTest NativeCSharp Sample] Unexpected error: {exception}");
            }

            Console.WriteLine($"[UniTest NativeCSharp Sample] Reports: {SampleEnvironment.ReportPath}");
        }

        private static Task RunSingleStateAsync()
        {
            return new SingleProject()
                .Run(
                    SampleEnvironment.ReportPath,
                    SampleEnvironment.SingleStateReportName,
                    SampleEnvironment.SingleStateDepth,
                    processLimit: SampleEnvironment.ProcessLimit,
                    timeLimit: SampleEnvironment.TimeLimit,
                    printResult: true);
        }

        private static Task RunSingleStateReplayAsync()
        {
            return new SingleProject()
                .Run(
                    SampleEnvironment.ReportPath,
                    SampleEnvironment.SingleStateReplayReportName,
                    SampleEnvironment.SingleStateReplayIDs,
                    timeLimit: SampleEnvironment.TimeLimit,
                    printResult: true);
        }

        private static Task RunMultiStateAsync()
        {
            return new MultiProject()
                .Run(
                    SampleEnvironment.ReportPath,
                    SampleEnvironment.MultiStateReportName,
                    SampleEnvironment.MultiStateDepth,
                    processLimit: SampleEnvironment.ProcessLimit,
                    timeLimit: SampleEnvironment.TimeLimit,
                    printResult: true);
        }

        private static Task RunSingleStateContinuouslyAsync()
        {
            return new SingleProject()
                .RunContinuously(
                    SampleEnvironment.ReportPath,
                    SampleEnvironment.SingleStateReportName,
                    SampleEnvironment.SingleStateContinuousDepth,
                    processLimit: SampleEnvironment.ProcessLimit,
                    timeLimit: SampleEnvironment.TimeLimit,
                    printResult: true);
        }

        private static Task RunMultiStateContinuouslyAsync()
        {
            return new MultiProject()
                .RunContinuously(
                    SampleEnvironment.ReportPath,
                    SampleEnvironment.MultiStateReportName,
                    SampleEnvironment.MultiStateContinuousDepth,
                    processLimit: SampleEnvironment.ProcessLimit,
                    timeLimit: SampleEnvironment.TimeLimit,
                    printResult: true);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  single");
            Console.WriteLine("  single-replay");
            Console.WriteLine("  multi");
            Console.WriteLine("  single-continuous");
            Console.WriteLine("  multi-continuous");
            Console.WriteLine("  help");
            Console.WriteLine("  exit");
        }
    }
}
