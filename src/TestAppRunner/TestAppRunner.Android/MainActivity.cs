using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;
using MSTestX;

namespace TestAppRunner.Droid
{
    [Activity(Name = "TestAppRunner.RunTestsActivity", Label = "MSTestX Test Runner", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : MSTestX.TestRunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            base.OnCreate(bundle);
        }

        protected override TestOptions GenerateTestOptions()
        {
            var testOptions = base.GenerateTestOptions();
            //Set/override test settings.
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
    }
}

