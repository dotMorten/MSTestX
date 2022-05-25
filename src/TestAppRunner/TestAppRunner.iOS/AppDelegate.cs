using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MSTestX;
using UIKit;

namespace TestAppRunner.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : MSTestX.TestRunnerApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            return base.FinishedLaunching(app, options);
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
