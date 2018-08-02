using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace TestAppRunner.Droid
{
    [Activity(Label = "TestAppRunner", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
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
            var testSettings = new TestAppRunner.TestSettings();
            testSettings.AutoStart = Intent.GetBooleanExtra("AutoRun", false);
#if true
            testSettings.AutoStart = true;
            // Note: To write the log to the external storage, make sure READ_EXTERNAL_STORAGE and WRITE_EXTERNAL_STORAGE is enabled in the manifest
            var logsdir = System.IO.Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "AndroidUnitTests");
            string date = System.DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");
            string logFileName = System.IO.Path.Combine(logsdir, $"{date}_Android_UnitTest.log");
            string resultsFileName = System.IO.Path.Combine(logsdir, $"{date}_Android_UnitTest_Results.trx");
            if (CheckSelfPermission(Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                logFileName = null; resultsFileName = null;
            }
            else
            {
                if (!System.IO.Directory.Exists(logsdir))
                    System.IO.Directory.CreateDirectory(logsdir);
            }
            testSettings.ProgressLogPath = logFileName;
            testSettings.TrxOutputPath = resultsFileName;
#endif
            testSettings.ShutdownOnCompletion = testSettings.AutoStart;
            LoadApplication(new App(testSettings));
        }
    }
}

