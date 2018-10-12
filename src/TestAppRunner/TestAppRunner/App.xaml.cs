using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using TestAppRunner;
using TestAppRunner.ViewModels;
using TestAppRunner.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace MSTestX
{
    /// <summary>
    /// The Xamarin.Forms Test Runner Application
    /// </summary>
	public partial class RunnerApp : Application
	{
        /// <summary>
        /// Initializes a new instance of the test runner app
        /// </summary>
        /// <param name="settings">Test options</param>
		public RunnerApp(TestOptions settings = null)
		{
			InitializeComponent();
            TestRunnerVM.Instance.Settings = settings ?? new TestOptions();
            TestRunnerVM.Instance.HostApp = this;
            TestRunnerVM.Instance.Initialize();
            MainPage = new NavigationPage(new AllTestsPage());
		}

        /// <inheritdoc />
        protected override void OnStart ()
		{
			// Handle when your app starts
		}

        /// <inheritdoc />
        protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

        /// <inheritdoc />
		protected override void OnResume ()
		{
			// Handle when your app resumes
		}

        /// <summary>
        /// Runs a set of tests
        /// </summary>
        /// <param name="testCases"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public System.Threading.Tasks.Task<IEnumerable<TestResult>> RunTestsAsync(IEnumerable<TestCase> testCases, Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter.IRunSettings settings = null)
        {
            return TestRunnerVM.Instance.Run(testCases, settings);
        }

        internal void RaiseTestRunStarted(IEnumerable<TestCase> tests) => TestRunStarted?.Invoke(this, tests);

        internal void RaiseTestRunCompleted(IEnumerable<TestResult> results) => TestRunCompleted?.Invoke(this, results);

        internal void RaiseTestsDiscovered(IEnumerable<TestCase> results) => TestsDiscovered?.Invoke(this, results);

        /// <summary>
        /// Raised when a test run has started
        /// </summary>
        public event EventHandler<IEnumerable<TestCase>> TestRunStarted;

        /// <summary>
        /// Raised when a test run has completed
        /// </summary>
        public event EventHandler<IEnumerable<TestResult>> TestRunCompleted;

        /// <summary>
        /// Raised when all tests have been discovered
        /// /// </summary>
        public event EventHandler<IEnumerable<TestCase>> TestsDiscovered;
    }
}
