using System;
using System.IO;
using System.Threading.Tasks;

namespace UniTest
{
    public static partial class Tools
    {
        public static Task<bool> Run<TModel>(this Project<TModel> project, string projectPath, string reportName, int depth, int processLimit = -1, int timeLimit = -1, bool printResult = false) where TModel : Model, new()
        {
            return RunAsync(project, project.Execute(depth), projectPath, reportName, processLimit, timeLimit, printResult);
        }

        public static Task<bool> Run<TModel>(this Project<TModel> project, string projectPath, string reportName, string executionIDs, int timeLimit = -1, bool printResult = false) where TModel : Model, new()
        {
            return RunAsync(project, project.Execute(executionIDs), projectPath, reportName, -1, timeLimit, printResult);
        }

        public static Task<bool> RunContinuously<TModel>(this Project<TModel> project, string projectPath, string projectName, int depth, int seed = 0, int processLimit = -1, int timeLimit = -1, bool printResult = false) where TModel : Model, new()
        {
            return RunAsync(project, project.ExecuteContinuously(depth, seed), projectPath, projectName, processLimit, timeLimit, printResult);
        }

        static async Task<bool> RunAsync<TModel>(Project<TModel> project, Task<Node<TModel>> task, string projectPath, string projectName, int processLimit, int timeLimit, bool printResult) where TModel : Model, new()
        {
            int timespan = 1;
            int times = 1;
            bool limitCancellationRequested = false;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                while (true)
                {
                    if (!task.IsCompleted)
                    {
                        if (!limitCancellationRequested && processLimit > 0 && project.ProcessCount > processLimit)
                        {
                            limitCancellationRequested = true;
                            Logger.Log(Project<Model>.DividingLine);
                            Logger.Log($"Cancelling test: process limit exceeded ({project.ProcessCount}/{processLimit})");
                            project.Cancel();
                        }
                        else if (!limitCancellationRequested && timeLimit > 0 && stopwatch.Elapsed.TotalSeconds > timeLimit)
                        {
                            limitCancellationRequested = true;
                            Logger.Log(Project<Model>.DividingLine);
                            Logger.Log($"Cancelling test: time limit exceeded ({stopwatch.Elapsed.TotalSeconds:F1}/{timeLimit})");
                            project.Cancel();
                        }

                        await Task.Delay(1);
                    }

                    if (task.IsCompleted)
                    {
                        var result = await task;

                        stopwatch.Stop();
                        var elapsed = stopwatch.Elapsed.TotalSeconds;

                        if (result.AllSucceed(out var cancelled))
                        {
                            Logger.Log($"{projectName} {(!cancelled ? "Succeed" : "Cancelled")} ({project.ProcessCount} / {elapsed:F2}s)");

                            if (printResult)
                                ExportJson(result.GetReport(), projectName, projectPath, true);

                            return true;
                        }
                        else
                        {
                            Logger.LogError($"{projectName} Failed ({project.ProcessCount} / {elapsed:F2}s)");

                            var report = result.GetFailedReports();

                            ExportJson(report, $"{projectName} - Failed", projectPath, printResult);
                            return false;
                        }
                    }

                    int current = (int)Math.Ceiling(stopwatch.Elapsed.TotalSeconds / timespan);
                    if (current != times)
                    {
                        times = current;
                        Logger.Log($"{projectName} : {project.ProcessCount} / {stopwatch.Elapsed.TotalSeconds:F2}s");
                    }

                    await Task.Delay(1);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public static void ExportJson(TestReport report, string name, string path, bool open = false)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            var jsonContent = report.ToUtf8Json();
            Directory.CreateDirectory(path);
            var fullPath = Path.Combine(path, $"{name}.json");
            File.WriteAllBytes(fullPath, jsonContent);

            if (open)
            {
                OpenPath(path);
                OpenPath(fullPath);
            }
        }

        static void OpenPath(string path)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo(path)
                {
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
