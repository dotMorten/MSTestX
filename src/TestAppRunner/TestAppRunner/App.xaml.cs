using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace TestAppRunner
{
	public partial class App : Application
	{
		public App (TestOptions settings = null)
		{
			InitializeComponent();
            ViewModels.TestRunnerVM.Instance.Settings = settings ?? new TestOptions();
            ViewModels.TestRunnerVM.Instance.HostApp = this;
            MainPage = new NavigationPage(new Views.AllTestsPage());
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}

        internal void RaiseTestRunStarted(IEnumerable<TestCase> tests) => TestRunStarted?.Invoke(this, tests);

        internal void RaiseTestRunCompleted(IEnumerable<TestResult> results) => TestRunCompleted?.Invoke(this, results);

        public event EventHandler<IEnumerable<TestCase>> TestRunStarted;

        public event EventHandler<IEnumerable<TestResult>> TestRunCompleted;
    }
}
