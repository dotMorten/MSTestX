#if __ANDROID__
using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Android;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace MSTestX
{
    public abstract class TestRunnerActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);

            var testOptions = GenerateTestOptions();
            // Launch the test app
            var testApp = new RunnerApp(testOptions);

            // Disable screen saver while tests are running
            testApp.TestRunStarted += (a, testCases) => { OnTestRunStarted(testCases); RunOnUiThread(() => Window?.AddFlags(WindowManagerFlags.KeepScreenOn)); };
            testApp.TestRunCompleted += (a, results) => { OnTestRunCompleted(results); RunOnUiThread(() => Window?.ClearFlags(WindowManagerFlags.KeepScreenOn)); };

            LoadApplication(testApp);
        }

        protected virtual void OnTestRunStarted(IEnumerable<TestCase> testCases)
        {
        }

        protected virtual void OnTestRunCompleted(IEnumerable<TestResult> results)
        {
        }

        /// <summary>
        /// Properties that can be set via intents:
        /// - bool AutoRun
        /// - string ReportFile (without extension - appends .trx and .log and assigns to TrxOuputPath and ProgressLogPath in the ApplicationContext.FilesDir.Path folder)
        /// - string SettingsXml
        /// - ushort TestAdapterPort
        /// </summary>
        /// <returns></returns>
        protected virtual MSTestX.TestOptions GenerateTestOptions()
        {
            var testOptions = new TestOptions();
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

            // Get the MSTest Settings as documented here: https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?view=vs-2017
            // Example setting default timeout to 10000ms:
            // <?xml version=""1.0"" encoding=""utf-8""?>
            // <RunSettings>
            //   <MSTestV2>
            //      <TestTimeout>10000</TestTimeout>
            //  </MSTestV2>
            // </RunSettings>"
            testOptions.SettingsXml = Intent.GetStringExtra("SettingsXml");

            testOptions.TestAdapterPort = (ushort)Intent.GetIntExtra("TestAdapterPort", testOptions.TestAdapterPort);

            return testOptions;
        }
    }
}
#endif