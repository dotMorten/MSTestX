using System;
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
	}
}
