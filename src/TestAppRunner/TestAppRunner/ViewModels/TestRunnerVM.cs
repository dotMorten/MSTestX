using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using MessageType = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel.MessageType;

namespace TestAppRunner.ViewModels
{
    internal class TestRunnerVM : VMBase, ITestExecutionRecorder
    {
        private static TestRunner testRunner;
        private static Dictionary<Guid, TestResultVM> alltests;
        private Dictionary<Guid, TestResultVM> tests;
        private System.IO.StreamWriter logOutput;
        private TrxWriter trxWriter;
        private TestAdapterConnection connection;

        private static TestRunnerVM _Instance;

        public static TestRunnerVM Instance => _Instance ?? (_Instance = new TestRunnerVM());

        internal MSTestX.RunnerApp HostApp { get; set; }

        private TestRunnerVM()
        {   
        }

        public void Initialize()
        {
            int port = Settings.TestAdapterPort;
            if (port > 0)
            {
                InitializeTestAdapterConnection(port);
            }
            LoadTests();
        }

        public async void InitializeTestAdapterConnection(int port)
        {
            Status = $"Waiting for connection on port {port}...";
            OnPropertyChanged(nameof(Status));
            var conn = new TestAdapterConnection(port);
            await conn.StartAsync();
            connection = conn;
        }

        private async void LoadTests()
        {
            Status = "Loading tests...";
            OnPropertyChanged(nameof(Status));
            if (testRunner == null)
            {
                await Task.Run(() =>
                {
                    var tests = new Dictionary<Guid, TestResultVM>();
                    var references = AppDomain.CurrentDomain.GetAssemblies().Where(c => !c.IsDynamic).Select(c => System.IO.Path.GetFileName(c.CodeBase)).ToArray();
                    //references = references.Where(r => !r.StartsWith("Microsoft.") && !r.StartsWith("Xamarin.Android.") && r != "mscorlib.dll" && !r.StartsWith("System.")).ToArray();
                    testRunner = new TestRunner(references, this);
                    foreach (var item in testRunner.Tests)
                    {
                        tests[item.Id] = new TestResultVM(item);
                    }
                    alltests = this.tests = tests;
                    this.HostApp.RaiseTestsDiscovered(testRunner.Tests);
                });
                if (Settings.AutoRun)
                {
                    var _ = Run(Settings);
                }
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

        public string DiagnosticsInfo { get; private set; }

        public string TestStatus => $"{PassedTests} passed. {FailedTests} failed. {SkippedTests} skipped. {NotRunTests} not run. {Percentage.ToString("0")}%";

        private CancellationTokenSource tcs;

        public event EventHandler<Exception> OnTestRunException;

        public void Cancel()
        {
            tcs?.Cancel();
            tcs = null;
        }

        public Task<IEnumerable<TestResult>> Run()
        {
            return Run(testRunner.Tests, Settings);
        }
        public Task<IEnumerable<TestResult>> Run(IRunSettings runSettings)
        {
            return Run(testRunner.Tests, runSettings);
        }

        private List<TestResult> results;

        public Task<IEnumerable<TestResult>> Run(IEnumerable<TestCase> testCollection)
        {
            return Run(testCollection, Settings);
        }

        public async Task<IEnumerable<TestResult>> Run(IEnumerable<TestCase> testCollection, IRunSettings runSettings)
        {
            if (IsRunning)
                throw new InvalidOperationException("Can't begin a test run while another is in progress");
            try
            {
                return await Run_Internal(testCollection, runSettings);
            }
            catch(System.Exception ex)
            {
                OnTestRunException?.Invoke(this, ex);
                throw;
            }
        }

        private async Task<IEnumerable<TestResult>> Run_Internal(IEnumerable<TestCase> testCollection, IRunSettings runSettings)
        {
            HostApp?.RaiseTestRunStarted(testCollection);
            var t = tcs = new CancellationTokenSource();
            Status = $"Running tests...";
            OnPropertyChanged(nameof(Status));
            DiagnosticsInfo = "";
            OnPropertyChanged(nameof(DiagnosticsInfo));
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

            results = new List<TestResult>();
            if (!string.IsNullOrEmpty(Settings.ProgressLogPath))
            {
                var s = System.IO.File.OpenWrite(Settings.ProgressLogPath);
                logOutput = new System.IO.StreamWriter(s);
                logOutput.WriteLine("*************************************************");
                logOutput.WriteLine($"* Starting Test Run @ {DateTime.Now}");
                logOutput.WriteLine("*************************************************");
                Logger.Log($"LOGREPORT LOCATION: {Settings.ProgressLogPath}");
            }
            if (!string.IsNullOrEmpty(Settings.TrxOutputPath))
            {
                trxWriter = new TrxWriter(Settings.TrxOutputPath);
                trxWriter.InitializeReport();
            }
            DateTime start = DateTime.Now;
            Logger.Log($"STARTING TESTRUN {testCollection.Count()} Tests");
            var task = testRunner.Run(testCollection, runSettings, t.Token);
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
            DateTime end = DateTime.Now;
            CurrentTestRunning = null;
            OnPropertyChanged(nameof(CurrentTestRunning));
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
                Logger.Log($"TRXREPORT LOCATION: {Settings.TrxOutputPath}");
            }
            DiagnosticsInfo += $"\nLast run duration: {(end - start).ToString("c")}";
            DiagnosticsInfo += $"\n{results.Where(a => a.Outcome == TestOutcome.Passed).Count()} passed - {results.Where(a => a.Outcome == TestOutcome.Failed).Count()} failed";
            if (Settings.ProgressLogPath != null) DiagnosticsInfo += $"\nLog: {Settings.ProgressLogPath}";
            if (Settings.TrxOutputPath != null) DiagnosticsInfo += $"\nTRX Report: {Settings.TrxOutputPath}";
            OnPropertyChanged(nameof(DiagnosticsInfo));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(Status));
            HostApp?.RaiseTestRunCompleted(results);
            Logger.Log($"COMPLETED TESTRUN Total:{results.Count()} Failed:{results.Where(a => a.Outcome == TestOutcome.Failed).Count()} Passed:{results.Where(a => a.Outcome == TestOutcome.Passed).Count()}  Skipped:{results.Where(a => a.Outcome == TestOutcome.Skipped).Count()}");
            if (Settings.TerminateAfterExecution)
            {
                Terminate();
            }
            return results;
        }

