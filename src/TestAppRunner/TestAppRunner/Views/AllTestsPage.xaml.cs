using TestAppRunner.ViewModels;

namespace TestAppRunner.Views
{
    /// <summary>
    /// Shows all tests grouped by namespace. This is the startup/main page
    /// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class AllTestsPage : ContentPage
    {
        internal AllTestsPage()
        {
            InitializeComponent();
            this.BindingContext = TestRunnerVM.Instance;
            TestRunnerVM.Instance.OnTestRunException += Instance_OnTestRunException;

            picker.ItemsSource = new string[] { "Category", "Namespace", "Outcome" };
            picker.SelectedIndex = 1;
            this.ToolbarItems.Add(new ToolbarItem("...", null, () => { PickerPanel.IsVisible = true; }));
        }

        private void Instance_OnTestRunException(object sender, Exception e)
        {
            ErrorHeader.Text = "Test Run Error";
            ErrorMessage.Text = e.Message;
            ErrorPanel.IsVisible = true;
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            if (TestRunnerVM.Instance.IsRunning)
                TestRunnerVM.Instance.Cancel();
            else
            {
                try
                {
                    await TestRunnerVM.Instance.Run();
                }
                catch { }
            }
        }


        private async void list_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var item = args.CurrentSelection?.FirstOrDefault() as TestResultGroup;
            if (item == null)
                return;

            await Navigation.PushAsync(new GroupByClassTestsPage(item));

            // Manually deselect item.
            (sender as CollectionView).SelectedItem = null;
        }

        private void picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            TestRunnerVM.Instance.UpdateGroup(((string[])picker.ItemsSource)[picker.SelectedIndex]);
        }

        private void Error_Close_Button_Clicked(object sender, EventArgs e)
        {
            ErrorPanel.IsVisible = false;
        }

        private void Picker_Close_Button_Clicked(object sender, EventArgs e)
        {
            PickerPanel.IsVisible = false;
        }

        private void RunRemaining_Clicked(object sender, EventArgs e)
        {
            PickerPanel.IsVisible = false;
            if (TestRunnerVM.Instance.IsRunning) return;
            TestRunnerVM.Instance.RunRemainingTests();
        }

        private void RunFailed_Clicked(object sender, EventArgs e)
        {
            PickerPanel.IsVisible = false;
            if (TestRunnerVM.Instance.IsRunning) return;
            TestRunnerVM.Instance.RunFailedTests();
        }

        private void Save_Report_Clicked(object sender, EventArgs e)
        {
#if MAUI
            string path = Path.Combine(Path.GetTempPath(), DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".trx");
            TrxWriter.GenerateReport(path, TestRunnerVM.Instance.Tests.Select(t => t.Result).Where(r=>r is not null));
            Microsoft.Maui.ApplicationModel.DataTransfer.Share.RequestAsync(
                new Microsoft.Maui.ApplicationModel.DataTransfer.ShareFileRequest("TRX Test Report", new Microsoft.Maui.ApplicationModel.DataTransfer.ShareFile(path)));
#endif
        }
        private void StopRun_Clicked(object sender, EventArgs e)
        {
            if (TestRunnerVM.Instance.IsRunning)
            {
                TestRunnerVM.Instance.Cancel();
            }
        }
    }
}