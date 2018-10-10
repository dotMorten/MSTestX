using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TestAppRunner
{
    internal static class Logger
    {
        static System.Reflection.MethodInfo logMethod;

        public static void Log(string message, string category = "MSTestX")
        {
            System.Diagnostics.Trace.WriteLine(message, category);
        }

        internal static void LogTestStart(TestCase testCase)
        {
            Log("TEST STARTING: " + testCase.FullyQualifiedName);
        }

        internal static void LogResult(TestResult testResult)
        {
            var testName = testResult.TestCase.FullyQualifiedName;
            if (testResult.DisplayName != null)
            {
                var className = testResult.TestCase.FullyQualifiedName.Substring(0, testResult.TestCase.FullyQualifiedName.LastIndexOf("."));
                testName = $"{className}.{testResult.DisplayName}";
            }
        }
    }
}
