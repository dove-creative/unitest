using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                while (true)
                {
                    if (!task.IsCompleted)
                    {
                        if (processLimit > 0 && project.ProcessCount > processLimit)
                        {
                            Logger.Log(Project<Model>.DividingLine);
                            Logger.Log($"Cancelling test: process limit exceeded ({project.ProcessCount}/{processLimit})");
                            project.Cancel();
                        }
                        else if (timeLimit > 0 && stopwatch.Elapsed.TotalSeconds > timeLimit)
                        {
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
                                ExportXml(result.Report, projectName, projectPath, true, true);

                            return true;
                        }
                        else
                        {
                            Logger.LogError($"{projectName} Failed ({project.ProcessCount} / {elapsed:F2}s)");

                            var report = result.GetFailedReports();

                            ExportXml(report, $"{projectName} - Failed", projectPath, printResult, true);
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

        //static IEnumerable<XmlElement> GetLeaves(XmlElement node, Func<XmlElement, bool> predicate)
        //{
        //    if (node == null)
        //        yield break;

        //    var children = node.ChildNodes.OfType<XmlElement>().ToArray();
        //    if (children.Length == 0)
        //    {
        //        if (predicate(node))
        //        {
        //            yield return node;
        //            yield break;
        //        }
        //    }

        //    foreach (var child in children)
        //    {
        //        foreach (var leaf in GetLeaves(child, predicate))
        //            yield return leaf;
        //    }
        //}

        public static void ExportXml(XmlNode node, string name, string path, bool open = false, bool copyToClipboard = false)
        {
            Directory.CreateDirectory(path);
            var fullPath = Path.Combine(path, $"{name}.xml");

            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false,
                Encoding = Encoding.UTF8
            };

            string xmlContent;
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                node.WriteTo(xmlWriter);
                xmlWriter.Flush();
                xmlContent = stringWriter.ToString();
            }

            File.WriteAllText(fullPath, xmlContent, Encoding.UTF8);

            if (copyToClipboard)
            {
                CopyToClipboard(xmlContent);
            }

            if (open)
            {
                OpenPath(path);
                OpenPath(fullPath);
            }
        }

        static void CopyToClipboard(string content)
        {
            try
            {
#if UNITY_5_3_OR_NEWER
                UnityEngine.GUIUtility.systemCopyBuffer = content;
#else
                var startInfo = new System.Diagnostics.ProcessStartInfo("clip.exe")
                {
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    if (process == null)
                        throw new InvalidOperationException("Failed to start clip.exe.");

                    process.StandardInput.Write(content);
                    process.StandardInput.Close();
                    process.WaitForExit();
                }
#endif
                Logger.Log("Node content has been copied to the clipboard.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        static void OpenPath(string path)
        {
            try
            {
#if UNITY_5_3_OR_NEWER
                UnityEngine.Application.OpenURL(path);
#else
                var startInfo = new System.Diagnostics.ProcessStartInfo(path)
                {
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(startInfo);
#endif
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
