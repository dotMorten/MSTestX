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
                        tests[item.Id] = new TestResultVM(item);
                    }
                    alltests = this.tests = tests;
                });
                if (Settings.AutoRun)
                    Run();
            }
            OnPropertyChanged(nameof(Tests));
            OnPropertyChanged(nameof(GroupedTests));
            OnPropertyChanged(nameof(TestStatus));
            OnPropertyChanged(nameof(NotRunTests));
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
                    _GroupedTests = new List<TestResultGroup>(tests.Values.GroupBy(t => t.Category).Select((g, t) => new TestResultGroup(g.Key, g)).OrderBy(g => g.Group));
                else if (grouping == "Outcome")
                    _GroupedTests = new List<TestResultGroup>(tests.Values.GroupBy(t => t.Outcome).Select((g, t) => new TestResultGroup(g.Key.ToString(), g)).OrderBy(g => g.Group));
                else if (grouping == "Namespace")
                    _GroupedTests = new List<TestResultGroup>(tests.Values.GroupBy(t => t.Namespace).Select((g, t) => new TestResultGroup(g.Key, g)).OrderBy(g => g.Group));
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
            }
            OnPropertyChanged(nameof(TestStatus));
            OnPropertyChanged(nameof(NotRunTests));
            OnPropertyChanged(nameof(PassedTests));
            OnPropertyChanged(nameof(FailedTests));
            OnPropertyChanged(nameof(SkippedTests));
            OnPropertyChanged(nameof(Percentage));

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
            if(Settings.TerminateAfterExecution)
            {
                Terminate();
            }
            return results;
        }

        private void Terminate()
        {
            switch (Device.RuntimePlatform)
            {
                case Device.Android: System.Environment.Exit(0); break;
                case Device.iOS:
                    {
                        // We'll just use reflection here, rather than having to start doing multi-targeting just for this one platform specific thing
						// Reflection code equivalent to:
                        // var selector = new ObjCRuntime.Selector("terminateWithSuccess");
                        // UIKit.UIApplication.SharedApplication.PerformSelector(selector, UIKit.UIApplication.SharedApplication, 0);
                        var selectorType = Type.GetType("ObjCRuntime.Selector, Xamarin.iOS, Version=0.0.0.0, Culture=neutral, PublicKeyToken=84e04ff9cfb79065");
                        var cnst = selectorType.GetConstructor(new Type[] { typeof(string) });
                        var selector = cnst.Invoke(new object[] { "terminateWithSuccess" });
                        var UIAppType = Type.GetType("UIKit.UIApplication, Xamarin.iOS, Version=0.0.0.0, Culture=neutral, PublicKeyToken=84e04ff9cfb79065");
                        var prop = UIAppType.GetProperty("SharedApplication");
                        var app = prop.GetValue(null);
                        var nsObjectType = Type.GetType("Foundation.NSObject, Xamarin.iOS, Version=0.0.0.0, Culture=neutral, PublicKeyToken=84e04ff9cfb79065");
                        var psMethod = UIAppType.GetMethod("PerformSelector", new Type[] { selector.GetType(), nsObjectType, typeof(double) });
                        psMethod.Invoke(app, new object[] { selector, app, 0d });
                    }
                    break;
                case Device.UWP:
                    // Windows.UI.Xaml.Application.Current.Exit();
                default:
                    break;
            }
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

        public TestOptions Settings { get; internal set; }

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
               
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(Percentage));
                OnPropertyChanged(nameof(TestStatus));
                OnPropertyChanged(nameof(NotRunTests));
                OnPropertyChanged(nameof(PassedTests));
                OnPropertyChanged(nameof(FailedTests));
                OnPropertyChanged(nameof(SkippedTests));
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
            tests[testCase.Id].SetInProgress();
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
            Log($"MESSAGE: {testMessageLevel}: {message}");
            Settings.TestRecorder?.SendMessage(testMessageLevel, message);
        }
    }
}
