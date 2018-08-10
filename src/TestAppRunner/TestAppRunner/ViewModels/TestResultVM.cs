using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace TestAppRunner.ViewModels
{
    internal class TestResultVM : VMBase
    {
        public TestResultVM(TestCase test)
        {
            Test = test;
        }

        public string DisplayName => result?.DisplayName ?? Test?.DisplayName;

        public TestCase Test { get; }

        private TestResult result;

        public TestResult Result
        {
            get { return result; }
            set
            {
                result = value;
                inProgress = false;
                ChildResults = null;
                OnPropertiesChanged(nameof(Result), nameof(Duration), nameof(Messages), nameof(HasMessages), nameof(HasError), nameof(Outcome), nameof(HasStacktrace), nameof(IsInProgress), nameof(ChildResults));
            }
        }

        public IList<TestResult> ChildResults { get; set; }

        private bool inProgress;
        internal void SetInProgress()
        {
            inProgress = true;
            OnPropertyChanged(nameof(Outcome));
            OnPropertyChanged(nameof(IsInProgress));
        }
        public Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome Outcome
        {
            get
            {
                if (result == null) return inProgress? Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.InProgress : Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.Unknown;
                switch (result.Outcome)
                {
                    case TestOutcome.Failed: return Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.Failed;
                    case TestOutcome.Passed: return Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.Passed;
                    case TestOutcome.NotFound: return Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.Error;
                    case TestOutcome.Skipped: return Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.NotRunnable;
                    case TestOutcome.None:
                    default:
                        return Microsoft.VisualStudio.TestTools.UnitTesting.UnitTestOutcome.Unknown;
                }
            }
        }

        public bool IsInProgress => inProgress;

        public string Category
        {
            get
            {
                return Test.GetProperty("MSTestDiscoverer.TestCategory", new string[] { }).FirstOrDefault();
            }
        }

        public string Duration
        {
            get
            {
                if (Result == null) return null;
                return Result.Duration.ToString("g");
            }
        }

        public string ClassName => Test.FullyQualifiedName.Substring(0, Test.FullyQualifiedName.LastIndexOf("."));

        public string Namespace => ClassName.Substring(0, ClassName.LastIndexOf("."));

        public bool HasProperties => Test.GetProperty<KeyValuePair<string, string>[]>("TestObject.Traits", new KeyValuePair<string, string>[] { }).Any();

        public string Properties
        {
            get
            {
                var traits = Test.GetProperty("TestObject.Traits", new KeyValuePair<string, string>[] { });
                string str = "";
                foreach (var trait in traits)
                    str += $"{trait.Key} = {trait.Value}\n";
                return str.Trim();
            }
        }

        public bool HasMessages => Result?.Messages?.Any() == true;

        public string Messages
        {
            get
            {
                if (Result?.Messages == null) return null;
                string p = "";
                foreach (var msg in Result.Messages)
                    p += $"{msg.Category}: {msg.Text.Trim()}\n";
                return p.Trim();
            }
        }

        public bool HasError => !string.IsNullOrEmpty(Result?.ErrorMessage);

        public bool HasStacktrace => !string.IsNullOrEmpty(Result?.ErrorStackTrace);

        public override string ToString() => Test.FullyQualifiedName;
    }

    internal static class PropertyExtensions
    {
        public static T GetProperty<T>(this TestObject test, string id, T defaultValue = default(T))
        {
            var prop = test.Properties.Where(p => p.Id == id).FirstOrDefault();
            if (prop != null)
                return test.GetPropertyValue<T>(prop, defaultValue);
            return defaultValue;
        }
    }
}
