using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace TestAppRunner
{
	public partial class App : Application
	{
		public App (TestSettings settings = null)
		{
			InitializeComponent();
            ViewModels.TestRunnerVM.Instance.Settings = settings ?? new TestSettings();
            MainPage = new NavigationPage(new Views.GroupTestsPage());
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
	}
}
