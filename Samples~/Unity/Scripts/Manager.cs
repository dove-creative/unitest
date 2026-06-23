using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UniTest;

namespace UniTest_Test
{
    class Manager : MonoBehaviour
    {
        private enum TestMode
        {
            SingleState,
            SingleStateReplay,
            MultiState,
            SingleStateContinuously,
            MultiStateContinuously,
        }

        private const int ProcessLimit = -1;
        private const int TimeLimit = -1;

        private const int SingleStateDepth = 5;
        private const int MultiStateDepth = 5;
        private const int SingleStateContinuousDepth = 20;
        private const int MultiStateContinuousDepth = 50;

        private const string SingleStateReportName = "SingleState";
        private const string SingleStateReplayReportName = "SingleStateReplay";
        private const string MultiStateReportName = "MultiState";
        private const string SingleStateReplayIDs = "Ignite/Mount_Null/Mount_Targeted";

        [SerializeField] private TestMode _testMode;

        private static string ReportPath
            => Path.Combine(Application.persistentDataPath, "UniTest", "Samples", "Unity");

        private async void Start() => await Run(_testMode);

        private Task Run(TestMode testMode)
        {
            switch (testMode)
            {
                case TestMode.SingleState:
                    return RunSingleStateAsync();

                case TestMode.SingleStateReplay:
                    return RunSingleStateReplayAsync();

                case TestMode.MultiState:
                    return RunMultiStateAsync();

                case TestMode.SingleStateContinuously:
                    return RunSingleStateContinuouslyAsync();

                case TestMode.MultiStateContinuously:
                    return RunMultiStateContinuouslyAsync();

                default:
                    throw new ArgumentOutOfRangeException(nameof(testMode), testMode, null);
            }
        }

        [ContextMenu(nameof(RunSingleState))]
        private async void RunSingleState() => await RunSingleStateAsync();

        private async Task RunSingleStateAsync()
        {
            await new SingleState.Project()
                .Run(
                    ReportPath,
                    SingleStateReportName,
                    SingleStateDepth,
                    processLimit: ProcessLimit,
                    timeLimit: TimeLimit,
                    printResult: true);
        }

        [ContextMenu(nameof(RunSingleStateReplay))]
        private async void RunSingleStateReplay() => await RunSingleStateReplayAsync();

        private async Task RunSingleStateReplayAsync()
        {
            await new SingleState.Project()
                .Run(
                    ReportPath,
                    SingleStateReplayReportName,
                    SingleStateReplayIDs,
                    timeLimit: TimeLimit,
                    printResult: true);
        }

        [ContextMenu(nameof(RunMultiState))]
        private async void RunMultiState() => await RunMultiStateAsync();

        private async Task RunMultiStateAsync()
        {
            await new MultiState.Project()
                .Run(
                    ReportPath,
                    MultiStateReportName,
                    MultiStateDepth,
                    processLimit: ProcessLimit,
                    timeLimit: TimeLimit,
                    printResult: true);
        }

        [ContextMenu(nameof(RunSingleStateContinuously))]
        private async void RunSingleStateContinuously() => await RunSingleStateContinuouslyAsync();

        private async Task RunSingleStateContinuouslyAsync()
        {
            await new SingleState.Project()
                .RunContinuously(
                    ReportPath,
                    SingleStateReportName,
                    SingleStateContinuousDepth,
                    processLimit: ProcessLimit,
                    timeLimit: TimeLimit,
                    printResult: true);
        }

        [ContextMenu(nameof(RunMultiStateContinuously))]
        private async void RunMultiStateContinuously() => await RunMultiStateContinuouslyAsync();

        private async Task RunMultiStateContinuouslyAsync()
        {
            await new MultiState.Project()
                .RunContinuously(
                    ReportPath,
                    MultiStateReportName,
                    MultiStateContinuousDepth,
                    processLimit: ProcessLimit,
                    timeLimit: TimeLimit,
                    printResult: true);
        }
    }
}
