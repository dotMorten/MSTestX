using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using MSTestX;


#if MAUI
using Microsoft.Maui.Controls;
#else
using Xamarin.Forms;
using static SQLite.SQLite3;
#endif

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
            //TODO: Get iOS mlaunch port number
            var conn = new TestAdapterConnection(port);
            try
            {
                await conn.StartAsync();
                connection = conn;
            }
            catch
            {
                Status = "Failed to open adapter socket";
            }
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
                    var asm = this.Settings.TestAssemblies ?? AppDomain.CurrentDomain.GetAssemblies().Where(c => !c.IsDynamic).ToArray();
                    var references = asm.Select(c => System.IO.Path.GetFileName(c.Location)).ToArray();
                    testRunner = new TestRunner(references, this);
                    foreach (var item in testRunner.Tests)
                    {
                        tests[item.Id] = new TestResultVM(item);
                    }
                    alltests = this.tests = tests;

                    if (!this.Settings.AutoRun || this.Settings.AutoResume)
                    {
                    var previousResults = LoadProgress().ToArray();
                    try
                    {
                        if (previousResults != null)
                        {
                            Parallel.ForEach(previousResults, (previousResult) =>
                            {
                                var candidates = tests.Where(tt => tt.Value.Test.FullyQualifiedName == previousResult.FullyQualifiedName).Select(tt => tt.Value);
                                TestResultVM t = null;
                                if (candidates.Count() == 1)
                                    t = candidates.Single();
                                else
                                    t = candidates.Where(c => previousResult.DisplayName == c.DisplayName).FirstOrDefault();
                                if (t != null)
                                {
                                    try
                                    {
                                        t.Result = MSTestX.UnitTestRunner.TestResultSerializer.Deserialize(previousResult.Result, t.Test);
                                    }
                                    catch { return; }
                                }
                                else
                                {
                                    // TODO: Remove from sql database
                                }
                            });
                            OnPropertyChanged(nameof(PassedTests));
                            OnPropertyChanged(nameof(FailedTests));
                            OnPropertyChanged(nameof(SkippedTests));
                        }
                    }
                        catch { }
                    }


                    this.HostApp.RaiseTestsDiscovered(testRunner.Tests);
                });
                if (Settings.AutoResume)
                {
                    var _ = RunRemainingTests(Settings);
                }
                else if (Settings.AutoRun)
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
            OnPropertyChanged(nameof(IsInitialized));
        }

        private readonly string progressPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unittest_progress.bin");
        private readonly string progressSqlPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "unittest_progress.sql");

        public IEnumerable<TestProgress> LoadProgress()
        {
            if (System.IO.File.Exists(progressSqlPath))
            {
                using (var conn = new SQLiteConnection(progressSqlPath))
                {
                    conn.CreateTable<TestProgress>();
                    foreach(var row in conn.Table<TestProgress>())
                    {
                        yield return row;
                    }
                }
            }
        }
        [Table("TestProgress")]
        public class TestProgress
        {
            [PrimaryKey, AutoIncrement, Column("_id")]
            public int Id { get; set; }

            [MaxLength(250)]
            public string FullyQualifiedName { get; set; }
            [MaxLength(250)]
            public string DisplayName { get; set; }

            [MaxLength(2500), Unique]
            public byte[] Result { get; set; }
        }

        public void DeleteProgress()
        {
            try
            {
                using (var conn = new SQLiteConnection(progressSqlPath))
                {
                    conn.DropTable<TestProgress>();
                }
            }
            catch { }
        }

        public void DeleteProgress(IEnumerable<TestCase> tests)
        {
            try
            {
                //var s = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.JsonDataSerializer.Instance;
                using (var conn = new SQLiteConnection(progressSqlPath))
                {
                    conn.CreateTable<TestProgress>();

                    foreach (var test in tests)
                    {
                        //var str = s.Serialize<TestResult>(test.Result);
                        int count = conn.Table<TestProgress>().Delete(p => p.FullyQualifiedName == test.FullyQualifiedName && test.DisplayName == test.DisplayName);
                    }
                }
            }
            catch (System.Exception ex) { }
        }
        public void SaveProgress(IEnumerable<TestResultVM> tests, bool append = true)
        {
            try
            {
                tests = tests.Where(t => t.Result != null);
                using (var conn = new SQLiteConnection(progressSqlPath))
                {
                    conn.CreateTable<TestProgress>();

                    foreach (var test in tests)
                    {
                        conn.Table<TestProgress>().Delete(p => p.FullyQualifiedName == test.Test.FullyQualifiedName && p.DisplayName == test.Test.DisplayName);
                        using var s = new MemoryStream();
                        MSTestX.UnitTestRunner.TestResultSerializer.Serialize(s, test.Result);
                        conn.Insert(new TestProgress() { FullyQualifiedName = test.Test.FullyQualifiedName, DisplayName = test.DisplayName, Result = s.ToArray() });
                    }
                }
            }
            catch(System.Exception ex) { }
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
            if(testRunner == null || !testRunner.Tests.Any())
                return Task.FromResult(Enumerable.Empty<TestResult>());
            return Run(testRunner.Tests.OrderBy(tst => tst.FullyQualifiedName), Settings);
        }
        public Task<IEnumerable<TestResult>> Run(IRunSettings runSettings)
        {
            return Run(testRunner.Tests.OrderBy(tst => tst.FullyQualifiedName), runSettings);
        }

        private List<TestResult> results;

        public Task<IEnumerable<TestResult>> Run(IEnumerable<TestCase> testCollection)
        {
            return Run(testCollection, Settings);
        }

        public Task<IEnumerable<TestResult>> RunRemainingTests(IRunSettings runSettings = null)
        {
            return Run(Tests.Where(t => t.Result is null).Select(t => t.Test).ToArray(), runSettings ?? Settings);
        }

        public Task<IEnumerable<TestResult>> RunFailedTests(IRunSettings runSettings = null)
        {
            return Run(Tests?.Where(t => t.Result?.Outcome == TestOutcome.Failed).Select(t => t.Test).ToArray(), runSettings ?? Settings);
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
                Logger.Log($"Test run failed: {ex.Message}\nStack trace: {ex.StackTrace}");
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
            }
            
            if (testCollection.Count() == Tests.Count())
                DeleteProgress();
            else
                DeleteProgress(testCollection);
            DateTime start = DateTime.Now;
            Logger.Log($"STARTING TESTRUN {testCollection.Count()} Tests");
            var task = testRunner.Run(testCollection.OrderBy(tst => tst.FullyQualifiedName), runSettings, t.Token);
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
                try
                {
#if DEBUG
                    File.Delete(Settings.TrxOutputPath);
#endif
                    trxWriter?.OnTestRunComplete(new Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.Zero));
