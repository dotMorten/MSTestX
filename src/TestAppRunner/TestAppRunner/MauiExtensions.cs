#if MAUI
#nullable enable
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using System;
using System.Collections.Generic;
using System.Text;

namespace MSTestX
{
    /// <summary>
    /// MSTextX Maui configuration extensions
    /// </summary>
    public static class MauiExtensions
    {
        /// <summary>
        /// Configures the MSTestX Application
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureTestOptions"></param>
        /// <returns></returns>
        public static MauiAppBuilder UseTestApp(this MauiAppBuilder builder, Func<TestOptions, TestOptions>? configureTestOptions = null)
        {
            return builder.UseTestApp<RunnerApp>(configureTestOptions);
        }

        /// <summary>
        /// Configures the MSTestX Application
        /// </summary>
        /// <typeparam name="T">The MSTextX Runner app to use</typeparam>
        /// <param name="builder"></param>
        /// <param name="configureTestOptions"></param>
        /// <returns></returns>
        public static MauiAppBuilder UseTestApp<T>(this MauiAppBuilder builder, Func<TestOptions, TestOptions>? configureTestOptions = null) where T : RunnerApp
        {
            return builder.UseMauiApp<T>()
                .ConfigureLifecycleEvents((events) =>
                {
#if __ANDROID__
                    events.AddAndroid(android => android.OnCreate((activity, savedInstanceState) =>
                    {
                        TestOptions testOptions = new TestOptions();
                        var intent = activity.Intent;
                        // You can deploy and launch the app from the ADB shell and parsing intent parameters
                        // Example:
                        // adb install PATH_TO_APK/TestAppRunner.Android-Signed.apk
                        // Launch the app and pass parameters where -n is for starting an intent
                        // The intent should be [AppName]/[Activity Name]
                        // Use --ez for passing boolean, --es for passing a string, --ei for passing int
                        // adb shell am start -n TestAppRunner/TestAppRunner.Android --ez AutoRun true --es TrxReportFile TestReport
                        // Once test run is complete you can copy the report back:
                        // adb exec -out run -as com.mstestx.TestAppRunner cat/data/data/com.mstestx.TestAppRunner/files/TestReport.trx > TestReport.trx
                        testOptions.AutoRun = intent!.GetBooleanExtra("AutoRun", false);
                        testOptions.AutoResume = intent.GetBooleanExtra("AutoResume", false);
                        string? path = intent.GetStringExtra("ReportFile");
                        // Or generate a new time-stamped log file path on each run:
                        // if (string.IsNullOrEmpty(path))
                        //     path = "TestAppRunner_" + System.DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss");

                        if (!string.IsNullOrEmpty(path))
                        {
                            path = System.IO.Path.Combine(activity.ApplicationContext!.FilesDir!.Path, path);
                            testOptions.TrxOutputPath = path + ".trx";
                            testOptions.ProgressLogPath = path + ".log";
                        }
                        testOptions.TerminateAfterExecution = testOptions.AutoRun || testOptions.AutoResume;

                        // Get the MSTest Settings as documented here: https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?view=vs-2017
                        // Example setting default timeout to 10000ms:
                        // <?xml version=""1.0"" encoding=""utf-8""?>
                        // <RunSettings>
                        //   <MSTestV2>
                        //      <TestTimeout>10000</TestTimeout>
                        //  </MSTestV2>
                        // </RunSettings>"
                        testOptions.SettingsXml = intent.GetStringExtra("SettingsXml");

                        testOptions.TestAdapterPort = (ushort)intent.GetIntExtra("TestAdapterPort", testOptions.TestAdapterPort);
                        configureTestOptions?.Invoke(testOptions);
                        ((RunnerApp)RunnerApp.Current!).Initialize(testOptions);
                    }));
#elif __IOS__
                    events.AddiOS(ios => ios.FinishedLaunching((app, launchOptions) =>
                    {
                        TestOptions testOptions = new TestOptions();
                        var procArgs = Foundation.NSProcessInfo.ProcessInfo.Arguments;
                        // Parse arguments and set up test options based on this.
                        Dictionary<string, string?> arguments = new Dictionary<string, string?>();
                        if (procArgs.Length > 0)
                        {
                            for (int i = 1; i < procArgs.Length; i++)
                            {
                                var a = procArgs[i];
                                if (a.StartsWith("-") && a.Length > 1)
                                {
                                    string? value = null;
                                    if (i + 1 < procArgs.Length)
                                    {
                                        var nextArg = procArgs[i + 1];
                                        if (!nextArg.StartsWith("-"))
                                        {
                                            value = nextArg;
                                            i++;
                                        }
                                    }
                                    arguments[a.TrimStart('-')] = value;
                                }
                            }
                        }

                        if (arguments.ContainsKey("AutoRun") && bool.TryParse(arguments["AutoRun"], out bool result))
                        {
                            testOptions.AutoRun = result;
                            testOptions.TerminateAfterExecution = result;
                        }

                        if (arguments.ContainsKey("AutoResume") && bool.TryParse(arguments["AutoResume"], out bool result2))
                        {
                            testOptions.AutoResume = result2;
                            testOptions.TerminateAfterExecution = result2;
                        }

                        if (arguments.ContainsKey("ReportFile"))
                        {
                            testOptions.TrxOutputPath = arguments["ReportFile"] + ".trx";
                            testOptions.ProgressLogPath = arguments["ReportFile"] + ".log";
                        }

                        if (arguments.ContainsKey("TestAdapterPort") && ushort.TryParse(arguments["TestAdapterPort"], out ushort port))
                        {
                            testOptions.TestAdapterPort = port;
                        }

                        if (arguments.ContainsKey("SettingsXml"))
                        {
                            testOptions.SettingsXml = arguments["SettingsXml"];
                        }
                        configureTestOptions?.Invoke(testOptions);
                        ((RunnerApp)RunnerApp.Current!).Initialize(testOptions);
                        return true;
                    }));
#elif WINDOWS
                    events.AddWindows(win => win.OnLaunched((app, eventArgs) =>
                    {
                        TestOptions testOptions = new TestOptions();
                        //TODO: Parse eventArgs.Arguments
                        configureTestOptions?.Invoke(testOptions);
                        ((RunnerApp)RunnerApp.Current!).Initialize(testOptions);
                    }));
#endif
                });
        }
    }
}
#endif