using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
#if MAUI
using Microsoft.Maui.Controls;
#else
using Xamarin.Forms;
#endif

namespace TestAppRunner.Views
{
    /// <summary>
    /// Converts <c>null</c> to <c>false</c> or if converter parameter is <c>reverse</c>, converts it to true
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class NullToFalseConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(parameter as string == "reverse")
                return value == null;
            return value != null;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
