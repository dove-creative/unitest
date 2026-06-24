using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UniTest;

namespace UniTest_Test
{
    class Manager : MonoBehaviour
    {
        enum TestMode
        {
            SingleState,
            SingleStateReplay,
            MultiState,
            SingleStateContinuously,
            MultiStateContinuously,
        }
        const int ProcessLimit = -1;
        const int TimeLimit = -1;
        const int SingleStateDepth = 5;
        const int MultiStateDepth = 5;
        const int SingleStateContinuousDepth = 20;
        const int MultiStateContinuousDepth = 50;
        const string SingleStateReportName = "SingleState";
        const string SingleStateReplayReportName = "SingleStateReplay";
        const string MultiStateReportName = "MultiState";
        const string SingleStateReplayIDs = "Ignite/Mount_Null/Mount_Targeted";

        [SerializeField] TestMode _testMode;
        static string ReportPath
            => Path.Combine(Application.persistentDataPath, "UniTest", "Samples", "Unity");
        async void Start() => await Run(_testMode);
        Task Run(TestMode testMode)
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
        async void RunSingleState() => await RunSingleStateAsync();
        async Task RunSingleStateAsync()
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
        async void RunSingleStateReplay() => await RunSingleStateReplayAsync();
        async Task RunSingleStateReplayAsync()
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
        async void RunMultiState() => await RunMultiStateAsync();
        async Task RunMultiStateAsync()
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
        async void RunSingleStateContinuously() => await RunSingleStateContinuouslyAsync();
        async Task RunSingleStateContinuouslyAsync()
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
        async void RunMultiStateContinuously() => await RunMultiStateContinuouslyAsync();
        async Task RunMultiStateContinuouslyAsync()
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