#if DEBUG
                    var trx = File.ReadAllText(Settings.TrxOutputPath);
                    System.Diagnostics.Debug.WriteLine(trx);
#endif
                }
                catch (PlatformNotSupportedException) // Throws due to https://github.com/microsoft/vstest/issues/4736. However it's thrown after TRX is written
                {
                    Debug.Assert(File.Exists(Settings.TrxOutputPath));
#if DEBUG
                    var trx = File.ReadAllText(Settings.TrxOutputPath);
                    System.Diagnostics.Debug.WriteLine(trx);
#endif
                }
                trxWriter = null;
                Logger.Log($"TRXREPORT LOCATION: {Settings.TrxOutputPath}");
            }
            var childResults = results.Where(tt => GetProperty<int>("InnerResultsCount", tt, 0) == 0); // Avoid counting parent tests
            DiagnosticsInfo += $"\nLast run duration: {(end - start).ToString("c")}";
            DiagnosticsInfo += $"\n{childResults.Where(a => a.Outcome == TestOutcome.Passed).Count()} passed - {childResults.Where(a => a.Outcome == TestOutcome.Failed).Count()} failed";
            if (Settings.ProgressLogPath != null) DiagnosticsInfo += $"\nLog: {Settings.ProgressLogPath}";
            if (Settings.TrxOutputPath != null) DiagnosticsInfo += $"\nTRX Report: {Settings.TrxOutputPath}";
            OnPropertyChanged(nameof(DiagnosticsInfo));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(Status));
            HostApp?.RaiseTestRunCompleted(results);

            Logger.Log($"COMPLETED TESTRUN Total:{childResults.Count()} Failed:{childResults.Where(a => a.Outcome == TestOutcome.Failed).Count()} Passed:{childResults.Where(a => a.Outcome == TestOutcome.Passed).Count()}  Skipped:{childResults.Where(a => a.Outcome == TestOutcome.Skipped).Count()}");
            if (Settings.TerminateAfterExecution)
            {
                Terminate();
            }
            return results;
        }

        private void Terminate()
        {
#if __IOS__
            var selector = new ObjCRuntime.Selector("terminateWithSuccess");
            UIKit.UIApplication.SharedApplication.PerformSelector(selector, UIKit.UIApplication.SharedApplication, 0);
#elif __ANDROID__
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#else
            Environment.Exit(0);
#endif
        }

        public bool IsRunning => testRunner?.IsRunning ?? false;
        public bool IsInitialized => Tests?.Any() == true;
        public int PassedTests => Tests?.SelectMany(t=>t.Results).Where(t => t.Outcome == TestOutcome.Passed).Count() ?? 0;
        public int PassedTestsWithoutChildren => Tests?.Where(t => t.Result?.Outcome == TestOutcome.Passed).Count() ?? 0;
        public int FailedTests => Tests?.SelectMany(t => t.Results)?.Where(t => t?.Outcome == TestOutcome.Failed).Count() ?? 0;
        public int SkippedTests => Tests?.SelectMany(t => t.Results)?.Where(t => t?.Outcome == TestOutcome.Skipped).Count() ?? 0;
        public int NotRunTests => Tests?.Where(t => t.Result == null).Count() ?? 0;
        public double Percentage => Tests?.Any(t => t.Result?.Outcome == TestOutcome.Passed) == true ? (int)(PassedTestsWithoutChildren * 100d / (FailedTests + PassedTestsWithoutChildren)) : 0;
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

        private static string s_computerName;
        private static string ComputerName
        {
            get
            {
                if(s_computerName is null)
                {
#if __ANDROID__
                    s_computerName = $"{Android.OS.Build.Manufacturer} {Android.OS.Build.Model} v{Environment.OSVersion.Version.Major}";
#elif __IOS__
                    var pLen = System.Runtime.InteropServices.Marshal.AllocHGlobal(sizeof(int));
                    sysctlbyname("hw.machine", IntPtr.Zero, pLen, IntPtr.Zero, 0);
                    var length = System.Runtime.InteropServices.Marshal.ReadInt32(pLen);
                    string deviceName = null;
                    if (length > 0)
                    {
                        var pStr = System.Runtime.InteropServices.Marshal.AllocHGlobal(length);
                        sysctlbyname("hw.machine", pStr, pLen, IntPtr.Zero, 0);
                        deviceName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(pStr);
                        System.Runtime.InteropServices.Marshal.FreeHGlobal(pStr);
                    }
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(pLen);
                    if (!string.IsNullOrEmpty(deviceName))
                        s_computerName = $"{deviceName} - {UIKit.UIDevice.CurrentDevice.SystemName} {UIKit.UIDevice.CurrentDevice.SystemVersion}";
                    else
                        s_computerName = $"{UIKit.UIDevice.CurrentDevice.SystemName} {UIKit.UIDevice.CurrentDevice.SystemVersion}";
#else
                   s_computerName = Environment.MachineName;
#endif
                }
                return s_computerName;
            }
        }
