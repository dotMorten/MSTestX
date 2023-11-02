using MSTestX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAppRunner.Maui
{
    internal class App : RunnerApp
    {
        public App() : base()
        {
        }

        protected override void OnSettingsMenuLoaded(List<Tuple<string, Action>> menuItems)
        {
            menuItems.Add(new Tuple<string, Action>("Custom action", () => Current.MainPage.DisplayAlert("Hello!", "You clicked a custom action", "OK")));
            base.OnSettingsMenuLoaded(menuItems);
        }
    }
}
