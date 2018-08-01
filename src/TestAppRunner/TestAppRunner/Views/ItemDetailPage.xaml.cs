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
	public partial class ItemDetailPage : ContentPage
	{
		public ItemDetailPage (TestResultVM vm)
		{
            this.BindingContext = vm;
            InitializeComponent();
            
		}

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (!TestRunnerVM.Instance.IsRunning)
                TestRunnerVM.Instance.Run(new [] { ((TestResultVM)BindingContext).Test });
        }
    }
}