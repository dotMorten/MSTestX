using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MSTestX.UnitTestRunner.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    /// Shows the results for a single test
    /// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public partial class ItemDetailPage : ContentPage
	{
		internal ItemDetailPage (TestResultVM vm)
		{
            this.BindingContext = vm;
            InitializeComponent();
		}

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (!TestRunnerVM.Instance.IsRunning)
            {
                var _ = TestRunnerVM.Instance.Run(new[] { ((TestResultVM)BindingContext).Test });
            }
        }

        private async void list_ItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var item = args.SelectedItem as TestResult;
            if (item == null)
                return;
            await Navigation.PushAsync(new ItemDetailPage( new TestResultVM(item.TestCase) { Result = item }));

            // Manually deselect item.
            (sender as ListView).SelectedItem = null;
        }

        private async void attachment_Selected(object sender, SelectedItemChangedEventArgs e)
        {
            var attachment = e.SelectedItem as UriDataAttachment;
            if(attachment != null)
            {
                await Navigation.PushAsync(new AttachmentPage(attachment));
            }

            // Manually deselect item.
            (sender as ListView).SelectedItem = null;
        }
    }

    /// <summary>
    /// Internal use
    /// </summary>
    public class AttachmentNameConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is UriDataAttachment uda)
            {
                return uda.Uri.OriginalString.Split('\\', '/').LastOrDefault();
            }
            return value;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}