#if __IOS__
        [System.Runtime.InteropServices.DllImport("libc", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern int sysctlbyname([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] string property, IntPtr output, IntPtr length, IntPtr newp, uint newlen);
#endif
        void ITestExecutionRecorder.RecordResult(TestResult testResult)
        {
            var execId = GetProperty<Guid>("ExecutionId", testResult, Guid.Empty);
            if (execId == Guid.Empty)
            {
                var prop = testResult.Properties.Where(p => p.Id == "ExecutionId").FirstOrDefault();
                if (prop != null)
                    testResult.SetPropertyValue(prop, Guid.NewGuid());
            }

            results?.Add(testResult);
            var innerResultsCount = GetProperty<int>("InnerResultsCount", testResult, 0);
            var parentExecId = GetProperty<Guid>("ParentExecId", testResult, Guid.Empty);
            var test = tests[testResult.TestCase.Id];
            if (parentExecId == Guid.Empty) // We don't report child result in the UI
            {
                testResult.ComputerName = ComputerName;
                test.Result = testResult;

                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(Percentage));
                OnPropertyChanged(nameof(TestStatus));
                OnPropertyChanged(nameof(NotRunTests));
                OnPropertyChanged(nameof(PassedTests));
                OnPropertyChanged(nameof(FailedTests));
                OnPropertyChanged(nameof(SkippedTests));
                if (innerResultsCount > 0) // Prep the child results getting reported immediately after this one
                    test.ChildResults = new List<TestResult>(innerResultsCount);
            }
            else
            {
                test.ChildResults.Add(testResult);
                test.OnPropertyChanged(nameof(TestResultVM.ChildResults));
            }
            Log($"Completed test '{testResult.TestCase.FullyQualifiedName}': {testResult.Outcome} {testResult.ErrorMessage}");
            if (testResult.Attachments.Count > 0)
            {
                connection?.SendAttachments(testResult.Attachments, Settings.TestRunDirectory);
                Settings.TestRecorder?.RecordAttachments(testResult.Attachments);
            }
            trxWriter?.OnTestResult(new TestResultEventArgs(testResult));
            Logger.LogResult(testResult);
            connection?.SendTestEndResult(testResult);
            Settings.TestRecorder?.RecordResult(testResult);
            
            if (test.ChildResults == null || test.ChildResults.Count == test.ChildResultCount)
            {
                // Single test or complete set of datarows: Save
                SaveProgress(new TestResultVM[] { test }, true);
            }
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
            connection?.SendAttachments(attachmentSets, Settings.TestRunDirectory);
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
