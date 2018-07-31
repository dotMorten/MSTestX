using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TestAppRunner
{
	public partial class MainPage : ContentPage
	{
        TestRunnerVM vm;
        public MainPage()
		{
			InitializeComponent();
            this.BindingContext = vm = new TestRunnerVM();
            //list.ItemsSource = vm.Tests;
		}

        private void Button_Clicked(object sender, EventArgs e)
        {
            vm.Run();
        }
    }
}