        private void Terminate()
        {
            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    {
#if __IOS__
                        var selector = new ObjCRuntime.Selector("terminateWithSuccess");
                        UIKit.UIApplication.SharedApplication.PerformSelector(selector, UIKit.UIApplication.SharedApplication, 0);
#endif
                        /*
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
                        */
                    }
                    break;
                case Device.Android:
#if __ANDROID__
                   Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#endif
                    break;
                default:
                    Environment.Exit(0);
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

        public MSTestX.TestOptions Settings { get; internal set; }

        private static T GetProperty<T>(string id, TestObject test, T defaultValue)
        {
            var prop = test.Properties.Where(p => p.Id == id).FirstOrDefault();
            if (prop != null)
                return test.GetPropertyValue<T>(prop, defaultValue);
            return defaultValue;
        }
        void ITestExecutionRecorder.RecordResult(TestResult testResult)
        {
            results?.Add(testResult);
            var innerResultsCount = GetProperty<int>("InnerResultsCount", testResult, 0);
            var parentExecId = GetProperty<Guid>("ParentExecId", testResult, Guid.Empty);
            var test = tests[testResult.TestCase.Id];
            if (parentExecId == Guid.Empty) // We don't report child result in the UI
            {
                test.Result = testResult;
               
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(Percentage));
                OnPropertyChanged(nameof(TestStatus));
                OnPropertyChanged(nameof(NotRunTests));
                OnPropertyChanged(nameof(PassedTests));
                OnPropertyChanged(nameof(FailedTests));
                OnPropertyChanged(nameof(SkippedTests));
            }
            else
            {
                if (test.ChildResults == null)
                {
                    test.ChildResults = new System.Collections.ObjectModel.ObservableCollection<TestResult>();
                    test.OnPropertyChanged(nameof(TestResultVM.ChildResults));
                }
                Device.BeginInvokeOnMainThread(() =>
                {
                    test.ChildResults.Add(testResult);
                });
            }
            Log($"Completed test '{testResult.TestCase.FullyQualifiedName}': {testResult.Outcome} {testResult.ErrorMessage}");
            trxWriter?.RecordResult(testResult);
            Logger.LogResult(testResult);
            connection?.SendTestEndResult(testResult);
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
            var vmtest = tests[testCase.Id];
            vmtest.SetInProgress();
            CurrentTestRunning = vmtest;
            OnPropertyChanged(nameof(CurrentTestRunning));
            Log($"Starting test '{testCase.FullyQualifiedName}'");
            Logger.LogTestStart(testCase);
            connection?.SendTestStart(testCase);
            Settings.TestRecorder?.RecordStart(testCase);
        }

        public TestResultVM CurrentTestRunning { get; private set; }

        void ITestExecutionRecorder.RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            connection?.SendTestEnd(testCase, outcome);
            Settings.TestRecorder?.RecordEnd(testCase, outcome);
        }

        void ITestExecutionRecorder.RecordAttachments(IList<AttachmentSet> attachmentSets)
        {
            connection?.SendAttachments(attachmentSets);
            Settings.TestRecorder?.RecordAttachments(attachmentSets);
        }

        void IMessageLogger.SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            Log($"MESSAGE: {testMessageLevel}: {message}");
            connection?.SendMessage(testMessageLevel, message);
            //Logger.LogMessage(Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel.MessageType.TestMessage)
            Settings.TestRecorder?.SendMessage(testMessageLevel, message);
        }
    }
}
