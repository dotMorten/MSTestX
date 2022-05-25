using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestAppRunner.Droid
{
    [Activity(Name = "testAppRunner.RunTestsActivity", Label = "MSTestX.Forms", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : MSTestX.TestRunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            base.OnCreate(bundle);
        }

        protected override MSTestX.TestOptions GenerateTestOptions()
        {
            var testOptions = base.GenerateTestOptions(); // Creates default test options and initializes some values based on intent arguments.
            // Set/override test settings...

            // Set default timeout to 30 seconds
            testOptions.SettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>" +
                   "<RunSettings>" +
                   "<!--MSTest adapter-->" +
                   "<MSTestV2><!--If no timeout is specified on a test, use this as default-->" +
                   "<TestTimeout>30000</TestTimeout>" +
                   "</MSTestV2>" +
                   "</RunSettings>";
            return testOptions;
        }

        protected override void OnTestRunStarted(IEnumerable<TestCase> testCases)
        {
            base.OnTestRunStarted(testCases);
        }

        protected override void OnTestRunCompleted(IEnumerable<TestResult> results)
        {
            base.OnTestRunCompleted(results);
        }

        protected override void OnTestsDiscovered(IEnumerable<TestCase> testCases)
        {
            base.OnTestsDiscovered(testCases);
            // Run all tests:
            // Task<IEnumerable<TestResult>> results = base.RunTestsAsync(testCases);
        }
    }
}

