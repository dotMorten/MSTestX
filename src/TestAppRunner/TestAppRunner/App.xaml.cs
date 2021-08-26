using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using TestAppRunner;
using TestAppRunner.ViewModels;
using TestAppRunner.Views;
#if MAUI
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
#else
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
#endif

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace MSTestX
{
    /// <summary>
    /// The Xamarin.Forms Test Runner Application
    /// </summary>
	public partial class RunnerApp : Application
	{
        internal static string AppTheme = "light";

        /// <summary>
        /// Initializes a new instance of the test runner app
        /// </summary>
        /// <param name="settings">Test options</param>
		public RunnerApp(TestOptions settings = null)
		{
            InitializeComponent();
            RunnerApp.Current.Resources = new Styles.DefaultTheme();
            TestRunnerVM.Instance.Settings = settings ?? new TestOptions();
            TestRunnerVM.Instance.HostApp = this;
            TestRunnerVM.Instance.Initialize();
            
            MainPage = new NavigationPage(new AllTestsPage());

#if MAUI
#if __ANDROID__
            TestRunStarted += (a, testCases) => { Dispatcher.BeginInvokeOnMainThread(() => Microsoft.Maui.Essentials.Platform.CurrentActivity.Window?.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn)); };
            TestRunCompleted += (a, results) => { Dispatcher.BeginInvokeOnMainThread(() => Microsoft.Maui.Essentials.Platform.CurrentActivity.Window?.ClearFlags(Android.Views.WindowManagerFlags.KeepScreenOn)); };
#elif __IOS__
            TestRunStarted += (a, testCases) => { Dispatcher.BeginInvokeOnMainThread(() => UIApplication.SharedApplication.IdleTimerDisabled = true); };
            TestRunCompleted += (a, results) => { Dispatcher.BeginInvokeOnMainThread(() => UIApplication.SharedApplication.IdleTimerDisabled = false); };
#endif
#endif
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
