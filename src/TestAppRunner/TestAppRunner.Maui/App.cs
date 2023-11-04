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
            menuItems.Add(new Tuple<string, Action>("Run two specific tests", async () =>
            {
                var tests= TestCases?.Where(t => t.DisplayName == "TestOK" || t.DisplayName == "MoreTests_1");
                try
                {
                    var results = await RunTestsAsync(tests);
                    var count = results.Where(t => t.Outcome == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed).Count();
                    _ = Current.MainPage.DisplayAlert("Test run complete", $"{count} tests passed", "OK");
                }
                catch(System.Exception ex)
                {
                    _ = Current.MainPage.DisplayAlert("Test run exception", ex.Message, "OK");
                }
            }));
            menuItems.Add(new Tuple<string, Action>("Custom test list", () =>
            {
                var tests = TestCases?.Where(t => t.DisplayName == "TestOK" || t.DisplayName == "MoreTests_1");
                NavigateToTestList("Custom Test List", tests);
            }));
            base.OnSettingsMenuLoaded(menuItems);
        }
    }
}
