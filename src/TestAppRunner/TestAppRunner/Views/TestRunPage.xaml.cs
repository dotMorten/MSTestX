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
    public partial class TestRunPage : ContentPage
    {
        internal TestRunPage (TestResultGroup testCases)
		{
			InitializeComponent();
            this.BindingContext = testCases;
            currentTestView.BindingContext = TestRunnerVM.Instance;
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