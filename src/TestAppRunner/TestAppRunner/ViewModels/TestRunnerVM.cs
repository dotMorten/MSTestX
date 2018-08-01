using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TestAppRunner.ViewModels
{

    public class TestRunnerVM : VMBase, ITestExecutionRecorder
    {
        private static TestRunner testRunner;
        private static Dictionary<Guid, TestResultVM> alltests;
        private Dictionary<Guid, TestResultVM> tests;

        static TestRunnerVM _Instance;
        public static TestRunnerVM Instance => _Instance ?? (_Instance = new TestRunnerVM());

        private TestRunnerVM()
        {
            LoadTests();
        }

        private async void LoadTests()
        {
            Status = "Loading tests...";
            OnPropertyChanged(nameof(Status));
            if (testRunner == null)
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    var tests = new Dictionary<Guid, TestResultVM>();
                    var references = AppDomain.CurrentDomain.GetAssemblies().Where(c => !c.IsDynamic).Select(c => System.IO.Path.GetFileName(c.CodeBase)).ToArray();
                    testRunner = new TestRunner(references, new TestSettings(), this);
                    foreach (var item in testRunner.Tests)
                    {
                        tests[item.Id] = new TestResultVM() { Test = item };
                    }
                    alltests = this.tests = tests;
                });
            }
            OnPropertyChanged(nameof(Tests));
            OnPropertyChanged(nameof(GroupedTests));
            OnPropertyChanged(nameof(TestStatus));
            Status = $"{tests.Count} tests found.";
            OnPropertyChanged(nameof(Status));
        }

        internal void UpdateGroup(string grouping)
        {
            if(tests != null)
            {
                if (grouping == "Category")
                    _GroupedTests = new List<TestResultGroup>(tests.Values.GroupBy(t => t.Category).Select((g, t) => new TestResultGroup(g.Key, g)));
                else if (grouping == "Outcome")
                    _GroupedTests = new List<TestResultGroup>(tests.Values.GroupBy(t => t.Outcome).Select((g, t) => new TestResultGroup(g.Key.ToString(), g)));
                else if (grouping == "Namespace")
                {
                    Func<string, string> getNamespace = (s) => s.Substring(0, s.LastIndexOf("."));
                    _GroupedTests = new List<TestResultGroup>(tests.Values.GroupBy(t => getNamespace(t.Test.FullyQualifiedName)).Select((g, t) => new TestResultGroup(g.Key, g)));
                }
                OnPropertyChanged(nameof(GroupedTests));
            }
        }

        public string Status { get; private set; }

        public string TestStatus => $"{PassedTests} passed. {FailedTests} failed. {SkippedTests} skipped. {NotRunTests} not run";

        private CancellationTokenSource tcs;

        public void Cancel()
        {
            tcs?.Cancel();
            tcs = null;
        }
        public void Run()
        {
            Run(testRunner.Tests);
        }

        public async void Run(IEnumerable<TestCase> testCollection)
        {
            var t = tcs = new CancellationTokenSource();
            Status = $"Running tests...";
            OnPropertyChanged(nameof(Status));
            foreach (var item in testCollection)
            {
                tests[item.Id].Result = null;
                tests[item.Id].OnPropertyChanged(nameof(TestResultVM.Result));
            }
            var task = testRunner.Run(testCollection, t.Token);
            OnPropertyChanged(nameof(IsRunning));
            try
            {
                await task;
                if(t.IsCancellationRequested)
                    Status = $"Test run canceled.";
                else
                    Status = $"Test run completed.";
            }
            catch (System.Exception ex)
            {
                Status = $"Test run failed to run: {ex.Message}";
            }
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(Status));
        }

        public bool IsRunning => testRunner.IsRunning;
        public int PassedTests => Tests?.Where(t => t.Result?.Outcome == TestOutcome.Passed).Count() ?? 0;
        public int FailedTests => Tests?.Where(t => t.Result?.Outcome == TestOutcome.Failed).Count() ?? 0;
        public int SkippedTests => Tests?.Where(t => t.Result?.Outcome == TestOutcome.Skipped).Count() ?? 0;
        public int NotRunTests => Tests?.Where(t => t.Result == null).Count() ?? 0;

        public double Progress => Tests == null || Tests.Count() == 0 ? 0 : 1 - (NotRunTests / (double)Tests.Count());

        public IEnumerable<TestResultVM> Tests => tests?.Values;
        List<TestResultGroup> _GroupedTests;
        public List<TestResultGroup> GroupedTests
        {
            get
            {
                if(_GroupedTests == null && tests != null)
                {
                    _GroupedTests = new List<TestResultGroup>(tests.Values.GroupBy(t => t.Category).Select((g, t) => new TestResultGroup(g.Key, g)));
                }
                return _GroupedTests;
            }
        }

        public class TestResultGroup : List<TestResultVM>
        {
            public TestResultGroup(string group, IEnumerable<TestResultVM> tests) : base(tests)
            {
                Group = group;
            }
            public string Group { get; }
        }

        void ITestExecutionRecorder.RecordResult(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult testResult)
        {
            var test = tests[testResult.TestCase.Id];
            test.Result = testResult;
            switch (testResult.Outcome)
            {
                case TestOutcome.Failed: test.Outcome = UnitTestOutcome.Failed; break;
                case TestOutcome.Passed: test.Outcome = UnitTestOutcome.Passed; break;
                case TestOutcome.NotFound: test.Outcome = UnitTestOutcome.Error; break;
                case TestOutcome.Skipped: test.Outcome = UnitTestOutcome.NotRunnable; break;
                case TestOutcome.None: test.Outcome = UnitTestOutcome.Unknown; break;
            }
            test.OnPropertyChanged(nameof(TestResultVM.Result));
            test.OnPropertyChanged(nameof(TestResultVM.Outcome));
            test.OnPropertyChanged(nameof(TestResultVM.Duration));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(TestStatus));

            System.Diagnostics.Debug.WriteLine($"Completed test: {testResult.TestCase.FullyQualifiedName} - {testResult.Outcome.ToString().ToUpper()} {testResult.ErrorMessage}");

            //var s = new System.Runtime.Serialization.DataContractSerializer(testResult.GetType());
            //using (var ms = new System.IO.MemoryStream())
            //{
            //    s.WriteObject(ms, testResult);
            //    var xml = System.Text.Encoding.Default.GetString(ms.ToArray());
            //}
        }

        void ITestExecutionRecorder.RecordStart(TestCase testCase)
        {
            tests[testCase.Id].Outcome = UnitTestOutcome.InProgress;
            tests[testCase.Id].OnPropertyChanged(nameof(TestResultVM.Outcome));
        }

        void ITestExecutionRecorder.RecordEnd(TestCase testCase, TestOutcome outcome)
        {
        }

        void ITestExecutionRecorder.RecordAttachments(IList<AttachmentSet> attachmentSets)
        {
        }

        void IMessageLogger.SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{testMessageLevel} - {message}");
        }
    }
}
