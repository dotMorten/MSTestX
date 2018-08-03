using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAppRunner.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TestAppRunner.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class GroupByClassTestsPage : ContentPage
    {
        private TestResultGroup tests;

        internal GroupByClassTestsPage(TestResultGroup tests)
		{
			InitializeComponent ();
            this.tests = tests;
            list.ItemsSource = new List<TestResultGroup>(tests.GroupBy(t => t.ClassName).Select((g, t) => new TestResultGroup(g.Key, g)).OrderBy(g=>g.Group));
            this.BindingContext = tests;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (TestRunnerVM.Instance.IsRunning)
            {
                TestRunnerVM.Instance.Cancel();
            }
            else
            {
                var _ = TestRunnerVM.Instance.Run(tests.Select(t => t.Test));
            }
        }

        private async void list_ItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var item = args.SelectedItem as TestResultGroup;
            if (item == null)
                return;

            await Navigation.PushAsync(new TestRunPage(item));

            // Manually deselect item.
            (sender as ListView).SelectedItem = null;
        }
    }
}