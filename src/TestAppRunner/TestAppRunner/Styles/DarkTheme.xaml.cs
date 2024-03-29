﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace MSTestX.Styles
{
    /// <summary>
    /// Defines the dark-theme resources
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DarkTheme : ResourceDictionary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DarkTheme"/> class.
        /// </summary>
        public DarkTheme()
        {
            InitializeComponent();
        }
    }
}