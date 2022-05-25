using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
#if MAUI
using Microsoft.Maui.Controls;
#else
using Xamarin.Forms;
#endif


namespace TestAppRunner.ViewModels
{
    internal abstract class VMBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
                catch { }
            });
        }

        public void OnPropertiesChanged(params string[] propertyNames)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    foreach (var propertyName in propertyNames)
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
                catch { }
            });
        }
    }
}
