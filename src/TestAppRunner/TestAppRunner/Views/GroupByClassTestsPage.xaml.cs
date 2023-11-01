using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestAppRunner.ViewModels;
#if MAUI
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
#else
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
#endif

namespace TestAppRunner.Views
{
    /// <summary>
    /// Shows all tests in a namespace grouped by class
    /// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class GroupByClassTestsPage : ContentPage
    {
        private TestResultGroup tests;

        internal GroupByClassTestsPage(TestResultGroup tests)
		{
			InitializeComponent ();
            this.tests = tests;
            list.ItemsSource = new List<TestResultGroup>(tests.GroupBy(t => t.ClassName).Select((g, t) => new TestResultGroup(g.Key.StartsWith(tests.Group + ".") ? g.Key.Substring(tests.Group.Length + 1) : g.Key, g)).OrderBy(g=>g.Group));
            currentTestView.BindingContext = TestRunnerVM.Instance;
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

        private async void list_ItemSelected(object sender, SelectionChangedEventArgs args)
        {
            var item = args.CurrentSelection?.FirstOrDefault() as TestResultGroup;
            if (item == null)
                return;

            await Navigation.PushAsync(new TestRunPage(item));

            // Manually deselect item.
            (sender as CollectionView).SelectedItem = null;
        }
    }
}