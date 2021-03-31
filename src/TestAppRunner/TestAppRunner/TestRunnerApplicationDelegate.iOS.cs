#if __IOS__
using Foundation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace MSTestX
{
    /// <summary>
    /// An MSTest default iOS app delegate for launching the test runs
    /// </summary>
    public abstract class TestRunnerApplicationDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        private MSTestX.RunnerApp testApp;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();

            var testOptions = GenerateTestOptions();
            testApp = new RunnerApp(testOptions);
            // Disable screen saver while tests are running
            testApp.TestRunStarted += (a, testCases) => { BeginInvokeOnMainThread(() => UIApplication.SharedApplication.IdleTimerDisabled = true); OnTestRunStarted(testCases); };
            testApp.TestRunCompleted += (a, results) => { BeginInvokeOnMainThread(() => UIApplication.SharedApplication.IdleTimerDisabled = false); OnTestRunCompleted(results); };
            testApp.TestsDiscovered += (a, testCases) => { OnTestsDiscovered(testCases); };
            LoadApplication(testApp);
            return base.FinishedLaunching(app, options);
        }

        /// <summary>
        /// Gets a value indicating whether a test run is currently running
        /// </summary>
        protected bool IsTestRunActive => TestAppRunner.ViewModels.TestRunnerVM.Instance.IsRunning;

        /// <summary>
        /// Called when a test run is about to start
        /// </summary>
        /// <param name="testCases">A collection of test cases being run</param>
        protected virtual void OnTestRunStarted(IEnumerable<TestCase> testCases)
        {
        }

        /// <summary>
        /// Called when a test run has completed
        /// </summary>
        /// <param name="results">A collection of test results</param>
        protected virtual void OnTestRunCompleted(IEnumerable<TestResult> results)
        {
        }

        /// <summary>
        /// Called on start-up when all tests have been discovered
        /// </summary>
        /// <param name="testCases"></param>
        protected virtual void OnTestsDiscovered(IEnumerable<TestCase> testCases)
        {
        }

        /// <summary>
        /// Launches a test run with the provided set of tests
        /// </summary>
        /// <param name="testCases">The set of tests to run</param>
        /// <param name="settings">Optional test settings</param>
        /// <returns>A task for the test results</returns>
        /// <seealso cref="IsTestRunActive"/>
        /// <exception cref="InvalidOperationException">Trown if <see cref="IsTestRunActive"/> is true</exception>
        protected System.Threading.Tasks.Task<IEnumerable<TestResult>> RunTestsAsync(IEnumerable<TestCase> testCases, IRunSettings settings = null)
        {
            if (IsTestRunActive)
                throw new InvalidOperationException("Can't start a test run while another is already active");
            return testApp.RunTestsAsync(testCases, settings);
        }

        /// <summary>
        /// Generates default test options, and parses the <see cref="NSProcessInfo.ProcessInfo.Arguments"/> arguments. Arguments supported:
        /// -AutoRun [true/false]
        /// -ReportFile [filename]  (without extension - appends .trx and .log and assigns to TrxOuputPath and ProgressLogPath)
        /// -TestAdapterPort [port] The port to use for the console app (defaults to 38300)
        /// -SettingsXml [MSTest Settings XML string]
        /// </summary>
        /// <returns></returns>
        protected virtual TestOptions GenerateTestOptions()
        {
            var testOptions = new TestOptions();

            var procArgs = NSProcessInfo.ProcessInfo.Arguments;
            // Parse arguments and set up test options based on this.
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            if (procArgs.Length > 0)
            {
                for (int i = 1; i < procArgs.Length; i++)
                {
                    var a = procArgs[i];
                    if (a.StartsWith("-") && a.Length > 1)
                    {
                        string value = null;
                        if (i + 1 < procArgs.Length)
                        {
                            var nextArg = procArgs[i + 1];
                            if (!nextArg.StartsWith("-"))
                            {
                                value = nextArg;
                                i++;
                            }
                        }
                        arguments[a.Substring(1)] = value;
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

            return testOptions;
        }
    }
}
#endif