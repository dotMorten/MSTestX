using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestAppRunner
{
    internal static class Logger
    {
        static System.Runtime.Serialization.Json.DataContractJsonSerializer testCaseSerializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TestCase));
        static System.Runtime.Serialization.Json.DataContractJsonSerializer testResultSerializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TestResult));

        static System.Reflection.MethodInfo logMethod;

        public static void Log(string message, string category = "MSTestX")
        {
            System.Diagnostics.Trace.WriteLine(message, category);
            if(Xamarin.Forms.Device.RuntimePlatform == Xamarin.Forms.Device.Android)
            {
                if (logMethod == null)
                {
                    var logType = Type.GetType("Android.Util.Log, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=84e04ff9cfb79065");
                    logMethod = logType.GetMethod("Info", new Type[] { typeof(string), typeof(string) });
                }
                logMethod.Invoke(null, new object[] { category, message });
            }
        }
        internal static void LogTestStart(TestCase testCase)
        {
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    testCaseSerializer.WriteObject(ms, testCase);
            //    string msg = Encoding.UTF8.GetString(ms.ToArray());
            //    Log("TEST STARTING:" + msg);
            //}
            Log("TEST STARTING: " + testCase.FullyQualifiedName);
        }

        internal static void LogResult(TestResult testResult)
        {
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    testResultSerializer.WriteObject(ms, testResult);
            //    string msg = Encoding.UTF8.GetString(ms.ToArray());
            //    Log("TEST COMPLETED:"+msg);
            //}
            var testName = testResult.TestCase.FullyQualifiedName;
            if (testResult.DisplayName != null)
            {
                var className = testResult.TestCase.FullyQualifiedName.Substring(0, testResult.TestCase.FullyQualifiedName.LastIndexOf("."));
                testName = $"{className}.{testResult.DisplayName}";
            }
            Log($"TEST COMPLETED: {testName} - {testResult.Outcome}" + (testResult.Outcome == TestOutcome.Failed?  " " + testResult.ErrorMessage : ""));
        }
    }
}
