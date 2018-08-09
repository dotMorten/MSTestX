using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace TestAppRunner.iOS
{
    public class Application
    {
        private UnitTests.Tests test; //Necessary to force include of the class library. Xamarin seems to strip it out if it's not referenced in code.

        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            var procArgs = NSProcessInfo.ProcessInfo.Arguments;
            // Parse arguments and set up test options based on this.
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            if (procArgs.Count() > 0)
            {
                for (int i = 1; i < procArgs.Length; i++)
                {
                    var a = procArgs[i];
                    if (a.StartsWith("-"))
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
                        arguments[a] = value;
                        System.Diagnostics.Debug.WriteLine($"Argument: {a} = '{value}'");
                    }
                }
            }
            if (arguments.ContainsKey("AutoRun") && bool.TryParse(arguments["AutoRun"], out bool result))
            {
                TestOptions.AutoRun = result;
                TestOptions.TerminateAfterExecution = result;
            }
            if (arguments.ContainsKey("ReportFile"))
            {
                TestOptions.TrxOutputPath = arguments["ReportFile"] + ".trx";
                TestOptions.ProgressLogPath = arguments["ReportFile"] + ".log";
            }

            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
        public static TestOptions TestOptions { get; } = new TestOptions();
    }
}
