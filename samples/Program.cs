using System;
using System.Threading.Tasks;
using MultiProject = BlackThunder.UniTest.Samples.Scenarios.MultiState.Project;
using SingleProject = BlackThunder.UniTest.Samples.Scenarios.SingleState.Project;

namespace BlackThunder.UniTest.Samples
{
    public static class Program
    {
        // Content --------------------
        // Entry point
        public static async Task<int> Main(string[] args)
        {
            if (args.Length > 0)
                return await RunCommandAsync(args);

            PrintUsage();
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

                await RunCommandAsync(command.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }
        }

        // Command routing
        private static async Task<int> RunCommandAsync(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Commands do not accept additional arguments.");
                PrintUsage();
                return 1;
            }

            try
            {
                switch (args[0].Trim().ToLowerInvariant())
                {
                    case "single":
                        return await RunScenarioAsync(
                            () => new SingleProject().Execute(SampleEnvironment.SingleStateDepth), "SingleState");
                    case "single-replay":
                        return await RunScenarioAsync(
                            () => new SingleProject().Execute(SampleEnvironment.SingleStateReplayIds), "SingleStateReplay");
                    case "multi":
                        return await RunScenarioAsync(
                            () => new MultiProject().Execute(SampleEnvironment.MultiStateDepth), "MultiState");
                    case "single-continuous":
                        return await RunScenarioAsync(
                            () => new SingleProject().ExecuteContinuously(SampleEnvironment.SingleStateContinuousDepth), "SingleStateContinuous");
                    case "multi-continuous":
                        return await RunScenarioAsync(
                            () => new MultiProject().ExecuteContinuously(SampleEnvironment.MultiStateContinuousDepth), "MultiStateContinuous");
                    case "help":
                        PrintUsage();
                        return 0;
                    case "exit":
                        return 0;
                    default:
                        Console.Error.WriteLine($"Unknown command: {args[0]}");
                        PrintUsage();
                        return 1;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Unexpected error: {exception}");
                return 1;
            }
        }

        private static async Task<int> RunScenarioAsync<TModel>(Func<Task<Node<TModel>>> execute, string reportName)
            where TModel : Model, new()
        {
            SampleEnvironment.Configure();
            var root = await execute();
            var succeeded = root.AllSucceed(out var cancelled) && !cancelled;
            var report = succeeded ? root.GetReport() : root.GetFailedReports();
            Tools.ExportJson(report, reportName, SampleEnvironment.ReportPath);

            Console.WriteLine($"{reportName}: {(succeeded ? "Succeeded" : "Failed or cancelled")}");
            Console.WriteLine($"Report: {System.IO.Path.Combine(SampleEnvironment.ReportPath, reportName + ".json")}");
            return succeeded ? 0 : 1;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  single             Explore the single-state model");
            Console.WriteLine("  single-replay      Replay a saved single-state path");
            Console.WriteLine("  multi              Explore the multi-state model");
            Console.WriteLine("  single-continuous  Follow a continuous single-state path");
            Console.WriteLine("  multi-continuous   Follow a continuous multi-state path");
            Console.WriteLine("  help               Show available commands");
            Console.WriteLine("  exit               Close the sample");
        }
    }
}
