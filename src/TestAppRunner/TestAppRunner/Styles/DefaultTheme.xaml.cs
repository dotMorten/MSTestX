using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace MSTestX.Styles
{
    /// <summary>
    /// Defines the light-theme resources
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DefaultTheme : ResourceDictionary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTheme"/> class.
        /// </summary>
        public DefaultTheme()
        {
            InitializeComponent();
        }
    }
}