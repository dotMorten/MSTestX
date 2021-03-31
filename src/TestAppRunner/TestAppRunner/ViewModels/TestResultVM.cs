using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace TestAppRunner.ViewModels
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class TestResultVM : VMBase
    {
        public TestResultVM(TestCase test)
        {
            Test = test;
        }

        public string DisplayName => result?.DisplayName ?? Test?.DisplayName;

        public string DataRowCompletion
        {
            get
            {
                if (ChildResults == null || ChildResults.Count == 0)
                    return string.Empty;

                return $"{ChildResults.Where(t => t.Outcome == TestOutcome.Passed).Count() * 100 / ChildResults.Count}%";
            }
        }

        public TestCase Test { get; }

        private TestResult result;

        /// <summary>
        ///  Gets the number of child results based on the test result property, or -1 if no test result is available at this point.
        /// </summary>
        public int ChildResultCount
        {
            get => Result is null ? -1 : Result.GetProperty<int>("InnerResultsCount", 0);
        }

        public TestResult Result
        {
            get { return result; }
            set
            {
                result = value;
                inProgress = false;
                ChildResults = null;
                OnPropertiesChanged(nameof(Result), nameof(Duration), nameof(Messages), nameof(HasMessages), nameof(HasError), nameof(Outcome), nameof(HasStacktrace), nameof(IsInProgress), nameof(ChildResults), nameof(DataRowCompletion), nameof(Attachments));
            }
        }
        public IEnumerable<TestResult> Results
        {
            get { return ChildResults ?? (Result != null ? Enumerable.Repeat(Result, 1) : Enumerable.Empty<TestResult>()); }
        }

        private IList<TestResult> childResults;

        public IList<TestResult> ChildResults
        {
            get => childResults;
            set
            {
                childResults = value;
                OnPropertiesChanged(nameof(ChildResults), nameof(DataRowCompletion));
            }
        }

        public IEnumerable<UriDataAttachment> Attachments
        {
            get
            {
                if(result != null && result.Attachments != null && result.Attachments.Any())
                    return result.Attachments.SelectMany(t => t.Attachments);
                return null;
            }
        }

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
        private string DebuggerDisplay => $"{Test.FullyQualifiedName} - {Outcome}" + (ChildResults?.Count > 0 ? $" ({ChildResults.Count} children)" : "");
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
