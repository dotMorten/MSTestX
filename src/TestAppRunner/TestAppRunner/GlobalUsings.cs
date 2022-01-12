global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using Microsoft.VisualStudio.TestPlatform.ObjectModel;

#if MAUI
global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Controls.Xaml;
global using Microsoft.Maui.Graphics;
global using Colors = Microsoft.Maui.Graphics.Colors;
#else
global using Xamarin.Forms;
global using Xamarin.Forms.Xaml;
global using Colors = Xamarin.Forms.Color;
#endif
