using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms;

namespace TestAppRunner
{
    public abstract class VMBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }

    public class TestRunnerVM : VMBase, ITestExecutionRecorder
    {
        private TestRunner testRunner;
        private Dictionary<Guid, TestResultVM> tests = new Dictionary<Guid, TestResultVM>();

        public TestRunnerVM()
        {
            var refs = this.GetType().Assembly.GetReferencedAssemblies().Select(c => c.Name).ToArray();
            refs = System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(c => c.Name).ToArray();
            var refs2 = AppDomain.CurrentDomain.GetAssemblies().Where(c=>!c.IsDynamic).Select(c => System.IO.Path.GetFileName(c.CodeBase)).ToArray();
            testRunner = new TestRunner(refs2, new TestSettings(), this);
            foreach (var item in testRunner.Tests)
            {
                tests[item.Id] = new TestResultVM() { Test = item };
            }
        }

        public void Run()
        {
            testRunner.Run();
        }

        public void Cancel()
        {
            testRunner.Cancel();
        }

        public IEnumerable<TestResultVM> Tests => tests.Values;

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
            test.OnPropertyChanged(nameof(TestResultVM.Status));
            System.Diagnostics.Debug.WriteLine($"Completed test: {testResult.TestCase.FullyQualifiedName} - {testResult.Outcome.ToString().ToUpper()}");
        }

        void ITestExecutionRecorder.RecordStart(TestCase testCase)
        {
            tests[testCase.Id].Outcome = UnitTestOutcome.InProgress;
            tests[testCase.Id].OnPropertyChanged(nameof(TestResultVM.Outcome));
            tests[testCase.Id].OnPropertyChanged(nameof(TestResultVM.Status));
        }

        void ITestExecutionRecorder.RecordEnd(TestCase testCase, TestOutcome outcome)
        {
        }

        void ITestExecutionRecorder.RecordAttachments(IList<AttachmentSet> attachmentSets)
        {

        }

        void IMessageLogger.SendMessage(TestMessageLevel testMessageLevel, string message)
        {
        }
    }

    public class TestResultVM : VMBase
    {
        public TestCase Test { get; set; }
        public Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult Result { get; set; }
        public UnitTestOutcome Outcome { get; set; } = UnitTestOutcome.Unknown;

        public override string ToString() => Test.FullyQualifiedName;
        public string Status => Outcome.ToString();
    }
}
