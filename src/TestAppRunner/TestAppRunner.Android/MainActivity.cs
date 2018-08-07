using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace TestAppRunner.Droid
{
    [Activity(Name = "TestAppRunner.RunTestsActivity", Label = "TestAppRunner", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);

            if (CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                Android.Support.V4.App.ActivityCompat.RequestPermissions(this, new String[] { Android.Manifest.Permission.WriteExternalStorage }, 1);
                return;
            }
            else
            {
                LaunchApp();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            // if was resumed by the storage permission dialog
            LaunchApp();
        }

        private void LaunchApp()
        {
            var testSettings = new TestAppRunner.TestOptions();
            // You can deploy and launch the app from the ADB shell and parsing intent parameters
            // Example:
            // adb install PATH_TO_APK/TestAppRunner.Android-Signed.apk
            // Launch the app and pass parameters where -n is for starting an intent
            // The intent should be [AppName]/[Activity Name]
            // Use --ez for passing boolean, --es for passing a string, --ei for passing int
            // adb shell am start -n TestAppRunner/TestAppRunner.Android --ez AutoRun true --es TrxReportFile TestResults/TestRunReport
            // Once test run is complete you can copy the report back:
            // adb pull /storage/emulated/0/TestResults/TestRunReport.trx TestResults/TestRunReport.trx
            testSettings.AutoRun = Intent.GetBooleanExtra("AutoRun", false);
            string path = Intent.GetStringExtra("ReportFile");
            // Or generate a new time-stamped log file path on each run:
            // if (string.IsNullOrEmpty(path))
            //     path = System.IO.Path.Combine("AndroidUnitTests", "TestAppRunner_" + System.DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss"));

            if (!string.IsNullOrEmpty(path))
            {
                path = System.IO.Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path);
                testSettings.TrxOutputPath = path + ".trx";
                testSettings.ProgressLogPath = path + ".log";
                if (CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
                {
                    // If we don't have write permission, turn off report output
                    testSettings.TrxOutputPath = null;
                    testSettings.ProgressLogPath = null;
                }
                else
                {
                    var fi = new System.IO.FileInfo(path);
                    if (!fi.Directory.Exists)
                        fi.Directory.Create();
                }
                   
            }
            testSettings.TerminateAfterExecution = testSettings.AutoRun;
            LoadApplication(new App(testSettings));
        }
    }
}

