﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter;
using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Execution;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestAppRunner
{
    internal class TestRunner : IDiscoveryContext, IRunContext, IFrameworkHandle
    {
        private ITestExecutionRecorder recorder;
        private TestCaseDiscoverySink sink;
        private TestRunCancellationToken? token;
        private IRunSettings? settings;

        public TestRunner(IEnumerable<string> sources, ITestExecutionRecorder recorder)
        {
            sink = new TestCaseDiscoverySink();
            new MSTestDiscoverer().DiscoverTests(sources, this, this, sink);
            this.recorder = recorder;
        }

        IRunSettings? IDiscoveryContext.RunSettings => settings;

        public IEnumerable<TestCase> Tests => sink?.Tests ?? System.Linq.Enumerable.Empty<TestCase>();

        internal Task Run(IRunSettings? runSettings = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            return Run(sink.Tests, runSettings, cancellationToken);
        }

        internal Task Run(IEnumerable<TestCase> tests, IRunSettings? runSettings, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
        {
            if (IsRunning)
                throw new InvalidOperationException("Test run already running");
            token = new TestRunCancellationToken();
            if (cancellationToken.CanBeCanceled)
                cancellationToken.Register(() => token.Cancel());
            settings = runSettings;
            MSTestSettings.PopulateSettings(this);
            return System.Threading.Tasks.Task.Run(() => {
                new TestExecutionManager().RunTests(tests, this, this, token);
                token = null;
            });
        }

        public bool IsRunning => token != null;

        #region IRunContext

        public bool KeepAlive => true;

        public bool InIsolation => throw new NotImplementedException();

        public bool IsDataCollectionEnabled => false;

        public bool IsBeingDebugged => System.Diagnostics.Debugger.IsAttached;

        public string? TestRunDirectory { get; set; }

        public string? SolutionDirectory { get; set; }

        ITestCaseFilterExpression? IRunContext.GetTestCaseFilter(IEnumerable<string>? supportedProperties, Func<string, TestProperty?> propertyProvider)
        {
            return null;
        }

        #endregion

        #region IFrameworkHandle

        bool IFrameworkHandle.EnableShutdownAfterTestRun { get; set; }

        int IFrameworkHandle.LaunchProcessWithDebuggerAttached(string filePath, string? workingDirectory, string? arguments, IDictionary<string, string?>? environmentVariables)
        {
            throw new NotImplementedException();
        }

        void ITestExecutionRecorder.RecordResult(TestResult testResult) => recorder?.RecordResult(testResult);

        void ITestExecutionRecorder.RecordStart(TestCase testCase) => recorder?.RecordStart(testCase);

        void ITestExecutionRecorder.RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            recorder?.RecordEnd(testCase, outcome);
        }

        void ITestExecutionRecorder.RecordAttachments(IList<AttachmentSet> attachmentSets) => recorder?.RecordAttachments(attachmentSets);

        void IMessageLogger.SendMessage(TestMessageLevel testMessageLevel, string message) => recorder?.SendMessage(testMessageLevel, message);

        #endregion
    }
}
