using TestAppRunner.ViewModels;

namespace TestAppRunner.Views
{
    /// <summary>
    /// Shows the test results for a single test
    /// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class TestRunPage : ContentPage
    {
        internal TestRunPage (TestResultGroup testCases)
		{
			InitializeComponent();
            currentTestView.BindingContext = TestRunnerVM.Instance;
            loopIterationLabel.BindingContext = TestRunnerVM.Instance;
            runUntilFailureButton.BindingContext = TestRunnerVM.Instance;
            this.BindingContext = testCases;
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            if (TestRunnerVM.Instance.IsBusy)
            {
                TestRunnerVM.Instance.Cancel();
            }
            else
            {
                try
                {
                    await TestRunnerVM.Instance.Run(((TestResultGroup)BindingContext).Select(t => t.Test));
                }
                catch (Exception ex)
                {
                    Logger.Log($"Run command failed: {ex}");
                    await DisplayAlert("Test Run Error", ex.Message, "OK");
                }
            }
        }

        private async void RunUntilFailureButton_Clicked(object sender, EventArgs e)
        {
            if (TestRunnerVM.Instance.IsBusy)
            {
                TestRunnerVM.Instance.Cancel();
            }
            else
            {
                try
                {
                    await TestRunnerVM.Instance.RunUntilFailure(((TestResultGroup)BindingContext).Select(t => t.Test));
                }
                catch (Exception ex)
                {
                    Logger.Log($"Run-until-failure command failed: {ex}");
                    await DisplayAlert("Test Run Error", ex.Message, "OK");
                }
            }
        }

        private async void list_ItemSelected(object sender, SelectionChangedEventArgs args)
        {
            var item = args.CurrentSelection?.FirstOrDefault() as TestResultVM;
            if (item == null)
                return;

            await Navigation.PushAsync(new ItemDetailPage(item));

            // Manually deselect item.
            (sender as CollectionView).SelectedItem = null;
        }
    }
}
