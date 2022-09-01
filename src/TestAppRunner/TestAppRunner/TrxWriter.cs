using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestAppRunner
{
    internal class TrxWriter : TestLoggerEvents, ITestLogger, ITestLoggerWithParameters
    {
        Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.TrxLogger logger;

        public TrxWriter(string trxOutputPath)
        {
            logger = new Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.TrxLogger();
            var parameters = new Dictionary<string, string>() { { "TestRunDirectory", "." } };
            if (!string.IsNullOrEmpty(trxOutputPath))
                parameters.Add("LogFileName", trxOutputPath);
            logger.Initialize(this, parameters);
        }

        internal static void GenerateReport(string trxOutputPath, IEnumerable<TestResult> tests)
        {
            var loggerEvents = new TrxWriter(trxOutputPath);
            foreach (var t in tests)
            {
                loggerEvents.OnTestResult(new TestResultEventArgs(t));
            }
            var result = new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero); //TRXLogger doesn't use these values anyway
            loggerEvents?.OnTestRunComplete(result);
        }

        public void OnTestRunMessage(TestRunMessageEventArgs e) => TestRunMessage?.Invoke(this, e);
        public override event EventHandler<TestRunMessageEventArgs> TestRunMessage;

        public void OnTestRunStart(TestRunStartEventArgs e) => TestRunStart?.Invoke(this, e);
        public override event EventHandler<TestRunStartEventArgs> TestRunStart;

        public void OnTestResult(TestResultEventArgs e) => TestResult?.Invoke(this, e);
        public override event EventHandler<TestResultEventArgs> TestResult;

        public void OnTestRunComplete(TestRunCompleteEventArgs e) => TestRunComplete?.Invoke(this, e);
        public override event EventHandler<TestRunCompleteEventArgs> TestRunComplete;

        public void OnDiscoveryStart(DiscoveryStartEventArgs e) => DiscoveryStart?.Invoke(this, e);
        public override event EventHandler<DiscoveryStartEventArgs> DiscoveryStart;

        public void OnDiscoveryMessage(TestRunMessageEventArgs e) => DiscoveryMessage?.Invoke(this, e);
        public override event EventHandler<TestRunMessageEventArgs> DiscoveryMessage;

        public void OnDiscoveredTests(DiscoveredTestsEventArgs e) => DiscoveredTests?.Invoke(this, e);
        public override event EventHandler<DiscoveredTestsEventArgs> DiscoveredTests;

        public void OnDiscoveryComplete(DiscoveryCompleteEventArgs e) => DiscoveryComplete?.Invoke(this, e);

        void ITestLogger.Initialize(TestLoggerEvents events, string testRunDirectory) => logger.Initialize(events, testRunDirectory);

        void ITestLoggerWithParameters.Initialize(TestLoggerEvents events, Dictionary<string, string> parameters) => logger.Initialize(events, parameters);

        public override event EventHandler<DiscoveryCompleteEventArgs> DiscoveryComplete;

    }
}