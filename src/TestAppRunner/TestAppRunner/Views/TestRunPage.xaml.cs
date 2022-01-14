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
            this.BindingContext = testCases;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (TestRunnerVM.Instance.IsRunning)
            {
                TestRunnerVM.Instance.Cancel();
            }
            else
            {
                var _ = TestRunnerVM.Instance.Run(((TestResultGroup)BindingContext).Select(t => t.Test));
            }
        }

        private async void list_ItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var item = args.SelectedItem as TestResultVM;
            if (item == null)
                return;

            await Navigation.PushAsync(new ItemDetailPage(item));

            // Manually deselect item.
            (sender as ListView).SelectedItem = null;
        }
    }
}