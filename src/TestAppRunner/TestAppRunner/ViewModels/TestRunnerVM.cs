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
    internal class TestRunnerVM : VMBase, ITestExecutionRecorder
    {
        private static TestRunner testRunner;
        private static Dictionary<Guid, TestResultVM> alltests;
        private Dictionary<Guid, TestResultVM> tests;
        private System.IO.StreamWriter logOutput;
        private TrxWriter trxWriter;

        private static TestRunnerVM _Instance;
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
                    testRunner = new TestRunner(references, new RunSettings(), this);
                    foreach (var item in testRunner.Tests)
                    {
                        tests[item.Id] = new TestResultVM() { Test = item };
                    }
                    alltests = this.tests = tests;
                });
                if (Settings.AutoStart)
                    Run();
            }
            OnPropertyChanged(nameof(Tests));
            OnPropertyChanged(nameof(GroupedTests));
            OnPropertyChanged(nameof(TestStatus));
            Status = $"{tests.Count} tests found.";
            OnPropertyChanged(nameof(Status));
        }

        private string _grouping;
        internal void UpdateGroup(string grouping)
        {
            _grouping = grouping;
            if (tests != null)
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

        public string TestStatus => $"{PassedTests} passed. {FailedTests} failed. {SkippedTests} skipped. {NotRunTests} not run. {Percentage.ToString("0")}%";

        private CancellationTokenSource tcs;

        public void Cancel()
        {
            tcs?.Cancel();
            tcs = null;
        }
        public Task<IEnumerable<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult>> Run()
        {
            return Run(testRunner.Tests);
        }

        private List<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult> results;

        public async Task<IEnumerable<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult>> Run(IEnumerable<TestCase> testCollection)
        {
            var t = tcs = new CancellationTokenSource();
            Status = $"Running tests...";
            OnPropertyChanged(nameof(Status));
            foreach (var item in testCollection)
            {
                tests[item.Id].Result = null;
                tests[item.Id].OnPropertyChanged(nameof(TestResultVM.Result));
            }
            results = new List<Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult>();
            if (!string.IsNullOrEmpty(Settings.ProgressLogPath))
            {
                var s = System.IO.File.OpenWrite(Settings.ProgressLogPath);
                logOutput = new System.IO.StreamWriter(s); // Settings.ProgressLogPath, true);
                logOutput.WriteLine("*************************************************");
                logOutput.WriteLine($"* Starting Test Run @ {DateTime.Now}");
                logOutput.WriteLine("*************************************************");
            }
            if(!string.IsNullOrEmpty(Settings.TrxOutputPath))
            {
                trxWriter = new TrxWriter(Settings.TrxOutputPath);
                trxWriter.InitializeReport();
            }
            var task = testRunner.Run(testCollection, t.Token);
            OnPropertyChanged(nameof(IsRunning));
            try
            {
                await task;
                if (t.IsCancellationRequested)
                {
                    Status = $"Test run canceled.";
                }
                else
                {
                    Status = $"Test run completed.";
                }
            }
            catch (System.Exception ex)
            {
                Status = $"Test run failed to run: {ex.Message}";
            }
            if (logOutput != null)
            {
                Log("*************************************************");
                Log(Status);
                Log(TestStatus);
                Log("*************************************************\n\n");
                logOutput.Dispose();
                logOutput = null;
            }
            if (trxWriter != null)
            {
                trxWriter.FinalizeReport();
                trxWriter = null;
            }
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(Status));
            return results;
        }

        public bool IsRunning => testRunner.IsRunning;
        public int PassedTests => Tests?.Where(t => t.Result?.Outcome == TestOutcome.Passed).Count() ?? 0;
        public int FailedTests => Tests?.Where(t => t.Result?.Outcome == TestOutcome.Failed).Count() ?? 0;
        public int SkippedTests => Tests?.Where(t => t.Result?.Outcome == TestOutcome.Skipped).Count() ?? 0;
        public int NotRunTests => Tests?.Where(t => t.Result == null).Count() ?? 0;
        public double Percentage => Tests?.Any(t => t.Result?.Outcome == TestOutcome.Passed) == true ? (int)(PassedTests * 100d / (FailedTests + PassedTests)) : 0;
        public double Progress => Tests == null || Tests.Count() == 0 ? 0 : 1 - (NotRunTests / (double)Tests.Count());

        public IEnumerable<TestResultVM> Tests => tests?.Values;
        private List<TestResultGroup> _GroupedTests;

        public List<TestResultGroup> GroupedTests
        {
            get
            {
                if(_GroupedTests == null && tests != null)
                {
                    UpdateGroup(_grouping);
                }
                return _GroupedTests;
            }
        }

        public TestSettings Settings { get; internal set; }

        private static T GetProperty<T>(string id, TestObject test, T defaultValue)
        {
            var prop = test.Properties.Where(p => p.Id == id).FirstOrDefault();
            if (prop != null)
                return test.GetPropertyValue<T>(prop, defaultValue);
            return defaultValue;
        }
        void ITestExecutionRecorder.RecordResult(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult testResult)
        {
            results?.Add(testResult);
            var innerResultsCount = GetProperty<int>("InnerResultsCount", testResult, 0);
            var parentExecId = GetProperty<Guid?>("ParentExecId", testResult, Guid.Empty);
            if(parentExecId == Guid.Empty) // We don't report child result in the UI
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
                OnPropertyChanged(nameof(Percentage));
                OnPropertyChanged(nameof(TestStatus));
            }
            Log($"Completed test '{testResult.TestCase.FullyQualifiedName}': {testResult.Outcome} {testResult.ErrorMessage}");
            System.Diagnostics.Debug.WriteLine($"Completed test: {testResult.TestCase.FullyQualifiedName} - {testResult.Outcome.ToString().ToUpper()} {testResult.ErrorMessage}");
            //var s = new System.Runtime.Serialization.DataContractSerializer(testResult.GetType());
            //using (var ms = new System.IO.MemoryStream())
            //{
            //    s.WriteObject(ms, testResult);
            //    var xml = System.Text.Encoding.Default.GetString(ms.ToArray());
            //}
            trxWriter?.RecordResult(testResult);
            Settings.TestRecorder?.RecordResult(testResult);
        }

        private void Log(string message)
        {
            if (logOutput != null)
            {
                logOutput.WriteLine(message);
                logOutput.Flush();
            }
        }

        void ITestExecutionRecorder.RecordStart(TestCase testCase)
        {
            tests[testCase.Id].Outcome = UnitTestOutcome.InProgress;
            tests[testCase.Id].OnPropertyChanged(nameof(TestResultVM.Outcome));
            Log($"Starting test '{testCase.FullyQualifiedName}'");
            Settings.TestRecorder?.RecordStart(testCase);
        }

        void ITestExecutionRecorder.RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            Settings.TestRecorder?.RecordEnd(testCase, outcome);
        }

        void ITestExecutionRecorder.RecordAttachments(IList<AttachmentSet> attachmentSets)
        {
            Settings.TestRecorder?.RecordAttachments(attachmentSets);
        }

        void IMessageLogger.SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            System.Diagnostics.Debug.WriteLine($"{testMessageLevel} - {message}");
            Settings.TestRecorder?.SendMessage(testMessageLevel, message);
        }
    }
}
