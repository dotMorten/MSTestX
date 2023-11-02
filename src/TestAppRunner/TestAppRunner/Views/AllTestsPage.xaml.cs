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
            this.ToolbarItems.Add(new ToolbarItem("...", null, ShowSettingsPanel));
            /*
             *                      <Button Text="Run Remaining Tests" Clicked="RunRemaining_Clicked" BackgroundColor="{DynamicResource accentColor}" TextColor="White" CornerRadius="5" Margin="5" Padding="10" />
                            <Button Text="Run Failed Tests" Clicked="RunFailed_Clicked" BackgroundColor="{DynamicResource accentColor}" TextColor="White" CornerRadius="5" Margin="5" Padding="10" />
                            <Button Text="Stop Test Run" Clicked="StopRun_Clicked" BackgroundColor="{DynamicResource accentColor}" TextColor="White" CornerRadius="5" Margin="5" Padding="10" />
                            <Button Text="Save Report" Clicked="Save_Report_Clicked" BackgroundColor="{DynamicResource accentColor}" TextColor="White" CornerRadius="5" Margin="5" Padding="10" />*/

        }
        private void ShowSettingsPanel()
        {
            List<Tuple<string, Action>> actions = new List<Tuple<string, Action>>();
            actions.Add(new Tuple<string, Action>("Run Remaining Tests", RunRemaining_Clicked));
            actions.Add(new Tuple<string, Action>("Run Failed Tests", RunFailed_Clicked));
            actions.Add(new Tuple<string, Action>("Stop Test Run", StopRun_Clicked));
            actions.Add(new Tuple<string, Action>("Save Report", Save_Report_Clicked));
            (Application.Current as MSTestX.RunnerApp)?.OnSettingsMenuLoaded(actions);
            BindableLayout.SetItemsSource(SettingsButtonList, actions);
            PickerPanel.IsVisible = true;
        }

        private void SettingsButton_Clicked(object sender, EventArgs e)
        {
            PickerPanel.IsVisible = false; 
            var context = (sender as Button).BindingContext as Tuple<string, Action>;
            context?.Item2();
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

        private void RunRemaining_Clicked()
        {
            if (TestRunnerVM.Instance.IsRunning) return;
            TestRunnerVM.Instance.RunRemainingTests();
        }

        private void RunFailed_Clicked()
        {
            if (TestRunnerVM.Instance.IsRunning) return;
            TestRunnerVM.Instance.RunFailedTests();
        }

        private void Save_Report_Clicked()
        {
#if MAUI
            string path = Path.Combine(Path.GetTempPath(), DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".trx");
            TrxWriter.GenerateReport(path, TestRunnerVM.Instance.Tests.Select(t => t.Result).Where(r=>r is not null));
            Microsoft.Maui.ApplicationModel.DataTransfer.Share.RequestAsync(
                new Microsoft.Maui.ApplicationModel.DataTransfer.ShareFileRequest("TRX Test Report", new Microsoft.Maui.ApplicationModel.DataTransfer.ShareFile(path)));
#endif
        }
        private void StopRun_Clicked()
        {
            if (TestRunnerVM.Instance.IsRunning)
            {
                TestRunnerVM.Instance.Cancel();
            }
        }
    }
}