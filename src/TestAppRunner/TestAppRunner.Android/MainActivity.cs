using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace TestAppRunner.Droid
{
    [Activity(Name = "TestAppRunner.RunTestsActivity", Label = "MSTestX Test Runner", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);

            var testOptions = new TestAppRunner.TestOptions();
            // You can deploy and launch the app from the ADB shell and parsing intent parameters
            // Example:
            // adb install PATH_TO_APK/TestAppRunner.Android-Signed.apk
            // Launch the app and pass parameters where -n is for starting an intent
            // The intent should be [AppName]/[Activity Name]
            // Use --ez for passing boolean, --es for passing a string, --ei for passing int
            // adb shell am start -n TestAppRunner/TestAppRunner.Android --ez AutoRun true --es TrxReportFile TestReport
            // Once test run is complete you can copy the report back:
            // adb exec -out run -as com.mstestx.TestAppRunner cat/data/data/com.mstestx.TestAppRunner/files/TestReport.trx > TestReport.trx
            testOptions.AutoRun = Intent.GetBooleanExtra("AutoRun", false);
            string path = Intent.GetStringExtra("ReportFile");
            // Or generate a new time-stamped log file path on each run:
            // if (string.IsNullOrEmpty(path))
            //     path = "TestAppRunner_" + System.DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");

            if (!string.IsNullOrEmpty(path))
            {
                path = System.IO.Path.Combine(ApplicationContext.FilesDir.Path, path);
                testOptions.TrxOutputPath = path + ".trx";
                testOptions.ProgressLogPath = path + ".log";
            }
            testOptions.TerminateAfterExecution = testOptions.AutoRun;
            var testApp = new App(testOptions);

            // Disable screen saver while tests are running
            testApp.TestRunStarted += (a, testCases) => Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
            testApp.TestRunCompleted += (a, results) => Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
            
            LoadApplication(testApp);
        }
    }
}

