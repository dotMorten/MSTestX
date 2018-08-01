using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TestAppRunner.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class TestRunPage : ContentPage
    {
        private static TestRunnerVM vm;
        public TestRunPage ()
		{
			InitializeComponent ();
            if(vm == null)
                vm = new TestRunnerVM();
            this.BindingContext = vm;

            picker.ItemsSource = new string[] { "Category", "Namespace", "Outcome" };
            picker.SelectedIndex = 0;
        }
        private void Button_Clicked(object sender, EventArgs e)
        {
            if (vm.IsRunning)
                vm.Cancel();
            else
                vm.Run();
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

        private void picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            vm.UpdateGroup(((string[])picker.ItemsSource)[picker.SelectedIndex]);
        }
    }
}